using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using SseDemo.Models;
using SseDemo.Outbox;
using SseDemo.Repositories;
using SseRealTimeDemo.Models;

namespace SseDemo.Services;

/// <summary>
/// Distributed SSE service that manages local client connections and uses MongoDB outbox for cross-pod communication
/// </summary>
public class SseService : ISseService, IDisposable
{
    private readonly ILogger<SseService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ICheckpointRepository? _checkpointRepository;
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
        
        // Initialize checkpoint repository if available
        try
        {
            _checkpointRepository = serviceProvider.GetService<ICheckpointRepository>();
            if (_checkpointRepository != null)
            {
                // Create indexes through repository
                Task.Run(async () => await _checkpointRepository.CreateIndexesAsync());
                _logger.LogInformation("Checkpoint repository initialized for client position tracking");
            }
            else
            {
                _logger.LogWarning("Checkpoint repository not available. Checkpoint tracking will be disabled.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize checkpoint repository. Checkpoint tracking will be disabled.");
        }
        
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
        // Synchronously publish to outbox to ensure event persistence
        // This prevents event loss if the pod crashes
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var outboxService = scope.ServiceProvider.GetRequiredService<SseOutboxService>();
            
            // Use GetAwaiter().GetResult() to make this synchronous
            // This ensures the event is persisted before we return
            var task = PublishWithRetryAsync(outboxService, sseEvent);
            task.GetAwaiter().GetResult();
            
            _logger.LogDebug("Published event {EventId} to outbox for broadcast", sseEvent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event to outbox after retries");
            throw; // Propagate the exception to the caller
        }
    }
    
