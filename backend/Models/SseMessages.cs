using System.Text.Json;
using System.Text.Json.Serialization;

namespace SseDemo.Models;

/// <summary>
/// Base class for all SSE message payloads
/// </summary>
public abstract class BaseEventPayload
{
    /// <summary>
    /// Unique identifier for the message
    /// </summary>
    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Timestamp when the message was created (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Schema version for backward compatibility
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Message type discriminator
    /// </summary>
    [JsonPropertyName("type")]
    public abstract string Type { get; }
}

/// <summary>
/// Notification message payload
/// </summary>
public class NotificationPayload : BaseEventPayload
{
    /// <summary>
    /// Type discriminator for notifications
    /// </summary>
    public override string Type => "notification";

    /// <summary>
    /// The notification message text
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Severity level of the notification
    /// </summary>
    public string Severity { get; set; } = "info";
}

/// <summary>
/// Data update message payload
/// </summary>
public class DataUpdatePayload : BaseEventPayload
{
    /// <summary>
    /// Type discriminator for data updates
    /// </summary>
    public override string Type => "dataUpdate";

    /// <summary>
    /// ID of the entity that was updated
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Type of the entity that was updated
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Changes made to the entity
    /// </summary>
    public JsonElement Changes { get; set; }
}

/// <summary>
/// Alert message payload for urgent notifications
/// </summary>
public class AlertPayload : BaseEventPayload
{
    /// <summary>
    /// Type discriminator for alerts
    /// </summary>
    public override string Type => "alert";

    /// <summary>
    /// The alert message text
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Severity level of the alert (critical, high, medium, low)
    /// </summary>
    public string Severity { get; set; } = "high";

    /// <summary>
    /// Category or type of alert (security, system, performance, etc.)
    /// </summary>
    public string Category { get; set; } = "system";
}

/// <summary>
/// Heartbeat message payload to keep the connection alive
/// </summary>
public class HeartbeatPayload : BaseEventPayload
{
    /// <summary>
    /// Type discriminator for heartbeats
    /// </summary>
    public override string Type => "heartbeat";
}

/// <summary>
/// Static class for message type constants
/// </summary>
[Obsolete("Use SseEventTypes directly instead for consistent event type naming")]
public static class MessageTypes
{
    // Use the shared event types from SseEventTypes
    public const string Notification = SseEventTypes.Notification;
    public const string DataUpdate = SseEventTypes.DataUpdate;
    public const string Heartbeat = SseEventTypes.Heartbeat;
}
