using System.Text.Json;
using SseDemo.Models;

namespace SseDemo.Services;

/// <summary>
/// Service for creating and sending typed SSE messages
/// </summary>
public class SseMessageService
{
    private readonly ISseService _sseService;
    private readonly ILogger<SseMessageService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public SseMessageService(ISseService sseService, ILogger<SseMessageService> logger)
    {
        _sseService = sseService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Sends a notification to all clients
    /// </summary>
    /// <param name="message">The notification message</param>
    /// <param name="severity">The severity level (info, warning, error)</param>
    public void SendNotificationToAll(string message, string severity = "info")
    {
        var payload = new NotificationPayload
        {
            Message = message,
            Severity = severity
        };

        var sseEvent = new SseEvent
        {
            Id = Guid.NewGuid().ToString(),
            Event = SseEventTypes.Notification,
            Data = JsonSerializer.Serialize(payload, _jsonOptions)
        };

        _sseService.SendEventToAll(sseEvent);
    }

    /// <summary>
    /// Sends a notification to a specific client
    /// </summary>
    /// <param name="clientId">The client ID</param>
    /// <param name="message">The notification message</param>
    /// <param name="severity">The severity level (info, warning, error)</param>
    public void SendNotificationToClient(string clientId, string message, string severity = "info")
    {
        var payload = new NotificationPayload
        {
            Message = message,
            Severity = severity
        };

        var sseEvent = new SseEvent
        {
            Id = Guid.NewGuid().ToString(),
            Event = SseEventTypes.Notification,
            Data = JsonSerializer.Serialize(payload, _jsonOptions)
        };

        _sseService.SendEventToClient(clientId, sseEvent);
    }

    /// <summary>
    /// Sends a data update to all clients
    /// </summary>
    /// <param name="entityId">The ID of the entity that was updated</param>
    /// <param name="entityType">The type of the entity that was updated</param>
    /// <param name="changes">The changes made to the entity</param>
    public void SendDataUpdateToAll(string entityId, string entityType, object changes)
    {
        var payload = new DataUpdatePayload
        {
            EntityId = entityId,
            EntityType = entityType,
            Changes = JsonSerializer.SerializeToElement(changes, _jsonOptions)
        };

        var sseEvent = new SseEvent
        {
            Id = Guid.NewGuid().ToString(),
            Event = SseEventTypes.DataUpdate,
            Data = JsonSerializer.Serialize(payload, _jsonOptions)
        };

        _sseService.SendEventToAll(sseEvent);
    }

    /// <summary>
    /// Sends a data update to a specific client
    /// </summary>
    /// <param name="clientId">The client ID</param>
    /// <param name="entityId">The ID of the entity that was updated</param>
    /// <param name="entityType">The type of the entity that was updated</param>
    /// <param name="changes">The changes made to the entity</param>
    public void SendDataUpdateToClient(string clientId, string entityId, string entityType, object changes)
    {
        var payload = new DataUpdatePayload
        {
            EntityId = entityId,
            EntityType = entityType,
            Changes = JsonSerializer.SerializeToElement(changes, _jsonOptions)
        };

        var sseEvent = new SseEvent
        {
            Id = Guid.NewGuid().ToString(),
            Event = SseEventTypes.DataUpdate,
            Data = JsonSerializer.Serialize(payload, _jsonOptions)
        };

        _sseService.SendEventToClient(clientId, sseEvent);
    }

    /// <summary>
    /// Sends an alert to all clients
    /// </summary>
    /// <param name="message">The alert message</param>
    /// <param name="severity">The severity level (critical, high, medium, low)</param>
    /// <param name="category">The alert category (security, system, performance, etc.)</param>
    public void SendAlertToAll(string message, string severity = "high", string category = "system")
    {
        var payload = new AlertPayload
        {
            Message = message,
            Severity = severity,
            Category = category
        };

        var sseEvent = new SseEvent
        {
            Id = Guid.NewGuid().ToString(),
            Event = SseEventTypes.Alert,
            Data = JsonSerializer.Serialize(payload, _jsonOptions)
        };

        _sseService.SendEventToAll(sseEvent);
    }

    /// <summary>
    /// Sends an alert to a specific client
    /// </summary>
    /// <param name="clientId">The client ID</param>
    /// <param name="message">The alert message</param>
    /// <param name="severity">The severity level (critical, high, medium, low)</param>
    /// <param name="category">The alert category (security, system, performance, etc.)</param>
    public void SendAlertToClient(string clientId, string message, string severity = "high", string category = "system")
    {
        var payload = new AlertPayload
        {
            Message = message,
            Severity = severity,
            Category = category
        };

        var sseEvent = new SseEvent
        {
            Id = Guid.NewGuid().ToString(),
            Event = SseEventTypes.Alert,
            Data = JsonSerializer.Serialize(payload, _jsonOptions)
        };

        _sseService.SendEventToClient(clientId, sseEvent);
    }

    /// <summary>
    /// Sends a heartbeat to all clients
    /// </summary>
    public void SendHeartbeatToAll()
    {
        var payload = new HeartbeatPayload();

        var sseEvent = new SseEvent
        {
            Id = Guid.NewGuid().ToString(),
            Event = SseEventTypes.Heartbeat,
            Data = JsonSerializer.Serialize(payload, _jsonOptions)
        };

        _sseService.SendEventToAll(sseEvent);
    }
}
