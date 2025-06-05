#!/bin/bash

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}=== SSE Demo Kubernetes Deployment ===${NC}"
echo

# Check if kubectl is installed
if ! command -v kubectl &> /dev/null; then
    echo -e "${RED}kubectl is not installed. Please install kubectl first.${NC}"
    exit 1
fi

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo -e "${RED}Docker is not installed. Please install Docker first.${NC}"
    exit 1
fi

# Build Docker images
echo -e "${YELLOW}Building Docker images...${NC}"
echo "Building backend..."
docker build -t sse-backend:latest ./backend

echo "Building frontend..."
# Build for Ingress access (production-standard)
# Use empty VITE_API_URL to make frontend use relative paths
docker build -t sse-frontend:latest \
  --build-arg VITE_API_URL= \
  --build-arg VITE_API_KEY=demo-api-key-12345 \
  ./frontend

# Create namespace
echo -e "${YELLOW}Creating namespace...${NC}"
kubectl apply -f k8s/namespace.yaml

# Deploy MongoDB
echo -e "${YELLOW}Deploying MongoDB...${NC}"
kubectl apply -f k8s/mongodb.yaml

# Wait for MongoDB to be ready
echo -e "${YELLOW}Waiting for MongoDB to be ready...${NC}"
kubectl wait --for=condition=ready pod -l app=mongodb -n sse-demo --timeout=60s

# Deploy backend configuration
echo -e "${YELLOW}Deploying backend configuration...${NC}"
kubectl apply -f k8s/backend-config.yaml

# Deploy backend
echo -e "${YELLOW}Deploying backend...${NC}"
kubectl apply -f k8s/backend.yaml

# Wait for backend to be ready
echo -e "${YELLOW}Waiting for backend to be ready...${NC}"
kubectl wait --for=condition=ready pod -l app=backend -n sse-demo --timeout=120s

# Deploy frontend
echo -e "${YELLOW}Deploying frontend...${NC}"
kubectl apply -f k8s/frontend.yaml

# Deploy ingress
echo -e "${YELLOW}Deploying ingress...${NC}"
kubectl apply -f k8s/ingress.yaml

# Get deployment status
echo
echo -e "${GREEN}Deployment Status:${NC}"
kubectl get all -n sse-demo

echo
echo -e "${GREEN}Access Information:${NC}"
echo "Frontend: http://sse-demo.local"
echo
echo -e "${YELLOW}Prerequisites:${NC}"
echo "1. Install Nginx Ingress Controller (if not already installed):"
echo "   kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.8.2/deploy/static/provider/cloud/deploy.yaml"
echo
echo "2. Add to /etc/hosts (if not already added):"
echo "   echo '127.0.0.1 sse-demo.local' | sudo tee -a /etc/hosts"
echo
echo -e "${RED}Important:${NC}"
echo "This application requires Ingress for proper operation."
echo "NodePort (30080) is only for emergency troubleshooting."
echo
echo -e "${YELLOW}To test SSE across pods (internal pod communication):${NC}"
echo "kubectl exec -it deploy/backend -n sse-demo -- curl -X POST http://localhost:8080/api/sse/broadcast \\"
echo "  -H 'Content-Type: application/json' \\"
echo "  -H 'X-API-Key: demo-api-key-12345' \\"
echo "  -d '{\"eventType\": \"notification\", \"data\": \"Test from pod\"}'"
echo
echo -e "${YELLOW}To view logs:${NC}"
echo "kubectl logs -f deploy/backend -n sse-demo"
echo
echo -e "${YELLOW}To clean up:${NC}"
echo "kubectl delete namespace sse-demo"