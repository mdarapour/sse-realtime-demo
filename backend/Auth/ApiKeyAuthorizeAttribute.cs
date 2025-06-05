using Microsoft.AspNetCore.Authorization;

namespace SseDemo.Auth;

/// <summary>
/// Authorization attribute that requires API key authentication
/// </summary>
public class ApiKeyAuthorizeAttribute : AuthorizeAttribute
{
    public ApiKeyAuthorizeAttribute()
    {
        AuthenticationSchemes = ApiKeyAuthenticationExtensions.SchemeName;
    }
}