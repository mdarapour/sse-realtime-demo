using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using SseDemo.Models;
using SseDemo.Outbox;

namespace SseDemo.Services;

/// <summary>
/// Distributed SSE service that manages local client connections and uses MongoDB outbox for cross-pod communication
/// </summary>
public class SseService : ISseService, IDisposable
{
    private readonly ILogger<SseService> _logger;
    private readonly IServiceProvider _serviceProvider;
    protected readonly ConcurrentDictionary<string, CancellationTokenSource> _clients = new();
    private readonly ConcurrentDictionary<string, string> _clientFilters = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _processedMessageIds = new();
    private readonly Timer _heartbeatTimer;
    private readonly Timer _cleanupTimer;
    private int _eventId = 0;
    private bool _disposed = false;

    // Maximum number of message IDs to track per client
    private const int MaxTrackedMessageIds = 1000;
    private const int CleanupIntervalMinutes = 5;

    public SseService(ILogger<SseService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        
        _logger.LogInformation("SseService initialized for distributed SSE with MongoDB outbox");

        // Setup a timer to send heartbeats every 30 seconds
        _heartbeatTimer = new Timer(SendHeartbeats, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        
        // Setup a timer to clean up old processed message IDs
        _cleanupTimer = new Timer(CleanupProcessedMessageIds, null, 
            TimeSpan.FromMinutes(CleanupIntervalMinutes), 
            TimeSpan.FromMinutes(CleanupIntervalMinutes));
    }

    /// <summary>
    /// Registers a client connection on this pod
    /// </summary>
    public CancellationTokenSource RegisterClient(string clientId, string? filter = null)
    {
        _logger.LogInformation("Client {ClientId} connected locally with filter: {Filter}", 
            clientId, filter ?? "none");

        var cts = new CancellationTokenSource();
        _clients[clientId] = cts;

        if (!string.IsNullOrEmpty(filter))
        {
            _clientFilters[clientId] = filter;
        }

        return cts;
    }

    /// <summary>
    /// Unregisters a client connection from this pod
    /// </summary>
    public void UnregisterClient(string clientId)
    {
        _logger.LogInformation("Client {ClientId} disconnected locally", clientId);

        if (_clients.TryRemove(clientId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }

        _clientFilters.TryRemove(clientId, out _);
        _processedMessageIds.TryRemove(clientId, out _);
    }

    /// <summary>
    /// Sends an event to all connected clients across all pods
    /// </summary>
    public void SendEventToAll(SseEvent sseEvent)
    {
        // Always publish to outbox for distribution across all pods
        Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var outboxService = scope.ServiceProvider.GetRequiredService<SseOutboxService>();
                await outboxService.PublishEventAsync(sseEvent);
                _logger.LogDebug("Published event {EventId} to outbox for broadcast", sseEvent.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing event to outbox");
            }
        });
    }
    
    /// <summary>
    /// Internal method for outbox service to deliver events to local clients only
    /// </summary>
    internal void DeliverEventToLocalClients(SseEvent sseEvent)
    {
        SendEventToLocalClients(sseEvent);
    }

