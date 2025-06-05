# Server-Sent Events (SSE) Best Practices Guide

This guide provides comprehensive best practices for implementing Server-Sent Events in production environments, based on the patterns demonstrated in this project.

## Table of Contents

1. [Connection Management](#connection-management)
2. [Event Design](#event-design)
3. [Error Handling and Resilience](#error-handling-and-resilience)
4. [Performance Optimization](#performance-optimization)
5. [Security Considerations](#security-considerations)
6. [Scaling Strategies](#scaling-strategies)
7. [Monitoring and Observability](#monitoring-and-observability)
8. [Testing Strategies](#testing-strategies)

## Connection Management

### Client-Side Best Practices

1. **Implement Reconnection Logic**
   ```typescript
   // Example from useSse.ts
   const connect = () => {
     const eventSource = new EventSource(url);
     
     eventSource.onerror = () => {
       if (retryCount < maxRetryAttempts && autoReconnect) {
         setTimeout(() => {
           connect();
         }, retryTimeout);
       }
     };
   };
   ```

2. **Track Connection State**
   - Maintain connection status (CONNECTING, OPEN, CLOSED, ERROR)
   - Provide visual feedback to users
   - Handle state transitions gracefully

3. **Clean Up Connections**
   ```typescript
   useEffect(() => {
     const client = new SseClient(url, options);
     client.connect();
     
     return () => {
       client.close();
     };
   }, [url]);
   ```

### Server-Side Best Practices

1. **Connection Pooling**
   ```csharp
   // From SseService.cs
   private readonly ConcurrentDictionary<string, ClientConnection> _clients = new();
   
   public void RegisterClient(string clientId, ClientConnection connection)
   {
       _clients.TryAdd(clientId, connection);
   }
   ```

2. **Heartbeat/Keep-Alive**
   ```csharp
   // Send periodic heartbeats to detect stale connections
   private async Task SendHeartbeatAsync()
   {
       await SendEventAsync(new SseEvent
       {
           EventType = ":heartbeat",
           Data = DateTime.UtcNow.ToString("O")
       });
   }
   ```

3. **Graceful Shutdown**
   ```csharp
   public async Task UnregisterClientAsync(string clientId)
   {
       if (_clients.TryRemove(clientId, out var connection))
       {
           await connection.CloseAsync();
       }
   }
   ```

## Event Design

### Event Structure

1. **Use Consistent Event Format**
   ```typescript
   interface SseEvent<T = any> {
     id?: string;
     type: string;
     data: T;
     retry?: number;
   }
   ```

2. **Implement Event Types**
   ```typescript
   // From sseEventTypes.ts
   export enum SseEventType {
     MESSAGE = 'message',
     NOTIFICATION = 'notification',
     ALERT = 'alert',
     DATA_UPDATE = 'dataUpdate',
     CONNECTION = 'connection',
     ERROR = 'error'
   }
   ```

3. **Version Your Events**
   ```typescript
   interface VersionedEvent<T> {
     version: string;
     type: string;
     data: T;
     timestamp: string;
   }
   ```

### Data Serialization

1. **Use JSON for Complex Data**
   ```csharp
   var eventData = JsonSerializer.Serialize(data, new JsonSerializerOptions
   {
       PropertyNamingPolicy = JsonNamingPolicy.CamelCase
   });
   ```

2. **Keep Payloads Small**
   - Compress large data when necessary
   - Send references instead of full objects
   - Implement pagination for lists

3. **Handle Special Characters**
   ```csharp
   // Escape newlines in SSE data
   data = data.Replace("\n", "\ndata: ");
   ```

## Error Handling and Resilience

### Client-Side Error Handling

1. **Exponential Backoff**
   ```typescript
   const backoffDelay = Math.min(
     initialDelay * Math.pow(2, retryCount),
     maxDelay
   );
   ```

2. **Error Classification**
   ```typescript
   eventSource.onerror = (error) => {
     if (eventSource.readyState === EventSource.CONNECTING) {
       // Temporary network issue
     } else if (eventSource.readyState === EventSource.CLOSED) {
       // Server closed connection
     }
   };
   ```

3. **Fallback Mechanisms**
   - Implement polling as fallback
   - Cache last known state
   - Provide offline indicators

### Server-Side Error Handling

1. **Connection Validation**
   ```csharp
   if (!HttpMethods.IsGet(context.Request.Method))
   {
       context.Response.StatusCode = 405;
       return;
   }
   ```

2. **Error Events**
   ```csharp
   await SendEventAsync(new SseEvent
   {
       EventType = "error",
       Data = new { message = "Service temporarily unavailable" }
   });
   ```

3. **Circuit Breaker Pattern**
   - Implement circuit breakers for downstream services
   - Prevent cascading failures
   - Provide graceful degradation

## Performance Optimization

### Client-Side Optimization

1. **Event Deduplication**
   ```typescript
   // From useSse.ts
   const processedIds = useRef(new Set<string>());
   
   if (eventId && processedIds.current.has(eventId)) {
     return; // Skip duplicate
   }
   ```

2. **Throttling and Debouncing**
   ```typescript
   const throttledUpdate = useThrottle((data) => {
     setDisplayData(data);
   }, 100);
   ```

3. **Memory Management**
   ```typescript
   // Limit stored events
   setEvents(prev => [...prev.slice(-MAX_EVENTS), newEvent]);
   ```

### Server-Side Optimization

1. **Async Operations**
   ```csharp
   public async IAsyncEnumerable<SseEvent> GetEventsAsync(
       [EnumeratorCancellation] CancellationToken cancellationToken)
   {
       await foreach (var evt in GetEventStream(cancellationToken))
       {
           yield return evt;
       }
   }
   ```

2. **Connection Limits**
   ```csharp
   private const int MaxConnectionsPerClient = 5;
   
   if (GetConnectionCount(clientId) >= MaxConnectionsPerClient)
   {
       throw new InvalidOperationException("Connection limit exceeded");
   }
   ```

3. **Event Batching**
   ```csharp
   public async Task SendBatchedEventsAsync(IEnumerable<SseEvent> events)
   {
       var batch = events.Take(MaxBatchSize);
       await SendEventsAsync(batch);
   }
   ```

## Security Considerations

### Authentication and Authorization

1. **API Key Authentication**
   ```csharp
   // From ApiKeyAuthenticationHandler.cs
   if (!request.Headers.TryGetValue("X-Api-Key", out var apiKey))
   {
       return AuthenticateResult.Fail("Missing API Key");
   }
   ```

2. **Token-Based Auth**
   ```typescript
   const eventSource = new EventSource(url, {
     headers: {
       'Authorization': `Bearer ${token}`
     }
   });
   ```

3. **CORS Configuration**
   ```csharp
   app.UseCors(builder => builder
       .WithOrigins("https://trusted-domain.com")
       .AllowAnyMethod()
       .AllowAnyHeader()
       .AllowCredentials());
   ```

### Data Protection

1. **Sanitize User Input**
   ```csharp
   var sanitizedData = HttpUtility.HtmlEncode(userInput);
   ```

2. **Validate Event Data**
   ```typescript
   // From sseSchemas.ts
   const schema = z.object({
     type: z.enum(['notification', 'alert']),
     data: z.object({
       message: z.string().max(1000)
     })
   });
   ```

3. **Rate Limiting**
   ```csharp
   services.AddRateLimiter(options =>
   {
       options.AddFixedWindowLimiter("sse", options =>
       {
           options.Window = TimeSpan.FromMinutes(1);
           options.PermitLimit = 100;
       });
   });
   ```

## Scaling Strategies

### Horizontal Scaling

1. **Stateless Design**
   - Use external stores (Redis, MongoDB) for state
   - Avoid server affinity requirements
   - Implement event distribution patterns

2. **Load Balancing**
   ```yaml
   # From ingress.yaml
   annotations:
     nginx.ingress.kubernetes.io/proxy-buffering: "off"
     nginx.ingress.kubernetes.io/proxy-read-timeout: "86400"
   ```

3. **Connection Distribution**
   - Use consistent hashing for client routing
   - Implement connection limits per instance
   - Monitor connection density

### Vertical Scaling

1. **Resource Optimization**
   ```yaml
   # From backend.yaml
   resources:
     requests:
       memory: "128Mi"
       cpu: "100m"
     limits:
       memory: "512Mi"
       cpu: "500m"
   ```

2. **Connection Pooling**
   - Tune connection pool sizes
   - Monitor resource utilization
   - Implement backpressure mechanisms

### Geographic Distribution

1. **Multi-Region Deployment**
   - Deploy SSE services close to users
   - Use geo-routing for optimal latency
   - Implement cross-region event propagation

2. **Edge Caching**
   - Cache static resources at edge
   - Use CDN for initial connection setup
   - Implement regional failover

## Monitoring and Observability

### Key Metrics

1. **Connection Metrics**
   ```csharp
   // Track active connections
   _metrics.Gauge("sse_active_connections", _clients.Count);
   
   // Track connection duration
   _metrics.Histogram("sse_connection_duration_seconds", duration);
   ```

2. **Event Metrics**
   ```csharp
   // Event throughput
   _metrics.Counter("sse_events_sent_total", 1, 
       new[] { "event_type", eventType });
   
   // Event size
   _metrics.Histogram("sse_event_size_bytes", eventSize);
   ```

3. **Error Metrics**
   ```csharp
   // Connection errors
   _metrics.Counter("sse_connection_errors_total", 1,
       new[] { "error_type", errorType });
   ```

### Logging Best Practices

1. **Structured Logging**
   ```csharp
   _logger.LogInformation("SSE connection established",
       new { ClientId = clientId, Timestamp = DateTime.UtcNow });
   ```

2. **Correlation IDs**
   ```csharp
   using (_logger.BeginScope(new { CorrelationId = correlationId }))
   {
       // All logs within scope include correlation ID
   }
   ```

3. **Log Levels**
   - DEBUG: Connection lifecycle events
   - INFO: Successful operations
   - WARN: Recoverable errors
   - ERROR: Failures requiring attention

### Distributed Tracing

1. **Trace Event Flow**
   ```csharp
   using var activity = Activity.StartActivity("SendSseEvent");
   activity?.SetTag("event.type", eventType);
   activity?.SetTag("client.id", clientId);
   ```

2. **Cross-Service Correlation**
   - Propagate trace contexts
   - Link frontend and backend spans
   - Track end-to-end latency

## Testing Strategies

### Unit Testing

1. **Mock EventSource**
   ```typescript
   class MockEventSource {
     constructor(url: string) {
       this.url = url;
       this.readyState = EventSource.OPEN;
     }
     
     simulateMessage(data: string) {
       this.onmessage?.({ data } as MessageEvent);
     }
   }
   ```

2. **Test Reconnection Logic**
   ```typescript
   it('should reconnect after error', async () => {
     const client = new SseClient(url, { maxRetryAttempts: 3 });
     client.simulateError();
     
     expect(client.retryCount).toBe(1);
     await waitFor(() => expect(client.readyState).toBe('OPEN'));
   });
   ```

### Integration Testing

1. **Test Full SSE Flow**
   ```csharp
   [Test]
   public async Task Should_Receive_Events()
   {
       var client = new TestSseClient("/api/sse/connect");
       await client.ConnectAsync();
       
       await _service.BroadcastAsync(new TestEvent());
       
       var received = await client.WaitForEventAsync();
       Assert.NotNull(received);
   }
   ```

2. **Load Testing**
   ```bash
   # Using k6 for SSE load testing
   k6 run --vus 1000 --duration 30s sse-load-test.js
   ```

### End-to-End Testing

1. **Browser Automation**
   ```typescript
   // Using Playwright
   const eventPromise = page.waitForEvent('console');
   await page.goto('http://localhost:5173');
   const msg = await eventPromise;
   expect(msg.text()).toContain('SSE connected');
   ```

2. **Chaos Testing**
   - Simulate network failures
   - Test connection limits
   - Verify graceful degradation

## Conclusion

These best practices are demonstrated throughout this project's implementation. By following these guidelines, you can build robust, scalable, and maintainable SSE applications suitable for production environments.

Remember that SSE is just one tool in the real-time communication toolbox. Always evaluate whether SSE is the right choice for your specific use case, considering factors like:

- Unidirectional vs bidirectional communication needs
- Message frequency and size
- Browser compatibility requirements
- Infrastructure constraints
- Team expertise

For more examples and implementation details, explore the source code in this repository.