    /// <summary>
    /// Publishes an event to the outbox with retry logic
    /// </summary>
    private async Task PublishWithRetryAsync(SseOutboxService outboxService, SseEvent sseEvent, string? targetClientId = null)
    {
        const int maxRetries = 3;
        const int baseDelayMs = 100;
        
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                if (targetClientId != null)
                {
                    await outboxService.PublishEventAsync(sseEvent, targetClientId);
                }
                else
                {
                    await outboxService.PublishEventAsync(sseEvent);
                }
                return; // Success
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                var delay = baseDelayMs * (int)Math.Pow(2, attempt); // Exponential backoff
                _logger.LogWarning(ex, "Failed to publish event to outbox, attempt {Attempt}. Retrying in {Delay}ms", 
                    attempt + 1, delay);
                await Task.Delay(delay);
            }
        }
        
        throw new InvalidOperationException($"Failed to publish event to outbox after {maxRetries + 1} attempts");
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
            // Client is on another pod, use outbox with synchronous write
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var outboxService = scope.ServiceProvider.GetRequiredService<SseOutboxService>();
                
                // Use GetAwaiter().GetResult() to make this synchronous
                var task = PublishWithRetryAsync(outboxService, sseEvent, clientId);
                task.GetAwaiter().GetResult();
                
                _logger.LogDebug("Published event {EventId} to outbox for client {ClientId}", 
                    sseEvent.Id, clientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing event to outbox for client {ClientId} after retries", clientId);
                throw; // Propagate the exception to the caller
            }
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
            // Don't send initial connection event - it bypasses sequencing
            // The client knows it's connected when it starts receiving events

            // Create a linked token that will be cancelled if either the request is aborted or the client is removed
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, clientCts.Token);

            // Setup a bounded channel with backpressure to prevent memory exhaustion
            // Use Wait mode to apply backpressure instead of dropping events
            const int channelCapacity = 10000; // Increased capacity to handle bursts
            var channelOptions = new System.Threading.Channels.BoundedChannelOptions(channelCapacity)
            {
                FullMode = System.Threading.Channels.BoundedChannelFullMode.Wait, // Wait instead of drop
                SingleReader = true,
                SingleWriter = false
            };
            var channel = System.Threading.Channels.Channel.CreateBounded<SseEvent>(channelOptions);
            _logger.LogInformation("Created bounded channel with capacity {Capacity} for client {ClientId}", 
                channelCapacity, clientId);

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

                    // Try to write to channel without blocking the event publisher
                    // Fire and forget to avoid blocking the outbox processing
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // Use a timeout to prevent indefinite blocking
                            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                            await channel.Writer.WriteAsync(args.Event, timeoutCts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogError("Timeout writing event {EventId} to client {ClientId} channel. Client may be too slow.", 
                                args.Event.Id, clientId);
                        }
                        catch (InvalidOperationException ex) when (ex.Message.Contains("closed"))
                        {
                            _logger.LogDebug("Channel closed for client {ClientId} while writing event {EventId}", 
                                clientId, args.Event.Id);
                        }
                    });
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
                        
                        // Update checkpoint after successful delivery
                        // Fire and forget to avoid blocking the stream
                        _ = Task.Run(async () => await UpdateClientCheckpointAsync(clientId, sseEvent));
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
    /// Gets an async enumerable of SSE events for a specific client with checkpoint support
    /// </summary>
    public async IAsyncEnumerable<SseEvent> GetSseEventsAsync(
        string clientId,
        long? checkpoint,
        string? lastEventId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting SSE event stream for client {ClientId} with checkpoint {Checkpoint} and lastEventId {LastEventId}", 
            clientId, checkpoint, lastEventId);

        // First, check if we have a stored checkpoint for this client
        var storedCheckpoint = await GetClientCheckpointAsync(clientId);
        
        // Use the provided checkpoint, or fall back to stored checkpoint
        var effectiveCheckpoint = checkpoint ?? storedCheckpoint;
        
        if (effectiveCheckpoint.HasValue)
        {
            _logger.LogInformation("Client {ClientId} resuming from checkpoint {Checkpoint}", clientId, effectiveCheckpoint.Value);
            
            // Replay missed events from MongoDB outbox
            await ReplayEventsFromCheckpoint(clientId, effectiveCheckpoint.Value, cancellationToken);
        }

        // Continue with normal event streaming
        await foreach (var sseEvent in GetSseEventsAsync(clientId, cancellationToken))
        {
            yield return sseEvent;
        }
    }

    /// <summary>
    /// Replays events from the outbox starting from a given checkpoint
    /// </summary>
    private async Task ReplayEventsFromCheckpoint(string clientId, long checkpoint, CancellationToken cancellationToken)
    {
        try
        {
            // Get the outbox repository to query for missed events
            using var scope = _serviceProvider.CreateScope();
            var outboxRepository = scope.ServiceProvider.GetService<IOutboxEventRepository>();
            
            if (outboxRepository == null)
            {
                _logger.LogWarning("Outbox repository not available. Cannot replay events.");
                return;
            }
            
            // Query for events with sequence number greater than checkpoint
            var missedEvents = await outboxRepository.GetEventsAfterSequenceAsync(checkpoint, 1000, cancellationToken);
            
            _logger.LogInformation("Found {Count} missed events for client {ClientId} from checkpoint {Checkpoint}", 
                missedEvents.Count, clientId, checkpoint);
            
            // Send each missed event to the client
            foreach (var outboxEvent in missedEvents)
            {
                var sseEvent = new SseEvent
                {
                    Id = outboxEvent.EventId,
                    Event = outboxEvent.EventType,
                    Data = outboxEvent.EventData,
                    SequenceNumber = outboxEvent.SequenceNumber
                };
                
                // Send directly to this specific client
                SendEventToClient(clientId, sseEvent);
                
                // Small delay to avoid overwhelming the client
                await Task.Delay(10, cancellationToken);
            }
            
            _logger.LogInformation("Completed replay of {Count} events for client {ClientId}", missedEvents.Count, clientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to replay events from checkpoint for client {ClientId}", clientId);
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

        // Send heartbeat through outbox to ensure it gets a sequence number
        SendEventToAll(heartbeatEvent);
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

    /// <summary>
    /// Updates the client's checkpoint after successfully delivering an event
    /// </summary>
    private async Task UpdateClientCheckpointAsync(string clientId, SseEvent sseEvent)
    {
        if (_checkpointRepository == null || !sseEvent.SequenceNumber.HasValue)
        {
            return;
        }

        await _checkpointRepository.UpdateCheckpointAsync(clientId, sseEvent.SequenceNumber.Value, sseEvent.Id);
    }

    /// <summary>
    /// Gets the last checkpoint for a client
    /// </summary>
    public async Task<long?> GetClientCheckpointAsync(string clientId)
    {
        if (_checkpointRepository == null)
        {
            return null;
        }

        var checkpoint = await _checkpointRepository.GetCheckpointAsync(clientId);
        return checkpoint?.LastSequenceNumber;
    }

    /// <summary>
    /// Gets the SSE event stream for a client with filter
    /// </summary>
    public async IAsyncEnumerable<SseEvent> GetSseEventsAsync(
        string clientId, 
        string? filter, 
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Set the filter for the client
        if (!string.IsNullOrEmpty(filter))
        {
            _clientFilters[clientId] = filter;
        }

        await foreach (var sseEvent in GetSseEventsAsync(clientId, cancellationToken))
        {
            yield return sseEvent;
        }
    }

    /// <summary>
    /// Gets the SSE event stream for a client with checkpoint support
    /// </summary>
    public async IAsyncEnumerable<SseEvent> GetSseEventsAsync(
        string clientId, 
        string? filter, 
        long? checkpoint, 
        string? lastEventId, 
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Set the filter for the client
        if (!string.IsNullOrEmpty(filter))
        {
            _clientFilters[clientId] = filter;
        }

        await foreach (var sseEvent in GetSseEventsAsync(clientId, checkpoint, lastEventId, cancellationToken))
        {
            yield return sseEvent;
        }
    }

    /// <summary>
    /// Checks if a client is currently connected
    /// </summary>
    public bool IsClientConnected(string clientId)
    {
        return _clients.ContainsKey(clientId);
    }

    /// <summary>
    /// Gets all currently connected client IDs
    /// </summary>
    public IEnumerable<string> GetConnectedClients()
    {
        return _clients.Keys.ToList();
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