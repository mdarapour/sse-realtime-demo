using Microsoft.AspNetCore.Mvc;
using SseDemo.Application.Services;
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
    private readonly IConnectionManagementService _connectionService;
    private readonly IEventBroadcastService _broadcastService;
    private readonly ILogger<SseController> _logger;

    public SseController(
        IConnectionManagementService connectionService,
        IEventBroadcastService broadcastService,
        ILogger<SseController> logger)
    {
        _connectionService = connectionService;
        _broadcastService = broadcastService;
        _logger = logger;
    }

    /// <summary>
    /// Endpoint for SSE connection
    /// This is an alternative to using the middleware approach
    /// </summary>
    [HttpGet("connect")]
    public async Task Connect(
        string? clientId = null, 
        string? filter = null, 
        long? checkpoint = null,
        string? lastEventId = null,
        CancellationToken cancellationToken = default)
    {
        // Generate client ID if not provided
        clientId ??= Guid.NewGuid().ToString();

        // Check for Last-Event-ID header (sent by browser on reconnection)
        if (string.IsNullOrEmpty(lastEventId) && Request.Headers.ContainsKey("Last-Event-ID"))
        {
            lastEventId = Request.Headers["Last-Event-ID"];
            _logger.LogInformation("Received Last-Event-ID header: {LastEventId} for client {ClientId}", lastEventId, clientId);
        }

        // Set up the response for SSE
        SseConnectionHelper.SetupSseResponseHeaders(Response.Headers);

        // Establish SSE connection through the service layer
        await foreach (var eventData in _connectionService.EstablishConnectionAsync(
            clientId, filter, checkpoint, lastEventId, cancellationToken))
        {
            await Response.WriteAsync(eventData, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Sends a custom event to all connected clients
    /// </summary>
    [HttpPost("broadcast")]
    public async Task<IActionResult> Broadcast([FromBody] BroadcastRequest request)
    {
        if (string.IsNullOrEmpty(request.Data))
        {
            return BadRequest("Data is required");
        }

        await _broadcastService.BroadcastEventAsync(
            request.EventType ?? SseEventTypes.Message,
            request.Data);

        return Ok(new { message = "Event broadcasted successfully" });
    }

    /// <summary>
    /// Sends a custom event to a specific client
    /// </summary>
    [HttpPost("send/{clientId}")]
    public async Task<IActionResult> SendToClient(string clientId, [FromBody] BroadcastRequest request)
    {
        if (string.IsNullOrEmpty(request.Data))
        {
            return BadRequest("Data is required");
        }

        await _broadcastService.SendEventToClientAsync(
            clientId,
            request.EventType ?? SseEventTypes.Message,
            request.Data);

        return Ok(new { message = $"Event sent to client {clientId} successfully" });
    }

    /// <summary>
    /// Sends a notification to all connected clients
    /// </summary>
    [HttpPost("notification")]
    public async Task<IActionResult> SendNotification([FromBody] NotificationRequest request)
    {
        if (string.IsNullOrEmpty(request.Message))
        {
            return BadRequest("Message is required");
        }

        await _broadcastService.BroadcastNotificationAsync(
            request.Message, 
            request.Severity ?? "info");

        return Ok(new { message = "Notification sent successfully" });
    }

    /// <summary>
    /// Sends a notification to a specific client
    /// </summary>
    [HttpPost("notification/{clientId}")]
    public async Task<IActionResult> SendNotificationToClient(string clientId, [FromBody] NotificationRequest request)
    {
        if (string.IsNullOrEmpty(request.Message))
        {
            return BadRequest("Message is required");
        }

        await _broadcastService.SendNotificationToClientAsync(
            clientId,
            request.Message, 
            request.Severity ?? "info");

        return Ok(new { message = $"Notification sent to client {clientId} successfully" });
    }

    /// <summary>
    /// Sends a data update to all connected clients
    /// </summary>
    [HttpPost("data-update")]
    public async Task<IActionResult> SendDataUpdate([FromBody] DataUpdateRequest request)
    {
        if (string.IsNullOrEmpty(request.EntityId) || string.IsNullOrEmpty(request.EntityType))
        {
            return BadRequest("EntityId and EntityType are required");
        }

        await _broadcastService.BroadcastDataUpdateAsync(
            request.EntityId, 
            request.EntityType, 
            request.Changes ?? new {});

        return Ok(new { message = "Data update sent successfully" });
    }

    /// <summary>
    /// Sends a data update to a specific client
    /// </summary>
    [HttpPost("data-update/{clientId}")]
    public async Task<IActionResult> SendDataUpdateToClient(string clientId, [FromBody] DataUpdateRequest request)
    {
        if (string.IsNullOrEmpty(request.EntityId) || string.IsNullOrEmpty(request.EntityType))
        {
            return BadRequest("EntityId and EntityType are required");
        }

        await _broadcastService.SendDataUpdateToClientAsync(
            clientId,
            request.EntityId, 
            request.EntityType, 
            request.Changes ?? new {});

        return Ok(new { message = $"Data update sent to client {clientId} successfully" });
    }

    /// <summary>
    /// Sends an alert to all connected clients
    /// </summary>
    [HttpPost("alert")]
    public async Task<IActionResult> SendAlert([FromBody] AlertRequest request)
    {
        if (string.IsNullOrEmpty(request.Message))
        {
            return BadRequest("Message is required");
        }

        await _broadcastService.BroadcastAlertAsync(
            request.Message, 
            request.Severity ?? "high", 
            request.Category ?? "system");

        return Ok(new { message = "Alert sent successfully" });
    }

    /// <summary>
    /// Sends an alert to a specific client
    /// </summary>
    [HttpPost("alert/{clientId}")]
    public async Task<IActionResult> SendAlertToClient(string clientId, [FromBody] AlertRequest request)
    {
        if (string.IsNullOrEmpty(request.Message))
        {
            return BadRequest("Message is required");
        }

        await _broadcastService.SendAlertToClientAsync(
            clientId,
            request.Message, 
            request.Severity ?? "high", 
            request.Category ?? "system");

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
