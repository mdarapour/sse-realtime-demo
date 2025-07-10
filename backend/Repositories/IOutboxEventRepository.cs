using SseDemo.Outbox.Models;

namespace SseDemo.Repositories;

/// <summary>
/// Repository interface for SSE outbox event operations
/// </summary>
public interface IOutboxEventRepository
{
    /// <summary>
    /// Publishes an event to the outbox
    /// </summary>
    Task PublishEventAsync(SseOutboxEvent outboxEvent, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets events with sequence number greater than the specified value
    /// </summary>
    Task<List<SseOutboxEvent>> GetEventsAfterSequenceAsync(long sequenceNumber, int limit = 100, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the latest event to determine starting sequence
    /// </summary>
    Task<SseOutboxEvent?> GetLatestEventAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates indexes for the outbox collection
    /// </summary>
    Task CreateIndexesAsync();
}