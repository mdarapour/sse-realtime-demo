using SseDemo.Models;
using SseDemo.Services;

namespace SseDemo.Helpers;

/// <summary>
/// Helper class for handling SSE connections
/// </summary>
public static class SseConnectionHelper
{
    /// <summary>
    /// Handles an SSE connection by streaming events to the client
    /// </summary>
    /// <param name="clientId">Client identifier</param>
    /// <param name="filter">Optional event filter</param>
    /// <param name="sseService">SSE service</param>
    /// <param name="logger">Logger</param>
    /// <param name="writeEvent">Function to write an event to the response</param>
    /// <param name="flushResponse">Function to flush the response</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public static async Task HandleSseConnectionAsync(
        string clientId,
        string? filter,
        ISseService sseService,
        ILogger logger,
        Func<string, CancellationToken, Task> writeEvent,
        Func<CancellationToken, Task> flushResponse,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("SSE connection requested for client {ClientId} with filter {Filter}", clientId, filter ?? "none");

        try
        {
            // Register the client with the filter
            var clientCts = sseService.RegisterClient(clientId, filter);

            try
            {
                // Stream SSE events to the client
                await foreach (var sseEvent in sseService.GetSseEventsAsync(clientId, cancellationToken))
                {
                    var eventString = sseEvent.Format();
                    await writeEvent(eventString, cancellationToken);
                    await flushResponse(cancellationToken);
                }
            }
            finally
            {
                // Ensure the client is unregistered when the connection ends
                sseService.UnregisterClient(clientId);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("SSE connection closed for client {ClientId}", clientId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in SSE connection for client {ClientId}", clientId);
        }
    }

    /// <summary>
    /// Sets up the HTTP response headers for SSE
    /// </summary>
    /// <param name="headers">HTTP response headers</param>
    public static void SetupSseResponseHeaders(IHeaderDictionary headers)
    {
        headers["Content-Type"] = "text/event-stream";
        headers["Cache-Control"] = "no-cache";
        headers["Connection"] = "keep-alive";
    }
}