    /// <summary>
    /// Sends an event to a specific client (may be on a different pod)
    /// </summary>
    public void SendEventToClient(string clientId, SseEvent sseEvent)
    {
        // First try local delivery
        if (_clients.ContainsKey(clientId))
        {
            SendEventToLocalClient(clientId, sseEvent);
        }
        else
        {
            // Client is on another pod, use outbox
            Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var outboxService = scope.ServiceProvider.GetRequiredService<SseOutboxService>();
                    await outboxService.PublishEventAsync(sseEvent, clientId);
                    _logger.LogDebug("Published event {EventId} to outbox for client {ClientId}", 
                        sseEvent.Id, clientId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error publishing event to outbox for client {ClientId}", clientId);
                }
            });
        }
    }

    /// <summary>
    /// Sends an event to all locally connected clients on this pod
    /// </summary>
    private void SendEventToLocalClients(SseEvent sseEvent)
    {
        _logger.LogDebug("Sending event to {Count} local clients: {EventType}", 
            _clients.Count, sseEvent.Event);

        foreach (var clientId in _clients.Keys)
        {
            SendEventToLocalClient(clientId, sseEvent);
        }
    }

    /// <summary>
    /// Sends an event to a specific locally connected client
    /// </summary>
    private void SendEventToLocalClient(string clientId, SseEvent sseEvent)
    {
        _logger.LogDebug("Sending event to local client {ClientId}: {EventType}", 
            clientId, sseEvent.Event);

        if (_clients.TryGetValue(clientId, out var cts) && !cts.IsCancellationRequested)
        {
            try
            {
                cts.Token.ThrowIfCancellationRequested();
                OnEventSent?.Invoke(this, new SseEventSentArgs(clientId, sseEvent));
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Client {ClientId} has been cancelled", clientId);
                UnregisterClient(clientId);
            }
        }
    }

    /// <summary>
    /// Gets an async enumerable of SSE events for a specific client
    /// </summary>
    public async IAsyncEnumerable<SseEvent> GetSseEventsAsync(
        string clientId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting SSE event stream for client {ClientId}", clientId);

        // Get the client's cancellation token source
        if (!_clients.TryGetValue(clientId, out var clientCts))
        {
            clientCts = RegisterClient(clientId);
        }

        try
        {
            // Send initial connection established event
            var initialEvent = new SseEvent
            {
                Id = Interlocked.Increment(ref _eventId).ToString(),
                Event = "connected",
                Data = JsonSerializer.Serialize(new 
                { 
                    clientId, 
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() 
                })
            };

            _logger.LogInformation("Sending initial connection event to client {ClientId}", clientId);
            yield return initialEvent;

            // Create a linked token that will be cancelled if either the request is aborted or the client is removed
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, clientCts.Token);

            // Setup a channel to receive events for this client
            var channel = System.Threading.Channels.Channel.CreateUnbounded<SseEvent>();
            _logger.LogInformation("Created channel for client {ClientId}", clientId);

            // Subscribe to events
            EventHandler<SseEventSentArgs> eventHandler = (sender, args) =>
            {
                if (args.ClientId == clientId)
                {
                    // Check for duplicate messages
                    if (!string.IsNullOrEmpty(args.Event.Id) && 
                        !IsMessageNew(clientId, args.Event.Id))
                    {
                        _logger.LogDebug("Skipping duplicate event {EventId} for client {ClientId}",
                            args.Event.Id, clientId);
                        return;
                    }

                    // Check if this event should be filtered
                    if (_clientFilters.TryGetValue(clientId, out var filter) &&
                        !string.IsNullOrEmpty(args.Event.Event) &&
                        !ShouldSendEvent(args.Event.Event, filter))
                    {
                        _logger.LogDebug("Filtering out event {EventType} for client {ClientId} with filter {Filter}",
                            args.Event.Event, clientId, filter);
                        return;
                    }

                    var success = channel.Writer.TryWrite(args.Event);
                    if (!success)
                    {
                        _logger.LogWarning("Failed to write event to channel for client {ClientId}", clientId);
                    }
                }
            };

            _logger.LogInformation("Subscribing to events for client {ClientId}", clientId);
            OnEventSent += eventHandler;

            try
            {
                // Read from the channel until cancellation is requested
                while (await channel.Reader.WaitToReadAsync(linkedCts.Token))
                {
                    while (channel.Reader.TryRead(out var sseEvent))
                    {
                        yield return sseEvent;
                    }
                }
            }
            finally
            {
                _logger.LogInformation("Unsubscribing from events for client {ClientId}", clientId);
                OnEventSent -= eventHandler;
                channel.Writer.Complete();
            }
        }
        finally
        {
            _logger.LogInformation("SSE event stream ended for client {ClientId}", clientId);
        }
    }

    /// <summary>
    /// Event for when an SSE event is sent to a client
    /// </summary>
    public event EventHandler<SseEventSentArgs>? OnEventSent;

    /// <summary>
    /// Checks if a message is new (not a duplicate)
    /// </summary>
    private bool IsMessageNew(string clientId, string messageId)
    {
        var clientMessageIds = _processedMessageIds.GetOrAdd(clientId, _ => new HashSet<string>());
        
        // Add returns true if the item was added (new), false if it already existed (duplicate)
        var isNew = clientMessageIds.Add(messageId);
        
        // Limit the number of tracked message IDs per client
        if (clientMessageIds.Count > MaxTrackedMessageIds)
        {
            var oldestIds = clientMessageIds.Take(clientMessageIds.Count / 2).ToList();
            foreach (var id in oldestIds)
            {
                clientMessageIds.Remove(id);
            }
            _logger.LogDebug("Cleaned up message IDs for client {ClientId}, kept {Count} recent IDs", 
                clientId, clientMessageIds.Count);
        }
        
        return isNew;
    }

    /// <summary>
    /// Determines if an event should be sent based on the client's filter
    /// </summary>
    private bool ShouldSendEvent(string eventType, string filter)
    {
        // Always send connected events (initial connection confirmation)
        if (eventType == "connected")
        {
            return true;
        }

        // For filtered connections, only send events that match the filter
        // This includes heartbeats - if you want heartbeats, filter for "heartbeat"
        return string.Equals(filter, eventType, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Timer callback to send heartbeats to all clients
    /// </summary>
    private void SendHeartbeats(object? state)
    {
        if (_clients.Count == 0)
        {
            return; // No local clients
        }

        var heartbeatEvent = new SseEvent
        {
            Id = Guid.NewGuid().ToString(),
            Event = SseEventTypes.Heartbeat,
            Data = JsonSerializer.Serialize(new HeartbeatPayload())
        };

        // Send heartbeat only to local clients (not through outbox)
        SendEventToLocalClients(heartbeatEvent);
    }

    /// <summary>
    /// Periodically cleans up processed message IDs for disconnected clients
    /// </summary>
    private void CleanupProcessedMessageIds(object? state)
    {
        try
        {
            var activeClients = _clients.Keys.ToHashSet();
            var messageIdClients = _processedMessageIds.Keys.ToList();
            
            foreach (var clientId in messageIdClients)
            {
                if (!activeClients.Contains(clientId))
                {
                    _processedMessageIds.TryRemove(clientId, out _);
                    _logger.LogDebug("Removed processed message IDs for disconnected client {ClientId}", clientId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup of processed message IDs");
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _logger.LogInformation("Disposing SseService");

            // Stop and dispose timers
            _heartbeatTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _heartbeatTimer?.Dispose();
            
            _cleanupTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _cleanupTimer?.Dispose();

            // Cancel and dispose all client connections
            foreach (var clientId in _clients.Keys.ToList())
            {
                UnregisterClient(clientId);
            }

            // Clear collections
            _clients.Clear();
            _clientFilters.Clear();
            _processedMessageIds.Clear();
        }

        _disposed = true;
    }
}

/// <summary>
/// Event arguments for when an SSE event is sent
/// </summary>
public class SseEventSentArgs : EventArgs
{
    public string ClientId { get; }
    public SseEvent Event { get; }

    public SseEventSentArgs(string clientId, SseEvent sseEvent)
    {
        ClientId = clientId;
        Event = sseEvent;
    }
}