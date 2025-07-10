using System.Text.Json;
using SseDemo.Application.Services;
using SseDemo.Models;
using SseDemo.Services;

namespace SseDemo.Infrastructure.Services;

/// <summary>
/// Implementation of event broadcasting service
/// </summary>
public class EventBroadcastService : IEventBroadcastService
{
    private readonly ISseService _sseService;
    private readonly ILogger<EventBroadcastService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public EventBroadcastService(
        ISseService sseService,
        ILogger<EventBroadcastService> logger)
    {
        _sseService = sseService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task BroadcastEventAsync(string eventType, string data, CancellationToken cancellationToken = default)
    {
        try
        {
            var sseEvent = new SseEvent
            {
                Id = Guid.NewGuid().ToString(),
                Event = eventType ?? SseEventTypes.Message,
                Data = data
            };

            _sseService.SendEventToAll(sseEvent);
            
            _logger.LogInformation("Broadcasted {EventType} event to all clients", eventType);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast {EventType} event", eventType);
            throw;
        }
    }

    public async Task SendEventToClientAsync(string clientId, string eventType, string data, CancellationToken cancellationToken = default)
    {
        try
        {
            var sseEvent = new SseEvent
            {
                Id = Guid.NewGuid().ToString(),
                Event = eventType ?? SseEventTypes.Message,
                Data = data
            };

            _sseService.SendEventToClient(clientId, sseEvent);
            
            _logger.LogInformation("Sent {EventType} event to client {ClientId}", eventType, clientId);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send {EventType} event to client {ClientId}", eventType, clientId);
            throw;
        }
    }

    public async Task BroadcastNotificationAsync(string message, string severity = "info", CancellationToken cancellationToken = default)
    {
        var payload = new NotificationPayload
        {
            Message = message,
            Severity = severity
        };

        var data = JsonSerializer.Serialize(payload, _jsonOptions);
        await BroadcastEventAsync(SseEventTypes.Notification, data, cancellationToken);
    }

    public async Task SendNotificationToClientAsync(string clientId, string message, string severity = "info", CancellationToken cancellationToken = default)
    {
        var payload = new NotificationPayload
        {
            Message = message,
            Severity = severity
        };

        var data = JsonSerializer.Serialize(payload, _jsonOptions);
        await SendEventToClientAsync(clientId, SseEventTypes.Notification, data, cancellationToken);
    }

    public async Task BroadcastDataUpdateAsync(string entityId, string entityType, object changes, CancellationToken cancellationToken = default)
    {
        var payload = new DataUpdatePayload
        {
            EntityId = entityId,
            EntityType = entityType,
            Changes = JsonSerializer.SerializeToElement(changes, _jsonOptions)
        };

        var data = JsonSerializer.Serialize(payload, _jsonOptions);
        await BroadcastEventAsync(SseEventTypes.DataUpdate, data, cancellationToken);
    }

    public async Task SendDataUpdateToClientAsync(string clientId, string entityId, string entityType, object changes, CancellationToken cancellationToken = default)
    {
        var payload = new DataUpdatePayload
        {
            EntityId = entityId,
            EntityType = entityType,
            Changes = JsonSerializer.SerializeToElement(changes, _jsonOptions)
        };

        var data = JsonSerializer.Serialize(payload, _jsonOptions);
        await SendEventToClientAsync(clientId, SseEventTypes.DataUpdate, data, cancellationToken);
    }

    public async Task BroadcastAlertAsync(string message, string severity = "high", string category = "system", CancellationToken cancellationToken = default)
    {
        var payload = new AlertPayload
        {
            Message = message,
            Severity = severity,
            Category = category
        };

        var data = JsonSerializer.Serialize(payload, _jsonOptions);
        await BroadcastEventAsync(SseEventTypes.Alert, data, cancellationToken);
    }

    public async Task SendAlertToClientAsync(string clientId, string message, string severity = "high", string category = "system", CancellationToken cancellationToken = default)
    {
        var payload = new AlertPayload
        {
            Message = message,
            Severity = severity,
            Category = category
        };

        var data = JsonSerializer.Serialize(payload, _jsonOptions);
        await SendEventToClientAsync(clientId, SseEventTypes.Alert, data, cancellationToken);
    }
}