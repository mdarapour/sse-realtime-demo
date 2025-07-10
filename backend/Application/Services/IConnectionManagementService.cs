namespace SseDemo.Application.Services;

/// <summary>
/// Service interface for managing SSE client connections
/// Handles business logic for connection lifecycle
/// </summary>
public interface IConnectionManagementService
{
    /// <summary>
    /// Establishes a new SSE connection for a client
    /// </summary>
    /// <param name="clientId">The client identifier</param>
    /// <param name="filter">Optional event type filter</param>
    /// <param name="checkpoint">Optional checkpoint for event replay</param>
    /// <param name="lastEventId">Optional last event ID for recovery</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An async enumerable of SSE events</returns>
    IAsyncEnumerable<string> EstablishConnectionAsync(
        string clientId, 
        string? filter = null, 
        long? checkpoint = null, 
        string? lastEventId = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Disconnects a client
    /// </summary>
    Task DisconnectClientAsync(string clientId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the current connection status for a client
    /// </summary>
    Task<ConnectionStatus> GetConnectionStatusAsync(string clientId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all currently connected client IDs
    /// </summary>
    Task<IEnumerable<string>> GetConnectedClientsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets connection statistics
    /// </summary>
    Task<ConnectionStatistics> GetConnectionStatisticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the connection status of a client
/// </summary>
public class ConnectionStatus
{
    public string ClientId { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public DateTime? ConnectedAt { get; set; }
    public string? Filter { get; set; }
    public long? LastSequenceNumber { get; set; }
}

/// <summary>
/// Represents overall connection statistics
/// </summary>
public class ConnectionStatistics
{
    public int TotalConnections { get; set; }
    public int ActiveConnections { get; set; }
    public Dictionary<string, int> ConnectionsByFilter { get; set; } = new();
    public DateTime? OldestConnection { get; set; }
}