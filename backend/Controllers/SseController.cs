using Microsoft.AspNetCore.Mvc;
using SseDemo.Auth;
using SseDemo.Helpers;
using SseDemo.Models;
using SseDemo.Services;
using System.Text.Json;

namespace SseDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiKeyAuthorize]
public class SseController : ControllerBase
{
    private readonly ISseService _sseService;
    private readonly SseMessageService _messageService;
    private readonly ILogger<SseController> _logger;

    public SseController(
        ISseService sseService,
        SseMessageService messageService,
        ILogger<SseController> logger)
    {
        _sseService = sseService;
        _messageService = messageService;
        _logger = logger;
    }

    /// <summary>
    /// Endpoint for SSE connection
    /// This is an alternative to using the middleware approach
    /// </summary>
    [HttpGet("connect")]
    public async Task Connect(string? clientId = null, string? filter = null, CancellationToken cancellationToken = default)
    {
        // Generate client ID if not provided
        clientId ??= Guid.NewGuid().ToString();

        // Set up the response for SSE
        SseConnectionHelper.SetupSseResponseHeaders(Response.Headers);

        // Use the helper to handle the SSE connection
        await SseConnectionHelper.HandleSseConnectionAsync(
            clientId,
            filter,
            _sseService,
            _logger,
            (eventString, token) => Response.WriteAsync(eventString, token),
            (token) => Response.Body.FlushAsync(token),
            cancellationToken);
    }

    /// <summary>
    /// Sends a custom event to all connected clients
    /// </summary>
    [HttpPost("broadcast")]
    public IActionResult Broadcast([FromBody] BroadcastRequest request)
    {
        if (string.IsNullOrEmpty(request.Data))
        {
            return BadRequest("Data is required");
        }

        var sseEvent = new SseEvent
        {
            Id = Guid.NewGuid().ToString(),
            Event = request.EventType ?? SseEventTypes.Message,
            Data = request.Data
        };

        _sseService.SendEventToAll(sseEvent);

        return Ok(new { message = "Event broadcasted successfully" });
    }

    /// <summary>
    /// Sends a custom event to a specific client
    /// </summary>
    [HttpPost("send/{clientId}")]
    public IActionResult SendToClient(string clientId, [FromBody] BroadcastRequest request)
    {
        if (string.IsNullOrEmpty(request.Data))
        {
            return BadRequest("Data is required");
        }

        var sseEvent = new SseEvent
        {
            Id = Guid.NewGuid().ToString(),
            Event = request.EventType ?? SseEventTypes.Message,
            Data = request.Data
        };

        _sseService.SendEventToClient(clientId, sseEvent);

        return Ok(new { message = $"Event sent to client {clientId} successfully" });
    }

    /// <summary>
    /// Sends a notification to all connected clients
    /// </summary>
    [HttpPost("notification")]
    public IActionResult SendNotification([FromBody] NotificationRequest request)
    {
        if (string.IsNullOrEmpty(request.Message))
        {
            return BadRequest("Message is required");
        }

        _messageService.SendNotificationToAll(request.Message, request.Severity ?? "info");

        return Ok(new { message = "Notification sent successfully" });
    }

    /// <summary>
    /// Sends a notification to a specific client
    /// </summary>
    [HttpPost("notification/{clientId}")]
    public IActionResult SendNotificationToClient(string clientId, [FromBody] NotificationRequest request)
    {
        if (string.IsNullOrEmpty(request.Message))
        {
            return BadRequest("Message is required");
        }

        _messageService.SendNotificationToClient(clientId, request.Message, request.Severity ?? "info");

        return Ok(new { message = $"Notification sent to client {clientId} successfully" });
    }

    /// <summary>
    /// Sends a data update to all connected clients
    /// </summary>
    [HttpPost("data-update")]
    public IActionResult SendDataUpdate([FromBody] DataUpdateRequest request)
    {
        if (string.IsNullOrEmpty(request.EntityId) || string.IsNullOrEmpty(request.EntityType))
        {
            return BadRequest("EntityId and EntityType are required");
        }

        _messageService.SendDataUpdateToAll(request.EntityId, request.EntityType, request.Changes ?? new {});

        return Ok(new { message = "Data update sent successfully" });
    }

    /// <summary>
    /// Sends a data update to a specific client
    /// </summary>
    [HttpPost("data-update/{clientId}")]
    public IActionResult SendDataUpdateToClient(string clientId, [FromBody] DataUpdateRequest request)
    {
        if (string.IsNullOrEmpty(request.EntityId) || string.IsNullOrEmpty(request.EntityType))
        {
            return BadRequest("EntityId and EntityType are required");
        }

        _messageService.SendDataUpdateToClient(clientId, request.EntityId, request.EntityType, request.Changes ?? new {});

        return Ok(new { message = $"Data update sent to client {clientId} successfully" });
    }

    /// <summary>
    /// Sends an alert to all connected clients
    /// </summary>
    [HttpPost("alert")]
    public IActionResult SendAlert([FromBody] AlertRequest request)
    {
        if (string.IsNullOrEmpty(request.Message))
        {
            return BadRequest("Message is required");
        }

        _messageService.SendAlertToAll(request.Message, request.Severity ?? "high", request.Category ?? "system");

        return Ok(new { message = "Alert sent successfully" });
    }

    /// <summary>
    /// Sends an alert to a specific client
    /// </summary>
    [HttpPost("alert/{clientId}")]
    public IActionResult SendAlertToClient(string clientId, [FromBody] AlertRequest request)
    {
        if (string.IsNullOrEmpty(request.Message))
        {
            return BadRequest("Message is required");
        }

        _messageService.SendAlertToClient(clientId, request.Message, request.Severity ?? "high", request.Category ?? "system");

        return Ok(new { message = $"Alert sent to client {clientId} successfully" });
    }
}

/// <summary>
/// Request model for broadcasting events
/// </summary>
public class BroadcastRequest
{
    public string? EventType { get; set; }
    public string? Data { get; set; }
}

/// <summary>
/// Request model for sending notifications
/// </summary>
public class NotificationRequest
{
    public string Message { get; set; } = string.Empty;
    public string? Severity { get; set; }
}

/// <summary>
/// Request model for sending data updates
/// </summary>
public class DataUpdateRequest
{
    public string EntityId { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public object? Changes { get; set; }
}

/// <summary>
/// Request model for sending alerts
/// </summary>
public class AlertRequest
{
    public string Message { get; set; } = string.Empty;
    public string? Severity { get; set; }
    public string? Category { get; set; }
}
