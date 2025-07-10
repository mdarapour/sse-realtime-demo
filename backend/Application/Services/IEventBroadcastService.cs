using SseDemo.Models;

namespace SseDemo.Application.Services;

/// <summary>
/// Service interface for broadcasting events to SSE clients
/// Handles business logic for event distribution
/// </summary>
public interface IEventBroadcastService
{
    /// <summary>
    /// Broadcasts a custom event to all connected clients
    /// </summary>
    Task BroadcastEventAsync(string eventType, string data, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a custom event to a specific client
    /// </summary>
    Task SendEventToClientAsync(string clientId, string eventType, string data, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Broadcasts a notification to all connected clients
    /// </summary>
    Task BroadcastNotificationAsync(string message, string severity = "info", CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a notification to a specific client
    /// </summary>
    Task SendNotificationToClientAsync(string clientId, string message, string severity = "info", CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Broadcasts a data update event to all connected clients
    /// </summary>
    Task BroadcastDataUpdateAsync(string entityId, string entityType, object changes, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a data update event to a specific client
    /// </summary>
    Task SendDataUpdateToClientAsync(string clientId, string entityId, string entityType, object changes, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Broadcasts an alert to all connected clients
    /// </summary>
    Task BroadcastAlertAsync(string message, string severity = "high", string category = "system", CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends an alert to a specific client
    /// </summary>
    Task SendAlertToClientAsync(string clientId, string message, string severity = "high", string category = "system", CancellationToken cancellationToken = default);
}