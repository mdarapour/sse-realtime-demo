using SseDemo.Models;

namespace SseDemo.Services;

/// <summary>
/// Interface for SSE service operations
/// </summary>
public interface ISseService
{
    /// <summary>
    /// Registers a client connection
    /// </summary>
    CancellationTokenSource RegisterClient(string clientId, string? filter = null);

    /// <summary>
    /// Unregisters a client connection
    /// </summary>
    void UnregisterClient(string clientId);

    /// <summary>
    /// Sends an event to all connected clients across all pods
    /// </summary>
    void SendEventToAll(SseEvent sseEvent);

    /// <summary>
    /// Sends an event to a specific client (may be on a different pod)
    /// </summary>
    void SendEventToClient(string clientId, SseEvent sseEvent);

    /// <summary>
    /// Gets the SSE event stream for a client
    /// </summary>
    IAsyncEnumerable<SseEvent> GetSseEventsAsync(string clientId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the SSE event stream for a client with filter
    /// </summary>
    IAsyncEnumerable<SseEvent> GetSseEventsAsync(string clientId, string? filter, CancellationToken cancellationToken);
    
    /// <summary>
    /// Gets the SSE event stream for a client with checkpoint support
    /// </summary>
    IAsyncEnumerable<SseEvent> GetSseEventsAsync(string clientId, string? filter, long? checkpoint, string? lastEventId, CancellationToken cancellationToken);
    
    /// <summary>
    /// Checks if a client is currently connected
    /// </summary>
    bool IsClientConnected(string clientId);
    
    /// <summary>
    /// Gets all currently connected client IDs
    /// </summary>
    IEnumerable<string> GetConnectedClients();
}