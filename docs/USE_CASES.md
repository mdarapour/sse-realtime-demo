# SSE Use Cases

## 1. Live Dashboard Updates

```typescript
// Frontend
const { events } = useSse({ 
  url: '/api/sse/connect',
  filter: 'metrics'
});

return (
  <Dashboard>
    {events.map(e => <MetricCard key={e.id} data={JSON.parse(e.data)} />)}
  </Dashboard>
);
```

```csharp
// Backend - Send metrics updates
public async Task SendMetricsUpdate()
{
    var metrics = await _metricsService.GetCurrentMetrics();
    var sseEvent = new SseEvent
    {
        Event = "metrics",
        Data = JsonSerializer.Serialize(metrics)
    };
    _sseService.SendEventToAll(sseEvent);
}
```

## 2. Real-Time Notifications

```typescript
// Frontend with typed events
interface NotificationEvent {
  id: string;
  title: string;
  message: string;
  severity: 'info' | 'warning' | 'error';
}

const { notification } = useSseEvents<{
  notification: NotificationEvent;
}>({ url: '/api/sse/connect' }, ['notification']);

// Display notifications
notification.forEach(n => toast(n.message, { type: n.severity }));
```

```csharp
// Backend - Send targeted notifications
_messageService.SendNotificationToClient(
    clientId: userId,
    message: "Your order has been shipped",
    severity: "info"
);
```

## 3. Progress Tracking

```typescript
// Frontend progress component
const ProgressTracker: React.FC<{ taskId: string }> = ({ taskId }) => {
  const { events } = useSse({ 
    url: '/api/sse/connect',
    filter: `progress-${taskId}`
  });
  
  const latest = events[events.length - 1];
  const progress = latest ? JSON.parse(latest.data) : { percent: 0 };
  
  return <ProgressBar value={progress.percent} status={progress.status} />;
};
```

```csharp
// Backend - Report progress
public async Task ReportProgress(string taskId, int percent, string status)
{
    var progressEvent = new SseEvent
    {
        Event = $"progress-{taskId}",
        Data = JsonSerializer.Serialize(new { percent, status, taskId })
    };
    _sseService.SendEventToAll(progressEvent);
}
```

## 4. Collaborative Features

```typescript
// Shared cursor positions
interface CursorEvent {
  userId: string;
  x: number;
  y: number;
  color: string;
}

const { cursor } = useSseEvents<{ cursor: CursorEvent }>({ 
  url: '/api/sse/connect',
  clientId: documentId 
}, ['cursor']);

// Render other users' cursors
return (
  <>
    {cursor.map(c => (
      <Cursor key={c.userId} x={c.x} y={c.y} color={c.color} />
    ))}
  </>
);
```

## 5. System Monitoring

```csharp
// Health monitoring with SSE
public class HealthMonitorService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var health = new
            {
                cpu = await GetCpuUsage(),
                memory = await GetMemoryUsage(),
                activeConnections = _sseService.GetConnectionCount(),
                timestamp = DateTime.UtcNow
            };
            
            _sseService.SendEventToAll(new SseEvent
            {
                Event = "system-health",
                Data = JsonSerializer.Serialize(health)
            });
            
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
```

## 6. Live Feed Updates

```typescript
// Auto-updating feed
const NewsFeed: React.FC = () => {
  const { events, clearEvents } = useSse({ 
    url: '/api/sse/connect',
    filter: 'news',
    maxEvents: 50,
    autoClearOldEvents: true
  });
  
  return (
    <Feed>
      {events.map(e => {
        const article = JSON.parse(e.data);
        return <Article key={article.id} {...article} />;
      })}
    </Feed>
  );
};
```

## Event Patterns

### 1. Broadcast Pattern
```csharp
// Send to all connected clients
_sseService.SendEventToAll(new SseEvent 
{ 
    Event = "announcement",
    Data = "System maintenance at 2 AM"
});
```

### 2. Targeted Pattern
```csharp
// Send to specific client
_sseService.SendEventToClient(userId, new SseEvent 
{ 
    Event = "private-message",
    Data = JsonSerializer.Serialize(message)
});
```

### 3. Filtered Pattern
```csharp
// Client subscribes with filter
// GET /api/sse/connect?filter=orders

// Only receives matching events
_messageService.SendDataUpdateToAll("order-123", "order", changes);
```

### 4. Room/Channel Pattern
```csharp
// Implement room-based events
public void JoinRoom(string clientId, string roomId)
{
    _roomMemberships[roomId].Add(clientId);
}

public void SendToRoom(string roomId, SseEvent evt)
{
    foreach (var clientId in _roomMemberships[roomId])
    {
        _sseService.SendEventToClient(clientId, evt);
    }
}
```

## Testing Strategies

### Load Testing
```bash
# Concurrent connections test
seq 1 1000 | xargs -P 100 -I {} curl -N \
  "http://sse-demo.local/api/sse/connect?clientId=load-test-{}" &

# Send burst of events
for i in {1..1000}; do
  curl -X POST http://sse-demo.local/api/sse/broadcast \
    -H "X-API-Key: demo-api-key-12345" \
    -d "{\"eventType\":\"test\",\"data\":\"Message $i\"}"
done
```

### Integration Testing
```typescript
describe('SSE Integration', () => {
  it('receives filtered events', async () => {
    const events: any[] = [];
    const sse = new EventSource('/api/sse/connect?filter=test');
    
    sse.onmessage = (e) => events.push(JSON.parse(e.data));
    
    // Trigger test event
    await fetch('/api/sse/notification', {
      method: 'POST',
      headers: { 'X-API-Key': 'test-key' },
      body: JSON.stringify({ message: 'Test', severity: 'info' })
    });
    
    await waitFor(() => expect(events).toHaveLength(1));
    expect(events[0].type).toBe('notification');
  });
});
```