# SSE Realtime Demo

> **⚠️ DEMO APPLICATION - NOT FOR PRODUCTION**  
> Hardcoded credentials for easy setup. See [SECURITY.md](./SECURITY.md).

Distributed Server-Sent Events implementation with .NET 9.0, React, and Kubernetes. Features MongoDB outbox pattern, clean architecture, and event ordering guarantees.

## Quick Start

```bash
# Prerequisites: Docker Desktop with Kubernetes, kubectl, Nginx Ingress
echo '127.0.0.1 sse-demo.local' | sudo tee -a /etc/hosts

# Deploy
./deploy-k8s.sh

# Access
open http://sse-demo.local
```

## Architecture

- **Frontend**: React SPA with TypeScript, deployed on Nginx (2 pods)
- **Backend**: .NET 9.0 API with clean architecture (3-10 pods auto-scaling)
- **Storage**: MongoDB with outbox pattern for distributed event delivery
- **Infrastructure**: Kubernetes with Nginx Ingress

### Key Patterns
- Repository pattern for data access
- Service layer for business logic
- MongoDB outbox for cross-pod event distribution
- Atomic sequence numbers for global event ordering
- Client checkpoints for reliable event replay

## API Endpoints

```bash
# SSE Connection
GET /api/sse/connect?clientId={id}&filter={eventType}&checkpoint={seq}&apikey={key}

# Event Broadcasting
POST /api/sse/broadcast          # Custom event to all clients
POST /api/sse/send/{clientId}    # Event to specific client
POST /api/sse/notification       # System notification
POST /api/sse/alert             # Alert broadcast
POST /api/sse/data-update       # Data update event

# Demo Control
POST /api/demo/start            # Start event generation
POST /api/demo/stop             # Stop event generation
```

## Event Flow

1. **Publish**: Event → Service Layer → MongoDB Outbox (with sequence number)
2. **Distribute**: Each pod polls outbox → Delivers to local clients
3. **Checkpoint**: Client acknowledges → Update checkpoint in MongoDB
4. **Recovery**: On reconnect → Replay from checkpoint → Resume streaming

## Testing

The project includes comprehensive test coverage for distributed SSE functionality, checkpoint recovery, and event ordering guarantees.

## Examples

### Frontend Usage
```typescript
const { events, status } = useSse({
  url: '/api/sse/connect',
  filter: 'notification',
  apiKey: 'demo-api-key-12345',
  useCheckpoint: true
});
```

### Backend Service Layer
```csharp
await _broadcastService.BroadcastNotificationAsync(
    "System update complete", 
    "info"
);
```

### Event Format
```json
{
  "_sequence": 12345,
  "type": "notification",
  "messageId": "uuid",
  "timestamp": "2025-01-09T10:30:00Z",
  "data": { "message": "Hello World" }
}
```

## Monitoring

```bash
GET /health/live   # Kubernetes liveness probe
GET /health/ready  # Kubernetes readiness probe
```

## Recent Improvements

- **Fixed Critical Event Loss** - Redesigned outbox pattern for reliable delivery
- **Repository Pattern** - Clean separation of data access  
- **Service Layer** - Business logic abstraction
- **Checkpoint Recovery** - Client-side event recovery

## Documentation

- [SSE Best Practices](docs/SSE_BEST_PRACTICES.md) - Implementation patterns
- [Use Cases](docs/USE_CASES.md) - Real-world scenarios
- [Quick Start](docs/QUICK_START.md) - Setup instructions


## Contributing

1. Fork the repository
2. Create a feature branch
3. Submit a pull request

## License

MIT - See [LICENSE](LICENSE)