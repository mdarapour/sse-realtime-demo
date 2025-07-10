using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SseDemo.Outbox.Models;

/// <summary>
/// Represents an SSE event stored in the MongoDB outbox
/// </summary>
public class SseOutboxEvent
{
    [BsonId]
    public ObjectId Id { get; set; }
    
    /// <summary>
    /// The SSE event ID
    /// </summary>
    public string EventId { get; set; } = string.Empty;
    
    /// <summary>
    /// Global sequence number for event ordering
    /// </summary>
    [BsonElement("seq")]
    public long SequenceNumber { get; set; }
    
    /// <summary>
    /// The event type (e.g., "message", "notification", "dataUpdate")
    /// </summary>
    public string EventType { get; set; } = string.Empty;
    
    /// <summary>
    /// The serialized event data
    /// </summary>
    public string EventData { get; set; } = string.Empty;
    
    /// <summary>
    /// Target client ID if this is a targeted event, null for broadcasts
    /// </summary>
    public string? TargetClientId { get; set; }
    
    /// <summary>
    /// Client filter if any
    /// </summary>
    public string? ClientFilter { get; set; }
    
    /// <summary>
    /// When the event was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the event was processed (null if not yet processed)
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
    
    /// <summary>
    /// Which pod/instance processed this event
    /// </summary>
    public string? ProcessedBy { get; set; }
    
    /// <summary>
    /// TTL for automatic cleanup (default: 1 hour)
    /// </summary>
    [BsonElement("ttl")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime Ttl { get; set; } = DateTime.UtcNow.AddHours(1);
}