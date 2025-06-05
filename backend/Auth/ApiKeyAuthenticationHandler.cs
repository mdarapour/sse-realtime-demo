using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SseDemo.Auth;

/// <summary>
/// Authentication handler for API key authentication
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private const string ApiKeyQueryName = "apikey";
    private readonly IConfiguration _configuration;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Try to get API key from header first
        string? providedApiKey = null;
        
        if (Request.Headers.TryGetValue(ApiKeyHeaderName, out var headerApiKey))
        {
            providedApiKey = headerApiKey.FirstOrDefault();
        }
        // For SSE connections, also check query string since headers might not work
        else if (Request.Query.TryGetValue(ApiKeyQueryName, out var queryApiKey))
        {
            providedApiKey = queryApiKey.FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // Get the configured API keys
        var validApiKeys = _configuration.GetSection("Authentication:ApiKeys").Get<string[]>() ?? Array.Empty<string>();
        
        if (validApiKeys.Length == 0)
        {
            Logger.LogWarning("No API keys configured. Authentication will fail.");
            return Task.FromResult(AuthenticateResult.Fail("No API keys configured"));
        }

        // Validate the API key
        if (!validApiKeys.Contains(providedApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
        }

        // Create claims for the authenticated user
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "ApiKeyUser"),
            new Claim("ApiKey", providedApiKey.Substring(0, 4) + "...")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.Headers["WWW-Authenticate"] = $"ApiKey realm=\"{Options.Realm}\", charset=\"UTF-8\"";
        await Response.WriteAsync("Unauthorized. Please provide a valid API key.");
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 403;
        await Response.WriteAsync("Forbidden. You do not have access to this resource.");
    }
}

/// <summary>
/// Options for API key authentication
/// </summary>
public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public string Realm { get; set; } = "SSE API";
}

/// <summary>
/// Extension methods for API key authentication
/// </summary>
public static class ApiKeyAuthenticationExtensions
{
    public const string SchemeName = "ApiKey";

    public static AuthenticationBuilder AddApiKeyAuthentication(
        this AuthenticationBuilder builder,
        Action<ApiKeyAuthenticationOptions>? configureOptions = null)
    {
        return builder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
            SchemeName, configureOptions);
    }
}