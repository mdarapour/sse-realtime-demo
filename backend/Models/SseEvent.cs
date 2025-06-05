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
            // Split data by newlines and prefix each line with "data: "
            foreach (var line in Data.Split('\n'))
            {
                builder.Append($"data: {line}\n");
            }
        }
        else
        {
            builder.Append("data: \n");
        }

        builder.Append("\n"); // End the event with an extra newline

        return builder.ToString();
    }
}

