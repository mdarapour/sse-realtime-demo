using SseDemo.Models;
using SseDemo.Outbox.Models;
using SseDemo.Services;
using SseDemo.Repositories;

namespace SseDemo.Outbox;

/// <summary>
/// Service that handles SSE events through MongoDB outbox pattern
/// </summary>
public class SseOutboxService : BackgroundService
{
    private readonly ILogger<SseOutboxService> _logger;
    private readonly IOutboxEventRepository _outboxRepository;
    private readonly ISequenceRepository _sequenceRepository;
    private readonly SseService _sseService;
    private readonly string _instanceId;
    private long _lastDeliveredSequence = 0;

    public SseOutboxService(
        ILogger<SseOutboxService> logger,
        IOutboxEventRepository outboxRepository,
        ISequenceRepository sequenceRepository,
        SseService sseService)
    {
        _logger = logger;
        _outboxRepository = outboxRepository;
        _sequenceRepository = sequenceRepository;
        _sseService = sseService;
        _instanceId = $"{Environment.MachineName}-{Guid.NewGuid():N}";
        
        // Create indexes through repositories
        Task.Run(async () => {
            await _outboxRepository.CreateIndexesAsync();
            await _sequenceRepository.InitializeAsync();
        });
    }


    /// <summary>
    /// Publishes an event to the outbox
    /// </summary>
    public async Task PublishEventAsync(SseEvent sseEvent, string? targetClientId = null)
    {
        // Generate atomic sequence number
        var sequenceNumber = await _sequenceRepository.GetNextSequenceNumberAsync();
        
        var outboxEvent = new SseOutboxEvent
        {
            EventId = sseEvent.Id ?? Guid.NewGuid().ToString(),
            EventType = sseEvent.Event ?? "message",
            EventData = sseEvent.Data ?? string.Empty,
            TargetClientId = targetClientId,
            CreatedAt = DateTime.UtcNow,
            SequenceNumber = sequenceNumber
        };
        
        await _outboxRepository.PublishEventAsync(outboxEvent);
        _logger.LogDebug("Published event {EventId} with sequence {Sequence} to outbox", 
            outboxEvent.EventId, sequenceNumber);
    }
    

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting SseOutboxService on instance {InstanceId}", _instanceId);
        
        // Get the last sequence from the database if we're restarting
        await InitializeLastDeliveredSequence(stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DeliverNewEventsToLocalClients(stoppingToken);
                await Task.Delay(TimeSpan.FromMilliseconds(50), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected when shutting down
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error delivering outbox events");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
        
        _logger.LogInformation("Stopping SseOutboxService on instance {InstanceId}", _instanceId);
    }
    
    private async Task InitializeLastDeliveredSequence(CancellationToken cancellationToken)
    {
        try
        {
            // Start from the latest event minus a small buffer to handle restarts
            var latestEvent = await _outboxRepository.GetLatestEventAsync(cancellationToken);
                
            if (latestEvent != null)
            {
                // Start from 100 events back to ensure no events are missed on restart
                _lastDeliveredSequence = Math.Max(0, latestEvent.SequenceNumber - 100);
                _logger.LogInformation("Initialized delivery from sequence {Sequence} on instance {InstanceId}", 
                    _lastDeliveredSequence, _instanceId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing last delivered sequence");
        }
    }

    private async Task DeliverNewEventsToLocalClients(CancellationToken cancellationToken)
    {
        try
        {
            // Query for events newer than what we've delivered to our local clients
            var events = await _outboxRepository.GetEventsAfterSequenceAsync(_lastDeliveredSequence, 100, cancellationToken);
            
            if (events.Count == 0)
            {
                return;
            }
            
            foreach (var outboxEvent in events)
            {
                // Convert to SseEvent
                var sseEvent = new SseEvent
                {
                    Id = outboxEvent.EventId,
                    Event = outboxEvent.EventType,
                    Data = outboxEvent.EventData,
                    SequenceNumber = outboxEvent.SequenceNumber
                };
                
                // Deliver to all local clients on this pod
                if (string.IsNullOrEmpty(outboxEvent.TargetClientId))
                {
                    // Broadcast event
                    _sseService.DeliverEventToLocalClients(sseEvent);
                }
                else
                {
                    // Targeted event - only deliver if the client is connected to this pod
                    _sseService.SendEventToClient(outboxEvent.TargetClientId, sseEvent);
                }
                
                _lastDeliveredSequence = outboxEvent.SequenceNumber;
            }
            
            _logger.LogDebug("Pod {InstanceId} delivered {Count} events up to sequence {Sequence}", 
                _instanceId, events.Count, _lastDeliveredSequence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying/delivering events from outbox");
        }
    }

}