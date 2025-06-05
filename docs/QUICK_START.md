# Quick Start

> **⚠️ Kubernetes-Only Application**  
> No local development mode. MongoDB required.

## Prerequisites

- Docker Desktop with Kubernetes enabled
- kubectl CLI
- Nginx Ingress Controller
- `/etc/hosts`: `127.0.0.1 sse-demo.local`

## Deploy

```bash
git clone https://github.com/mdarapour/sse-realtime-demo.git
cd sse-realtime-demo

# Install Nginx Ingress if needed
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.8.2/deploy/static/provider/cloud/deploy.yaml

# Deploy application
./deploy-k8s.sh

# Wait for pods
kubectl get pods -n sse-demo -w
```

## Access

- Frontend: http://sse-demo.local
- API: http://sse-demo.local/api

## Test API

```bash
# Connect to SSE stream
curl -N http://sse-demo.local/api/sse/connect?apikey=demo-api-key-12345

# Send broadcast event
curl -X POST http://sse-demo.local/api/sse/broadcast \
  -H "Content-Type: application/json" \
  -H "X-API-Key: demo-api-key-12345" \
  -d '{"eventType": "notification", "data": "Hello SSE!"}'

# Start demo mode (generates events)
curl -X POST http://sse-demo.local/api/demo/start \
  -H "Content-Type: application/json" \
  -H "X-API-Key: demo-api-key-12345" \
  -d '{"intervalSeconds": 3}'
```

## Troubleshooting

```bash
# Check deployment
kubectl get all -n sse-demo
kubectl logs -n sse-demo deployment/backend

# MongoDB issues
kubectl logs -n sse-demo statefulset/mongodb

# Ingress issues
kubectl get ingress -n sse-demo
kubectl describe ingress sse-ingress -n sse-demo
```

## Cleanup

```bash
kubectl delete namespace sse-demo
```