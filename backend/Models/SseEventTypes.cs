namespace SseDemo.Models;

/// <summary>
/// Shared event types for Server-Sent Events
/// This class defines all valid event types that can be used in the application
/// Both frontend and backend should use these constants to ensure consistency
/// </summary>
public static class SseEventTypes
{
    /// <summary>
    /// Default message event type
    /// </summary>
    public const string Message = "message";

    /// <summary>
    /// Notification event type for user notifications
    /// </summary>
    public const string Notification = "notification";

    /// <summary>
    /// Data update event type for entity changes
    /// </summary>
    public const string DataUpdate = "dataUpdate";

    /// <summary>
    /// Alert event type for important alerts
    /// </summary>
    public const string Alert = "alert";

    /// <summary>
    /// Heartbeat event type to keep connections alive
    /// </summary>
    public const string Heartbeat = "heartbeat";

    /// <summary>
    /// Connected event type sent when a client connects
    /// </summary>
    public const string Connected = "connected";

    /// <summary>
    /// Validates if the provided event type is a valid SSE event type
    /// </summary>
    /// <param name="eventType">The event type to validate</param>
    /// <returns>True if the event type is valid, false otherwise</returns>
    public static bool IsValidEventType(string eventType)
    {
        return eventType switch
        {
            Message => true,
            Notification => true,
            DataUpdate => true,
            Alert => true,
            Heartbeat => true,
            Connected => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets all valid event types
    /// </summary>
    /// <returns>Array of all valid event types</returns>
    public static string[] GetAllEventTypes()
    {
        return new[]
        {
            Message,
            Notification,
            DataUpdate,
            Alert,
            Heartbeat,
            Connected
        };
    }
}
