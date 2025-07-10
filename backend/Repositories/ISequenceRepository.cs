using SseDemo.Outbox.Models;

namespace SseDemo.Repositories;

/// <summary>
/// Repository interface for sequence number generation
/// </summary>
public interface ISequenceRepository
{
    /// <summary>
    /// Gets the next sequence number atomically
    /// </summary>
    Task<long> GetNextSequenceNumberAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates the sequence collection and initializes if needed
    /// </summary>
    Task InitializeAsync();
}