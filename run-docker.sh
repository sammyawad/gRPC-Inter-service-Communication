#!/bin/bash

echo "🚀 Starting gRPC Chat Demo with Docker..."
echo "=========================================="

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker first."
    exit 1
fi

# Build and start services
echo "📦 Building and starting services..."
docker-compose down --volumes --remove-orphans
docker-compose up --build

echo "✅ Services started!"
echo "Frontend: http://localhost:3000"
echo "Backend Health: http://localhost:5000/health"
echo ""
echo "Press Ctrl+C to stop all services"
