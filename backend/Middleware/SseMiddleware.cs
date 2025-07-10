using System.Net;
using SseDemo.Helpers;
using SseDemo.Models;
using SseDemo.Services;

namespace SseDemo.Middleware;

/// <summary>
/// Middleware for handling Server-Sent Events
/// </summary>
public class SseMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SseMiddleware> _logger;

    public SseMiddleware(RequestDelegate next, ILogger<SseMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISseService sseService)
    {
        if (!context.Request.Headers.Accept.ToString().Contains("text/event-stream"))
        {
            await _next(context);
            return;
        }

        // Get client ID from query string or generate a new one
        var clientId = context.Request.Query.ContainsKey("clientId")
            ? context.Request.Query["clientId"].ToString()
            : Guid.NewGuid().ToString();

        // Get optional event filter
        var filter = context.Request.Query.ContainsKey("filter")
            ? context.Request.Query["filter"].ToString()
            : null;

        // Set up the response for SSE
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        SseConnectionHelper.SetupSseResponseHeaders(context.Response.Headers);

        // Ensure response buffering is disabled
        await context.Response.Body.FlushAsync();

        // Use the helper to handle the SSE connection
        await SseConnectionHelper.HandleSseConnectionAsync(
            clientId,
            filter,
            null, // checkpoint - middleware doesn't support this, use controller endpoint instead
            null, // lastEventId - middleware doesn't support this, use controller endpoint instead
            sseService,
            _logger,
            (eventString, token) => context.Response.WriteAsync(eventString, token),
            (token) => context.Response.Body.FlushAsync(token),
            context.RequestAborted);
    }
}

/// <summary>
/// Extension methods for the SSE middleware
/// </summary>
public static class SseMiddlewareExtensions
{
    public static IApplicationBuilder UseSse(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SseMiddleware>();
    }
}
