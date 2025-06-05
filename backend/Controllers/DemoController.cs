using Microsoft.AspNetCore.Mvc;
using SseDemo.Auth;
using SseDemo.Services;

namespace SseDemo.Controllers;

/// <summary>
/// Demo controller for simulating automatic events
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiKeyAuthorize]
public class DemoController : ControllerBase
{
    private readonly SseMessageService _messageService;
    private readonly ILogger<DemoController> _logger;
    private static Timer? _demoTimer;
    private static readonly object _timerLock = new();

    public DemoController(
        SseMessageService messageService,
        ILogger<DemoController> logger)
    {
        _messageService = messageService;
        _logger = logger;
    }

    /// <summary>
    /// Starts sending demo events on a timer
    /// </summary>
    [HttpPost("start")]
    public IActionResult StartDemo([FromBody] DemoRequest request)
    {
        lock (_timerLock)
        {
            if (_demoTimer != null)
            {
                return BadRequest("Demo is already running. Stop it first.");
            }

            var intervalMs = request.IntervalSeconds * 1000;
            if (intervalMs < 1000 || intervalMs > 60000)
            {
                return BadRequest("Interval must be between 1 and 60 seconds.");
            }

            var eventCount = 0;
            _demoTimer = new Timer(_ =>
            {
                try
                {
                    eventCount++;
                    var eventTypeIndex = eventCount % 4;
                    
                    switch (eventTypeIndex)
                    {
                        case 0:
                            _messageService.SendNotificationToAll(
                                $"Demo notification #{eventCount} at {DateTime.Now:HH:mm:ss}",
                                "info");
                            break;
                        case 1:
                            _messageService.SendAlertToAll(
                                $"Demo alert #{eventCount} - System check at {DateTime.Now:HH:mm:ss}",
                                "medium",
                                "demo");
                            break;
                        case 2:
                            _messageService.SendDataUpdateToAll(
                                $"demo-entity-{eventCount}",
                                "demoEntity",
                                new { 
                                    counter = eventCount, 
                                    timestamp = DateTime.UtcNow,
                                    status = "updated"
                                });
                            break;
                        case 3:
                            _messageService.SendHeartbeatToAll();
                            break;
                    }
                    
                    _logger.LogInformation("Sent demo event #{EventCount}", eventCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending demo event");
                }
            }, null, intervalMs, intervalMs);

            _logger.LogInformation("Started demo timer with {Interval}s interval", request.IntervalSeconds);
            return Ok(new { 
                message = "Demo started successfully", 
                intervalSeconds = request.IntervalSeconds,
                info = "The demo will cycle through notification, alert, dataUpdate, and heartbeat events."
            });
        }
    }

    /// <summary>
    /// Stops the demo timer
    /// </summary>
    [HttpPost("stop")]
    public IActionResult StopDemo()
    {
        lock (_timerLock)
        {
            if (_demoTimer == null)
            {
                return BadRequest("Demo is not running.");
            }

            _demoTimer.Dispose();
            _demoTimer = null;
            _logger.LogInformation("Stopped demo timer");
            return Ok(new { message = "Demo stopped successfully" });
        }
    }

    /// <summary>
    /// Gets the current demo status
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        lock (_timerLock)
        {
            return Ok(new
            {
                isRunning = _demoTimer != null,
                message = _demoTimer != null ? "Demo is running" : "Demo is not running"
            });
        }
    }
}

/// <summary>
/// Request model for demo configuration
/// </summary>
public class DemoRequest
{
    public int IntervalSeconds { get; set; } = 5;
}