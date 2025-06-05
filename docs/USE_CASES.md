# Server-Sent Events Use Cases and Examples

This document provides real-world use cases and implementation examples for Server-Sent Events, demonstrating when and how to use SSE effectively.

## Table of Contents

1. [Real-Time Dashboards](#real-time-dashboards)
2. [Live Notifications](#live-notifications)
3. [Progress Tracking](#progress-tracking)
4. [Live Sports/News Updates](#live-sportsnews-updates)
5. [Stock Market Data](#stock-market-data)
6. [IoT Device Monitoring](#iot-device-monitoring)
7. [Collaborative Features](#collaborative-features)
8. [System Monitoring](#system-monitoring)

## Real-Time Dashboards

### Use Case
Display live metrics, KPIs, and system status in real-time dashboards without requiring users to refresh the page.

### Example Implementation

**Backend (C#):**
```csharp
[HttpGet("dashboard-metrics")]
public async Task GetDashboardMetrics()
{
    Response.Headers.Add("Content-Type", "text/event-stream");
    
    while (!HttpContext.RequestAborted.IsCancellationRequested)
    {
        var metrics = await _metricsService.GetCurrentMetricsAsync();
        
        var sseEvent = new SseEvent
        {
            EventType = "metrics-update",
            Data = new
            {
                timestamp = DateTime.UtcNow,
                cpu = metrics.CpuUsage,
                memory = metrics.MemoryUsage,
                activeUsers = metrics.ActiveUsers,
                requestsPerSecond = metrics.RequestsPerSecond
            }
        };
        
        await Response.WriteAsync($"event: {sseEvent.EventType}\n");
        await Response.WriteAsync($"data: {JsonSerializer.Serialize(sseEvent.Data)}\n\n");
        await Response.Body.FlushAsync();
        
        await Task.Delay(5000); // Update every 5 seconds
    }
}
```

**Frontend (React):**
```typescript
const DashboardMetrics: React.FC = () => {
  const [metrics, setMetrics] = useState<DashboardMetrics | null>(null);
  
  useEffect(() => {
    const eventSource = new EventSource('/api/dashboard-metrics');
    
    eventSource.addEventListener('metrics-update', (event) => {
      const data = JSON.parse(event.data);
      setMetrics(data);
    });
    
    return () => eventSource.close();
  }, []);
  
  return (
    <div className="dashboard">
      <MetricCard title="CPU Usage" value={`${metrics?.cpu}%`} />
      <MetricCard title="Memory" value={`${metrics?.memory}MB`} />
      <MetricCard title="Active Users" value={metrics?.activeUsers} />
      <MetricCard title="Requests/sec" value={metrics?.requestsPerSecond} />
    </div>
  );
};
```

## Live Notifications

### Use Case
Push instant notifications to users for events like new messages, system alerts, or important updates.

### Example Implementation

**Backend (C#):**
```csharp
public class NotificationService
{
    private readonly ISseService _sseService;
    
    public async Task SendUserNotification(string userId, NotificationType type, string message)
    {
        var notification = new SseEvent
        {
            Id = Guid.NewGuid().ToString(),
            EventType = "notification",
            Data = new NotificationMessage
            {
                Type = type,
                Message = message,
                Timestamp = DateTime.UtcNow,
                Priority = GetPriority(type)
            }
        };
        
        // Send to specific user
        await _sseService.SendEventToClientAsync(userId, notification);
        
        // Store for offline delivery
        await _notificationStore.SaveAsync(userId, notification);
    }
}
```

**Frontend (React):**
```typescript
const NotificationCenter: React.FC = () => {
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const { events } = useSseTyped<NotificationMessage>('/api/notifications', {
    eventTypes: ['notification'],
    onMessage: (notification) => {
      // Show toast notification
      toast.info(notification.message, {
        position: 'top-right',
        autoClose: 5000
      });
      
      // Add to notification list
      setNotifications(prev => [notification, ...prev].slice(0, 50));
    }
  });
  
  return (
    <div className="notification-center">
      {notifications.map(notif => (
        <NotificationItem key={notif.id} notification={notif} />
      ))}
    </div>
  );
};
```

## Progress Tracking

### Use Case
Track long-running operations like file uploads, data processing, or batch jobs in real-time.

### Example Implementation

**Backend (C#):**
```csharp
[HttpPost("process-batch")]
public async Task<IActionResult> ProcessBatch([FromBody] BatchRequest request)
{
    var jobId = Guid.NewGuid().ToString();
    
    // Start background job
    _backgroundJobs.Enqueue(async () => await ProcessBatchJob(jobId, request));
    
    return Ok(new { jobId });
}

private async Task ProcessBatchJob(string jobId, BatchRequest request)
{
    var totalItems = request.Items.Count;
    var processed = 0;
    
    foreach (var item in request.Items)
    {
        // Process item
        await ProcessItemAsync(item);
        processed++;
        
        // Send progress update
        var progress = new SseEvent
        {
            EventType = "progress",
            Data = new
            {
                jobId,
                processed,
                total = totalItems,
                percentage = (processed * 100) / totalItems,
                currentItem = item.Name
            }
        };
        
        await _sseService.SendEventToAllAsync(progress);
        
        // Send completion event
        if (processed == totalItems)
        {
            await _sseService.SendEventToAllAsync(new SseEvent
            {
                EventType = "job-complete",
                Data = new { jobId, status = "success" }
            });
        }
    }
}
```

**Frontend (React):**
```typescript
const ProgressTracker: React.FC<{ jobId: string }> = ({ jobId }) => {
  const [progress, setProgress] = useState(0);
  const [status, setStatus] = useState<'running' | 'complete'>('running');
  
  useSse('/api/job-progress', {
    eventTypes: ['progress', 'job-complete'],
    onMessage: (event) => {
      if (event.type === 'progress' && event.data.jobId === jobId) {
        setProgress(event.data.percentage);
      } else if (event.type === 'job-complete' && event.data.jobId === jobId) {
        setStatus('complete');
      }
    }
  });
  
  return (
    <div className="progress-tracker">
      <h3>Processing Batch Job</h3>
      <ProgressBar value={progress} max={100} />
      <span>{progress}% complete</span>
      {status === 'complete' && <CheckIcon />}
    </div>
  );
};
```

## Live Sports/News Updates

### Use Case
Stream live scores, news updates, or event commentary to users in real-time.

### Example Implementation

**Backend (C#):**
```csharp
public class LiveSportsService
{
    public async Task StreamMatchUpdates(string matchId)
    {
        await _sseService.SendEventToAllAsync(new SseEvent
        {
            EventType = "match-event",
            Data = new
            {
                matchId,
                type = "goal",
                team = "home",
                player = "Smith",
                minute = 23,
                score = new { home = 1, away = 0 }
            }
        });
    }
    
    public async Task SendLiveCommentary(string matchId, string commentary)
    {
        await _sseService.SendEventToAllAsync(new SseEvent
        {
            EventType = "commentary",
            Data = new
            {
                matchId,
                text = commentary,
                timestamp = DateTime.UtcNow
            }
        });
    }
}
```

**Frontend (React):**
```typescript
const LiveMatch: React.FC<{ matchId: string }> = ({ matchId }) => {
  const [score, setScore] = useState({ home: 0, away: 0 });
  const [events, setEvents] = useState<MatchEvent[]>([]);
  const [commentary, setCommentary] = useState<string[]>([]);
  
  useSse(`/api/live-match/${matchId}`, {
    eventTypes: ['match-event', 'commentary'],
    onMessage: (event) => {
      if (event.type === 'match-event') {
        setScore(event.data.score);
        setEvents(prev => [event.data, ...prev]);
      } else if (event.type === 'commentary') {
        setCommentary(prev => [event.data.text, ...prev].slice(0, 100));
      }
    }
  });
  
  return (
    <div className="live-match">
      <ScoreBoard home={score.home} away={score.away} />
      <EventsFeed events={events} />
      <LiveCommentary entries={commentary} />
    </div>
  );
};
```

## Stock Market Data

### Use Case
Stream real-time stock prices, market indices, and trading data to financial applications.

### Example Implementation

**Backend (C#):**
```csharp
public class StockMarketService
{
    private readonly IMarketDataProvider _marketData;
    
    public async Task StreamStockPrices(string[] symbols)
    {
        _marketData.Subscribe(symbols, async (priceUpdate) =>
        {
            var sseEvent = new SseEvent
            {
                EventType = "price-update",
                Data = new
                {
                    symbol = priceUpdate.Symbol,
                    price = priceUpdate.Price,
                    change = priceUpdate.Change,
                    changePercent = priceUpdate.ChangePercent,
                    volume = priceUpdate.Volume,
                    timestamp = priceUpdate.Timestamp
                }
            };
            
            await _sseService.SendEventToAllAsync(sseEvent);
        });
    }
}
```

**Frontend (React):**
```typescript
const StockTicker: React.FC<{ symbols: string[] }> = ({ symbols }) => {
  const [prices, setPrices] = useState<Map<string, StockPrice>>(new Map());
  
  useSse('/api/stock-prices', {
    eventTypes: ['price-update'],
    filter: (event) => symbols.includes(event.data.symbol),
    onMessage: (event) => {
      setPrices(prev => {
        const updated = new Map(prev);
        updated.set(event.data.symbol, event.data);
        return updated;
      });
    }
  });
  
  return (
    <div className="stock-ticker">
      {symbols.map(symbol => {
        const price = prices.get(symbol);
        return (
          <StockCard
            key={symbol}
            symbol={symbol}
            price={price?.price}
            change={price?.changePercent}
          />
        );
      })}
    </div>
  );
};
```

## IoT Device Monitoring

### Use Case
Monitor IoT devices, sensors, and telemetry data in real-time for industrial or smart home applications.

### Example Implementation

**Backend (C#):**
```csharp
public class IoTMonitoringService
{
    public async Task StreamDeviceTelemetry(string deviceId)
    {
        var device = await _deviceRegistry.GetDeviceAsync(deviceId);
        
        device.OnTelemetryReceived += async (telemetry) =>
        {
            var sseEvent = new SseEvent
            {
                EventType = "telemetry",
                Data = new
                {
                    deviceId,
                    sensorData = telemetry.Sensors,
                    status = telemetry.Status,
                    battery = telemetry.BatteryLevel,
                    location = telemetry.Location,
                    timestamp = telemetry.Timestamp
                }
            };
            
            await _sseService.SendEventToAllAsync(sseEvent);
            
            // Check for alerts
            if (telemetry.Temperature > device.ThresholdTemp)
            {
                await SendAlert(deviceId, "High temperature detected");
            }
        };
    }
}
```

**Frontend (React):**
```typescript
const DeviceMonitor: React.FC<{ deviceId: string }> = ({ deviceId }) => {
  const [telemetry, setTelemetry] = useState<DeviceTelemetry | null>(null);
  const [alerts, setAlerts] = useState<Alert[]>([]);
  
  useSse(`/api/devices/${deviceId}/telemetry`, {
    eventTypes: ['telemetry', 'alert'],
    onMessage: (event) => {
      if (event.type === 'telemetry') {
        setTelemetry(event.data);
      } else if (event.type === 'alert') {
        setAlerts(prev => [event.data, ...prev]);
      }
    }
  });
  
  return (
    <div className="device-monitor">
      <DeviceStatus device={telemetry} />
      <SensorReadings sensors={telemetry?.sensorData} />
      <AlertsList alerts={alerts} />
    </div>
  );
};
```

## Collaborative Features

### Use Case
Enable real-time collaboration features like seeing who's online, cursor positions, or live document changes.

### Example Implementation

**Backend (C#):**
```csharp
public class CollaborationService
{
    private readonly Dictionary<string, HashSet<string>> _documentUsers = new();
    
    public async Task JoinDocument(string documentId, string userId, string userName)
    {
        if (!_documentUsers.ContainsKey(documentId))
            _documentUsers[documentId] = new HashSet<string>();
            
        _documentUsers[documentId].Add(userId);
        
        // Notify other users
        await _sseService.SendEventToAllAsync(new SseEvent
        {
            EventType = "user-joined",
            Data = new
            {
                documentId,
                userId,
                userName,
                activeUsers = _documentUsers[documentId].Count
            }
        });
    }
    
    public async Task SendCursorPosition(string documentId, string userId, int line, int column)
    {
        await _sseService.SendEventToAllAsync(new SseEvent
        {
            EventType = "cursor-move",
            Data = new
            {
                documentId,
                userId,
                position = new { line, column }
            }
        });
    }
}
```

**Frontend (React):**
```typescript
const CollaborativeEditor: React.FC<{ documentId: string }> = ({ documentId }) => {
  const [activeUsers, setActiveUsers] = useState<User[]>([]);
  const [cursors, setCursors] = useState<Map<string, CursorPosition>>(new Map());
  
  useSse(`/api/documents/${documentId}/collaboration`, {
    eventTypes: ['user-joined', 'user-left', 'cursor-move'],
    onMessage: (event) => {
      switch (event.type) {
        case 'user-joined':
          setActiveUsers(prev => [...prev, event.data]);
          break;
        case 'user-left':
          setActiveUsers(prev => prev.filter(u => u.id !== event.data.userId));
          break;
        case 'cursor-move':
          setCursors(prev => {
            const updated = new Map(prev);
            updated.set(event.data.userId, event.data.position);
            return updated;
          });
          break;
      }
    }
  });
  
  return (
    <div className="collaborative-editor">
      <ActiveUsersList users={activeUsers} />
      <Editor cursors={cursors} />
    </div>
  );
};
```

## System Monitoring

### Use Case
Monitor system health, performance metrics, and alerts for DevOps and system administration.

### Example Implementation

**Backend (C#):**
```csharp
public class SystemMonitoringService
{
    public async Task StreamSystemMetrics()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        
        while (await timer.WaitForNextTickAsync())
        {
            var metrics = await GatherSystemMetrics();
            
            await _sseService.SendEventToAllAsync(new SseEvent
            {
                EventType = "system-metrics",
                Data = new
                {
                    cpu = metrics.CpuUsage,
                    memory = metrics.MemoryUsage,
                    disk = metrics.DiskUsage,
                    network = metrics.NetworkStats,
                    services = metrics.ServiceStatuses,
                    timestamp = DateTime.UtcNow
                }
            });
            
            // Check thresholds and send alerts
            if (metrics.CpuUsage > 90)
            {
                await SendSystemAlert("High CPU usage detected", AlertLevel.Warning);
            }
        }
    }
}
```

**Frontend (React):**
```typescript
const SystemDashboard: React.FC = () => {
  const [metrics, setMetrics] = useState<SystemMetrics | null>(null);
  const [alerts, setAlerts] = useState<SystemAlert[]>([]);
  
  useSse('/api/system/monitoring', {
    eventTypes: ['system-metrics', 'system-alert'],
    onMessage: (event) => {
      if (event.type === 'system-metrics') {
        setMetrics(event.data);
      } else if (event.type === 'system-alert') {
        setAlerts(prev => [event.data, ...prev].slice(0, 20));
      }
    }
  });
  
  return (
    <div className="system-dashboard">
      <MetricsGrid metrics={metrics} />
      <ServiceStatusList services={metrics?.services} />
      <AlertsPanel alerts={alerts} />
    </div>
  );
};
```

## Event Triggering Patterns

### Overview
In SSE applications, events can be triggered in different ways depending on your use case. Understanding these patterns helps you design better real-time systems.

### Automatic Event Patterns

**1. Timer-Based Events**
```csharp
// Periodic updates (e.g., heartbeats, metrics)
public class PeriodicEventService : BackgroundService
{
    private readonly SseMessageService _messageService;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            _messageService.SendHeartbeatToAll();
        }
    }
}
```

**2. Change-Based Events**
```csharp
// Database change notifications
public class DataChangeService
{
    public async Task OnEntityUpdated(Entity entity)
    {
        _messageService.SendDataUpdateToAll(
            entity.Id,
            entity.Type,
            new { status = entity.Status, updatedAt = entity.UpdatedAt }
        );
    }
}
```

**3. System-Triggered Events**
```csharp
// System alerts and monitoring
public class SystemAlertService
{
    public async Task CheckSystemHealth()
    {
        if (cpuUsage > threshold)
        {
            _messageService.SendAlertToAll(
                "High CPU usage detected",
                severity: "high",
                category: "performance"
            );
        }
    }
}
```

### Manual Event Patterns

**1. User-Initiated Events**
```csharp
// User actions trigger notifications
[HttpPost("send-announcement")]
public async Task<IActionResult> SendAnnouncement([FromBody] AnnouncementDto dto)
{
    _messageService.SendNotificationToAll(dto.Message, "info");
    return Ok();
}
```

**2. Admin-Triggered Events**
```csharp
// Administrative broadcasts
[HttpPost("broadcast-maintenance")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> BroadcastMaintenance([FromBody] MaintenanceDto dto)
{
    _messageService.SendAlertToAll(
        $"Scheduled maintenance: {dto.StartTime}",
        severity: "critical",
        category: "system"
    );
    return Ok();
}
```

### Demo Mode for Testing

For testing and demonstration purposes, you can implement a demo mode that simulates automatic events:

```csharp
[ApiController]
[Route("api/demo")]
public class DemoController : ControllerBase
{
    [HttpPost("start")]
    public IActionResult StartDemo([FromBody] DemoRequest request)
    {
        // Start sending various event types on a timer
        // Cycles through notification, alert, dataUpdate, heartbeat
        _demoService.StartPeriodicEvents(request.IntervalSeconds);
        return Ok();
    }
}
```

**Frontend Integration:**
```typescript
// Inform users about manual vs automatic events
const EventTypeInfo = {
  notification: {
    automatic: false,
    description: 'User notifications - triggered by user actions or admin',
    example: 'New message, system announcement'
  },
  alert: {
    automatic: false, // Can be automatic in production systems
    description: 'Critical alerts - triggered by system conditions or admin',
    example: 'Security alerts, system failures'
  },
  dataUpdate: {
    automatic: true, // When connected to real database
    description: 'Data changes - triggered by entity modifications',
    example: 'User profile updates, config changes'
  },
  heartbeat: {
    automatic: true, // Typically on a timer
    description: 'Connection keep-alive - sent periodically',
    example: 'Prevents timeout disconnections'
  }
};
```

### Testing Event Flows

When developing SSE applications, test different event patterns:

1. **Manual Testing via API:**
   ```bash
   # Send test notification
   curl -X POST http://localhost:5121/api/sse/notification \
     -H "Content-Type: application/json" \
     -H "X-API-Key: your-api-key" \
     -d '{"message": "Test notification", "severity": "info"}'
   ```

2. **Automated Testing:**
   ```csharp
   [Test]
   public async Task TestEventDelivery()
   {
       // Connect test client
       var client = new SseTestClient("/api/sse/connect");
       
       // Trigger event
       await _messageService.SendNotificationToAll("Test");
       
       // Verify receipt
       var received = await client.WaitForEvent();
       Assert.AreEqual("notification", received.EventType);
   }
   ```

## Best Practices for Different Use Cases

### High-Frequency Updates (Stock Market, IoT)
- Implement client-side throttling
- Use event batching on the server
- Consider data compression
- Monitor bandwidth usage

### Long-Running Operations (Progress Tracking)
- Include operation IDs in events
- Implement resumable operations
- Store progress state for recovery
- Handle connection interruptions gracefully

### Multi-User Scenarios (Collaboration, Gaming)
- Use user-specific event streams
- Implement presence detection
- Handle user disconnections promptly
- Consider using rooms or channels

### Critical Notifications (Alerts, System Monitoring)
- Implement acknowledgment mechanisms
- Store undelivered notifications
- Use multiple delivery channels
- Include severity levels

## Conclusion

Server-Sent Events provide an elegant solution for many real-time communication needs. By understanding these use cases and following the implementation patterns, you can build robust, scalable real-time features in your applications.

Remember to:
- Choose SSE when unidirectional communication suffices
- Implement proper error handling and reconnection
- Monitor and scale based on connection load
- Consider fallback mechanisms for older browsers
- Test thoroughly under various network conditions

For more implementation details and code examples, refer to the source code in this repository.