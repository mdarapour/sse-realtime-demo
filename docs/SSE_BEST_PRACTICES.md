# SSE Best Practices

## Connection Management

### Client-Side Reconnection
```typescript
const eventSource = new EventSource(url);
let retryCount = 0;
const maxRetries = 5;

eventSource.onerror = () => {
  if (retryCount++ < maxRetries) {
    setTimeout(() => eventSource = new EventSource(url), 
      Math.min(1000 * Math.pow(2, retryCount), 30000));
  }
};
```

### Server Keep-Alive
```csharp
// Send heartbeat every 30s to prevent timeout
private void SendHeartbeats(object? state)
{
    var heartbeatEvent = new SseEvent
    {
        Id = Guid.NewGuid().ToString(),
        Event = "heartbeat",
        Data = JsonSerializer.Serialize(new { timestamp = DateTimeOffset.UtcNow })
    };
    SendEventToLocalClients(heartbeatEvent);
}
```

## Event Design

### Typed Events with Validation
```typescript
// Frontend
const MessageSchema = z.object({
  id: z.string(),
  type: z.enum(['notification', 'alert', 'update']),
  data: z.unknown(),
  timestamp: z.string()
});

eventSource.addEventListener('message', (e) => {
  const validated = MessageSchema.parse(JSON.parse(e.data));
  // Process validated event
});
```

```csharp
// Backend
public class TypedSseEvent<T> : SseEvent
{
    public string Type { get; set; }
    public T Payload { get; set; }
    public string Version { get; set; } = "1.0";
}
```

## Scalability

### MongoDB Outbox Pattern
```csharp
// Publish event to outbox for distribution
public async Task PublishEventAsync(SseEvent sseEvent)
{
    var outboxEvent = new SseOutboxEvent
    {
        EventId = sseEvent.Id,
        EventType = sseEvent.Event,
        EventData = sseEvent.Data,
        CreatedAt = DateTime.UtcNow
    };
    await _outboxCollection.InsertOneAsync(outboxEvent);
}

// Background service polls and distributes
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        var events = await GetUnprocessedEvents();
        foreach (var evt in events)
        {
            DeliverEventToLocalClients(evt);
            await MarkAsProcessed(evt);
        }
        await Task.Delay(100, stoppingToken);
    }
}
```

### Kubernetes Configuration
```yaml
# HPA for auto-scaling
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: backend-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: backend
  minReplicas: 3
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
```

## Performance

### Client-Side Deduplication
```typescript
const processedIds = new Set<string>();
const MAX_IDS = 1000;

eventSource.onmessage = (event) => {
  const data = JSON.parse(event.data);
  if (data.messageId && processedIds.has(data.messageId)) return;
  
  processedIds.add(data.messageId);
  if (processedIds.size > MAX_IDS) {
    const idsArray = Array.from(processedIds);
    processedIds.clear();
    idsArray.slice(-MAX_IDS/2).forEach(id => processedIds.add(id));
  }
  
  // Process event
};
```

### Server-Side Filtering
```csharp
private bool ShouldSendEvent(string eventType, string filter)
{
    if (eventType == "connected") return true;
    return string.Equals(filter, eventType, StringComparison.OrdinalIgnoreCase);
}
```

## Security

### API Key Authentication
```csharp
// Query parameter for SSE (headers not supported)
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var apiKey = Request.Query["apikey"].FirstOrDefault() 
                  ?? Request.Headers["X-API-Key"].FirstOrDefault();
                  
        if (!IsValidApiKey(apiKey))
            return AuthenticateResult.Fail("Invalid API key");
            
        var identity = new ClaimsIdentity("ApiKey");
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
```

### CORS Configuration
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("SsePolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowCredentials()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});
```

## Error Handling

### Graceful Degradation
```typescript
class SseService {
  private fallbackTimer?: number;
  
  connect() {
    try {
      this.eventSource = new EventSource(this.url);
      this.setupEventHandlers();
    } catch (error) {
      console.error('SSE not supported, falling back to polling');
      this.startPolling();
    }
  }
  
  private startPolling() {
    this.fallbackTimer = window.setInterval(async () => {
      const events = await fetch(`${this.url}/poll`).then(r => r.json());
      events.forEach(event => this.handleEvent(event));
    }, 5000);
  }
}
```

## Monitoring

### Health Checks
```csharp
app.MapGet("/health/sse", () =>
{
    var status = new
    {
        ConnectedClients = sseService.GetClientCount(),
        EventQueueSize = outboxService.GetQueueSize(),
        LastHeartbeat = sseService.GetLastHeartbeatTime()
    };
    return Results.Ok(status);
});
```

### Metrics Collection
```csharp
// Track key metrics
public class SseMetrics
{
    private readonly IMetrics _metrics;
    
    public void RecordConnection() => _metrics.Increment("sse.connections");
    public void RecordDisconnection() => _metrics.Increment("sse.disconnections");
    public void RecordEventSent(string eventType) => 
        _metrics.Increment("sse.events.sent", new[] { $"type:{eventType}" });
    public void RecordEventFiltered(string reason) => 
        _metrics.Increment("sse.events.filtered", new[] { $"reason:{reason}" });
}
```