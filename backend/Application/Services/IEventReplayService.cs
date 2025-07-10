namespace SseDemo.Application.Services;

/// <summary>
/// Service interface for event replay and recovery
/// Handles business logic for checkpoint-based event recovery
/// </summary>
public interface IEventReplayService
{
    /// <summary>
    /// Replays events from a specific checkpoint for a client
    /// </summary>
    Task<ReplayResult> ReplayEventsFromCheckpointAsync(
        string clientId, 
        long fromSequence, 
        int maxEvents = 1000,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the last checkpoint for a client
    /// </summary>
    Task<ClientCheckpointInfo?> GetClientCheckpointAsync(
        string clientId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates the checkpoint for a client
    /// </summary>
    Task UpdateClientCheckpointAsync(
        string clientId,
        long sequenceNumber,
        string? eventId = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clears the checkpoint for a client
    /// </summary>
    Task ClearClientCheckpointAsync(
        string clientId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets events that occurred between two sequence numbers
    /// </summary>
    Task<IEnumerable<EventSummary>> GetEventsBetweenSequencesAsync(
        long fromSequence,
        long toSequence,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an event replay operation
/// </summary>
public class ReplayResult
{
    public int EventsReplayed { get; set; }
    public long FromSequence { get; set; }
    public long ToSequence { get; set; }
    public bool HasMoreEvents { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Client checkpoint information
/// </summary>
public class ClientCheckpointInfo
{
    public string ClientId { get; set; } = string.Empty;
    public long LastSequenceNumber { get; set; }
    public string? LastEventId { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Summary of an event for replay purposes
/// </summary>
public class EventSummary
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public long SequenceNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? TargetClientId { get; set; }
}