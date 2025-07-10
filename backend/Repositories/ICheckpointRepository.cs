using SseRealTimeDemo.Models;

namespace SseDemo.Repositories;

/// <summary>
/// Repository interface for client checkpoint operations
/// </summary>
public interface ICheckpointRepository
{
    /// <summary>
    /// Updates or creates a checkpoint for a client
    /// </summary>
    Task UpdateCheckpointAsync(string clientId, long sequenceNumber, string? eventId = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the checkpoint for a client
    /// </summary>
    Task<ClientCheckpoint?> GetCheckpointAsync(string clientId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates indexes for the checkpoint collection
    /// </summary>
    Task CreateIndexesAsync();
}