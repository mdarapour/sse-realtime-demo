# Quick Start Guide

Get the SSE demo application running in under 5 minutes!

## 🚀 Prerequisites

This demo is designed for production-ready distributed systems and **requires Kubernetes** for all development and testing.

**Required:**
- Docker Desktop with Kubernetes enabled
- kubectl CLI tool
- Nginx Ingress Controller
- Host file entry: `127.0.0.1 sse-demo.local`

## 🎯 Deployment (Kubernetes Only)

```bash
# Clone the repository
git clone https://github.com/your-org/sse-realtime-demo.git
cd sse-realtime-demo

# Deploy to Kubernetes
./deploy-k8s.sh

# Open your browser
# Main URL: http://sse-demo.local
```

That's it! The application is now running with:
- Frontend (React + TypeScript) with nginx proxy
- Backend (.NET 9.0 SSE Service) with horizontal scaling (3-10 pods)
- MongoDB for distributed event delivery across pods
- Nginx Ingress for production-like routing

## 📋 Why Kubernetes is Required

This demo implements a truly distributed SSE architecture:
- Events are distributed across multiple backend pods via MongoDB outbox pattern
- No sticky sessions required - any pod can handle any client
- Horizontal auto-scaling based on load
- Production-ready architecture from development to deployment

There is no "local mode" - we develop exactly as we deploy.

## 🎮 Try the Examples

Once running, navigate to the frontend and try:

1. **Basic Example** - Simple SSE connection
2. **Typed Events** - Strongly-typed event handling
3. **Multiple Streams** - Concurrent SSE connections
4. **Reconnection** - Automatic reconnection demo
5. **Filtered Events** - Server-side event filtering
6. **Custom Events** - Send your own events

## 🧪 Test the API

### Using curl
```bash
# Connect to SSE stream
curl -N http://localhost:5121/api/sse/connect

# Send a broadcast message
curl -X POST http://localhost:5121/api/sse/broadcast \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: demo-api-key-123" \
  -d '{"eventType":"message","data":"Hello SSE!"}'
```

### Using the included HTTP file
```bash
# Open backend/SseDemo.http in VS Code with REST Client extension
# Click "Send Request" on any example
```

## ☸️ Kubernetes Setup Details

### Install Nginx Ingress Controller

```bash
# For Docker Desktop / Local Kubernetes
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.8.2/deploy/static/provider/cloud/deploy.yaml

# Wait for it to be ready
kubectl wait --namespace ingress-nginx \
  --for=condition=ready pod \
  --selector=app.kubernetes.io/component=controller \
  --timeout=120s
```

### Add Host Entry

```bash
# Linux/Mac
echo "127.0.0.1 sse-demo.local" | sudo tee -a /etc/hosts

# Windows (run as Administrator)
# Add to C:\Windows\System32\drivers\etc\hosts
```

### Deploy Application

```bash
# Use the deployment script
./deploy-k8s.sh

# Check deployment
kubectl get all,ingress -n sse-demo

# Watch backend logs
kubectl logs -f deploy/backend -n sse-demo
```

### Access via Ingress

- Main URL: **http://sse-demo.local**
- The application uses production-like Ingress routing
- All API calls are proxied through frontend nginx

### Alternative: NodePort Access

If Ingress is not available:
- Frontend: http://localhost:30080
- Note: Some features may not work correctly without Ingress

## 📖 Next Steps

- Read the [Architecture Overview](README.md#architecture-overview)
- Explore [SSE Best Practices](SSE_BEST_PRACTICES.md)
- Check out [Real-World Use Cases](USE_CASES.md)
- Review the [API Documentation](README.md#sse-endpoints)

## 🆘 Troubleshooting

### Backend won't start
- Check .NET version: `dotnet --version` (needs 9.0+)
- Check port 5121 is free: `lsof -i :5121`

### Frontend won't connect
- Verify backend is running at http://localhost:5121
- Check browser console for CORS errors
- Ensure you're using a modern browser

### MongoDB connection issues
- Backend works without MongoDB (in-memory mode)
- For Docker: wait for MongoDB to fully start
- Check connection string in appsettings.json

### Events not showing
- Open browser DevTools Network tab
- Look for "eventsource" connection
- Check for "text/event-stream" content type
- Verify API key if using authenticated endpoints

## 💡 Tips

- Use Chrome DevTools to inspect SSE connections
- Monitor the Network tab for real-time events
- Try different examples to understand patterns
- Modify code and see changes instantly with hot-reload

Happy coding! 🎉