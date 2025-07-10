using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SseDemo.Outbox.Models;

/// <summary>
/// Represents a sequence counter for generating monotonic sequence numbers
/// </summary>
public class SequenceCounter
{
    [BsonId]
    public string Id { get; set; } = "event_sequence";
    
    /// <summary>
    /// The current sequence value
    /// </summary>
    public long CurrentValue { get; set; }
    
    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}