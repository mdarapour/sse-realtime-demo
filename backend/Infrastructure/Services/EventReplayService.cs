using SseDemo.Application.Services;
using SseDemo.Repositories;
using SseDemo.Services;

namespace SseDemo.Infrastructure.Services;

/// <summary>
/// Implementation of event replay service
/// </summary>
public class EventReplayService : IEventReplayService
{
    private readonly ICheckpointRepository _checkpointRepository;
    private readonly IOutboxEventRepository _outboxRepository;
    private readonly ISseService _sseService;
    private readonly ILogger<EventReplayService> _logger;

    public EventReplayService(
        ICheckpointRepository checkpointRepository,
        IOutboxEventRepository outboxRepository,
        ISseService sseService,
        ILogger<EventReplayService> logger)
    {
        _checkpointRepository = checkpointRepository;
        _outboxRepository = outboxRepository;
        _sseService = sseService;
        _logger = logger;
    }

    public async Task<ReplayResult> ReplayEventsFromCheckpointAsync(
        string clientId, 
        long fromSequence, 
        int maxEvents = 1000, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting event replay for client {ClientId} from sequence {FromSequence}", 
                clientId, fromSequence);

            var events = await _outboxRepository.GetEventsAfterSequenceAsync(fromSequence, maxEvents, cancellationToken);
            
            var result = new ReplayResult
            {
                FromSequence = fromSequence,
                EventsReplayed = events.Count,
                ToSequence = events.LastOrDefault()?.SequenceNumber ?? fromSequence,
                HasMoreEvents = events.Count == maxEvents
            };

            if (events.Any())
            {
                _logger.LogInformation("Replaying {Count} events for client {ClientId} (sequences {From} to {To})", 
                    events.Count, clientId, fromSequence, result.ToSequence);

                // Send events to the specific client
                foreach (var outboxEvent in events)
                {
                    var sseEvent = new Models.SseEvent
                    {
                        Id = outboxEvent.EventId,
                        Event = outboxEvent.EventType,
                        Data = outboxEvent.EventData,
                        SequenceNumber = outboxEvent.SequenceNumber
                    };

                    _sseService.SendEventToClient(clientId, sseEvent);
                    
                    // Update checkpoint as we replay
                    await _checkpointRepository.UpdateCheckpointAsync(
                        clientId, 
                        outboxEvent.SequenceNumber, 
                        outboxEvent.EventId, 
                        cancellationToken);
                    
                    // Small delay to avoid overwhelming the client
                    await Task.Delay(10, cancellationToken);
                }
            }
            else
            {
                _logger.LogInformation("No events to replay for client {ClientId} from sequence {FromSequence}", 
                    clientId, fromSequence);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to replay events for client {ClientId} from sequence {FromSequence}", 
                clientId, fromSequence);
            
            return new ReplayResult
            {
                FromSequence = fromSequence,
                EventsReplayed = 0,
                ToSequence = fromSequence,
                HasMoreEvents = false,
                Error = ex.Message
            };
        }
    }

    public async Task<ClientCheckpointInfo?> GetClientCheckpointAsync(string clientId, CancellationToken cancellationToken = default)
    {
        try
        {
            var checkpoint = await _checkpointRepository.GetCheckpointAsync(clientId, cancellationToken);
            
            if (checkpoint == null)
            {
                return null;
            }

            return new ClientCheckpointInfo
            {
                ClientId = checkpoint.ClientId,
                LastSequenceNumber = checkpoint.LastSequenceNumber,
                LastEventId = checkpoint.LastEventId,
                LastUpdated = checkpoint.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get checkpoint for client {ClientId}", clientId);
            throw;
        }
    }

    public async Task UpdateClientCheckpointAsync(
        string clientId, 
        long sequenceNumber, 
        string? eventId = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _checkpointRepository.UpdateCheckpointAsync(clientId, sequenceNumber, eventId, cancellationToken);
            
            _logger.LogDebug("Updated checkpoint for client {ClientId} to sequence {SequenceNumber}", 
                clientId, sequenceNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update checkpoint for client {ClientId}", clientId);
            throw;
        }
    }

    public async Task ClearClientCheckpointAsync(string clientId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Implementation would depend on repository having a delete method
            // For now, we'll update to sequence 0
            await _checkpointRepository.UpdateCheckpointAsync(clientId, 0, null, cancellationToken);
            
            _logger.LogInformation("Cleared checkpoint for client {ClientId}", clientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear checkpoint for client {ClientId}", clientId);
            throw;
        }
    }

    public async Task<IEnumerable<EventSummary>> GetEventsBetweenSequencesAsync(
        long fromSequence, 
        long toSequence, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var events = await _outboxRepository.GetEventsAfterSequenceAsync(fromSequence, int.MaxValue, cancellationToken);
            
            return events
                .Where(e => e.SequenceNumber <= toSequence)
                .Select(e => new EventSummary
                {
                    EventId = e.EventId,
                    EventType = e.EventType,
                    SequenceNumber = e.SequenceNumber,
                    CreatedAt = e.CreatedAt,
                    TargetClientId = e.TargetClientId
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get events between sequences {From} and {To}", fromSequence, toSequence);
            throw;
        }
    }
}