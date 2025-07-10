namespace SseDemo.Models;

/// <summary>
/// Represents a Server-Sent Event
/// </summary>
public class SseEvent
{
    /// <summary>
    /// Gets or sets the event ID.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    public string? Event { get; set; }

    /// <summary>
    /// Gets or sets the event data.
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// Gets or sets the retry interval in milliseconds.
    /// </summary>
    public int? Retry { get; set; }

    /// <summary>
    /// Gets or sets the sequence number for event ordering.
    /// </summary>
    public long? SequenceNumber { get; set; }

    /// <summary>
    /// Formats the event according to the SSE specification.
    /// </summary>
    /// <returns>A string representation of the event.</returns>
    public string Format()
    {
        var builder = new System.Text.StringBuilder();

        if (!string.IsNullOrEmpty(Id))
        {
            builder.Append($"id: {Id}\n");
        }

        if (!string.IsNullOrEmpty(Event))
        {
            builder.Append($"event: {Event}\n");
        }

        if (Retry.HasValue)
        {
            builder.Append($"retry: {Retry.Value}\n");
        }

        if (!string.IsNullOrEmpty(Data))
        {
            // If we have a sequence number, include it in the data as JSON metadata
            if (SequenceNumber.HasValue)
            {
                // Parse existing data as JSON and add sequence number
                try
                {
                    var jsonData = System.Text.Json.JsonDocument.Parse(Data);
                    var root = jsonData.RootElement;
                    
                    using var stream = new System.IO.MemoryStream();
                    using var writer = new System.Text.Json.Utf8JsonWriter(stream);
                    
                    writer.WriteStartObject();
                    writer.WriteNumber("_sequence", SequenceNumber.Value);
                    
                    // Copy existing properties
                    foreach (var property in root.EnumerateObject())
                    {
                        property.WriteTo(writer);
                    }
                    
                    writer.WriteEndObject();
                    writer.Flush();
                    
                    var modifiedData = System.Text.Encoding.UTF8.GetString(stream.ToArray());
                    foreach (var line in modifiedData.Split('\n'))
                    {
                        builder.Append($"data: {line}\n");
                    }
                }
                catch
                {
                    // If data is not JSON, append sequence as separate data line
                    builder.Append($"data: {{\"_sequence\":{SequenceNumber.Value}}}\n");
                    foreach (var line in Data.Split('\n'))
                    {
                        builder.Append($"data: {line}\n");
                    }
                }
            }
            else
            {
                // No sequence number, just output data as before
                foreach (var line in Data.Split('\n'))
                {
                    builder.Append($"data: {line}\n");
                }
            }
        }
        else
        {
            if (SequenceNumber.HasValue)
            {
                builder.Append($"data: {{\"_sequence\":{SequenceNumber.Value}}}\n");
            }
            else
            {
                builder.Append("data: \n");
            }
        }

        builder.Append("\n"); // End the event with an extra newline

        return builder.ToString();
    }
}

