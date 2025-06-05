# SSE API Authentication

This SSE implementation uses API key authentication to secure the endpoints.

## Configuration

API keys are configured in `appsettings.json`:

```json
{
  "Authentication": {
    "ApiKeys": [
      "your-api-key-here",
      "another-api-key"
    ]
  }
}
```

## Usage

### Backend

All SSE endpoints are protected by the `[ApiKeyAuthorize]` attribute. The API key can be provided in two ways:

1. **HTTP Header** (preferred for non-SSE endpoints):
   ```
   X-API-Key: your-api-key-here
   ```

2. **Query Parameter** (required for SSE/EventSource):
   ```
   http://localhost:5121/api/sse/connect?apikey=your-api-key-here
   ```

### Frontend

When using the SSE client, provide the API key in the options:

```typescript
const { status, events } = useSse({
  url: 'http://localhost:5121/api/sse/connect',
  clientId: 'my-client',
  apiKey: 'your-api-key-here'
});
```

Or with the SseClient directly:

```typescript
const client = new SseClient({
  url: 'http://localhost:5121/api/sse/connect',
  apiKey: 'your-api-key-here'
});
```

## Security Notes

1. **Never hardcode API keys** in production code
2. Use environment variables or secure configuration management
3. Rotate API keys regularly
4. Use HTTPS in production to prevent key interception
5. Consider implementing rate limiting per API key

## Disabling Authentication (Development Only)

To disable authentication during development, remove the `[ApiKeyAuthorize]` attribute from the controller:

```csharp
// Remove this line to disable auth
[ApiKeyAuthorize]
public class SseController : ControllerBase
```