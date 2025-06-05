# SSE Demo Frontend

React + TypeScript frontend for the Server-Sent Events (SSE) demonstration application.

## Overview

This frontend showcases various SSE patterns and best practices through interactive examples:

- **Basic SSE Connection** - Simple event streaming
- **Typed Events** - Strongly-typed event handling with schema validation
- **Multiple Streams** - Managing concurrent SSE connections
- **Automatic Reconnection** - Resilient connection handling
- **Event Filtering** - Server-side event filtering
- **Custom Events** - Broadcasting events between clients

## Getting Started

For the complete setup guide, see the [Quick Start Guide](../docs/QUICK_START.md).

### Quick Start

```bash
# Install dependencies
npm install

# Start development server
npm run dev
```

The frontend will be available at http://localhost:5173

### Environment Variables

```bash
# For local development
export VITE_API_URL=http://localhost:5121
export VITE_API_KEY=demo-api-key-12345

# For production/Kubernetes
# These are set during the Docker build process
```

## Project Structure

```
src/
├── components/
│   ├── SseDemo.tsx              # Main demo component
│   └── examples/                # Individual example components
│       ├── BasicExample.tsx
│       ├── TypedEventsExample.tsx
│       ├── MultipleStreamsExample.tsx
│       ├── ReconnectionExample.tsx
│       ├── FilteredEventsExample.tsx
│       └── CustomEventsExample.tsx
├── hooks/
│   ├── useSse.ts               # Basic SSE hook
│   └── useSseTyped.ts          # Typed SSE hook with validation
├── services/
│   └── sseService.ts           # SSE client service
├── models/
│   ├── sseEventTypes.ts        # Event type definitions
│   ├── sseMessages.ts          # Message payload types
│   └── sseSchemas.ts           # Zod schemas for validation
└── main.tsx                    # Application entry point
```

## Key Features

### Custom Hooks

- **`useSse`** - Basic SSE connection management with automatic reconnection
- **`useSseTyped`** - Type-safe event handling with schema validation

### SSE Client Service

The `SseClient` class provides:
- Automatic reconnection with exponential backoff
- Connection status tracking
- Event deduplication
- Configurable retry strategies
- Clean resource management

### Type Safety

All event types and payloads are strongly typed and validated:
- Shared type definitions with backend
- Runtime validation using Zod schemas
- TypeScript interfaces for all event payloads

## Development

```bash
# Run development server
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview

# Run linter
npm run lint
```

## Docker Build

The frontend is built as a multi-stage Docker image:

```bash
# Build with specific API URL (for Kubernetes/Ingress)
docker build -t sse-frontend:latest \
  --build-arg VITE_API_URL=http://sse-demo.local \
  --build-arg VITE_API_KEY=demo-api-key-12345 \
  .
```

## Configuration

The frontend uses nginx to serve the static files and proxy API requests:
- Static files served from `/usr/share/nginx/html`
- API requests (`/api/*`) proxied to backend service
- SSE-optimized settings (no buffering, long timeouts)

## Browser Support

SSE is supported in all modern browsers:
- Chrome/Edge 6+
- Firefox 6+
- Safari 5+
- Opera 11+

Note: Internet Explorer does not support SSE natively.

## Troubleshooting

### Connection Issues
- Check browser DevTools Network tab for EventSource connections
- Verify backend is running and accessible
- Check for CORS errors in console
- Ensure API key is correct (if required)

### Events Not Appearing
- Verify connection status in the UI
- Check for duplicate event filtering
- Monitor browser console for errors
- Ensure event types match expected values

## Contributing

See the main [Contributing Guidelines](../CONTRIBUTING.md) for details on our development process.
