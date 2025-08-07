# gRPC Real-Time Chat Application

A real-time chat application demonstrating gRPC inter-service communication with Protocol Buffers, featuring both console and web clients.

## What I Built

- **gRPC Server**: ASP.NET Core server with bidirectional streaming for real-time chat
- **Console Client**: .NET console application for terminal-based chatting  
- **Web Client**: Vue.js frontend for browser-based chatting
- **Docker Setup**: Complete containerized deployment

## Architecture

```
Backend (ASP.NET Core + gRPC)  ←→  Frontend (Vue.js Web App)
                ↕                        ↕
        Console Client (.NET)     SignalR WebSocket
```

## Quick Start with Docker

### Prerequisites
- Docker Desktop installed and running

### Run the Application
1. Clone this repository
2. Open terminal in project folder
3. Run:
   ```bash
   docker-compose up --build
   ```

### Access the Chat
- **Web Client**: Open http://localhost:3000 in your browser
- **Multiple Clients**: Open multiple browser tabs for different users
- **Console Client**: Run `dotnet run --mode=client` in separate terminals

### Stop the Application
Press `Ctrl+C` in the terminal or run:
```bash
docker-compose down
```

## Manual Development Setup

### Start Server
```bash
dotnet run --mode=server --port=5000
```

### Start Console Clients
```bash
# Terminal 1
dotnet run --mode=client

# Terminal 2  
dotnet run --mode=client
```

### Start Web Frontend
```bash
cd frontend
npm install
npm run dev
```

## Key Features

- **Real-time messaging** via gRPC bidirectional streaming
- **Multi-client support** with automatic broadcasting
- **Protocol Buffers** for efficient serialization
- **Thread-safe** concurrent client handling
- **Health monitoring** and connection status
- **Docker containerization** for easy deployment

## Technologies Used

- **Backend**: ASP.NET Core 8, gRPC, Protocol Buffers
- **Frontend**: Vue.js 3, Vite
- **Deployment**: Docker, Docker Compose
- **Transport**: HTTP/2 with gRPC streaming

## Project Structure

```
├── Server/                 # gRPC server implementation
├── Client/                 # Console client
├── frontend/              # Vue.js web application  
├── protos/                # Protocol buffer definitions
├── docker-compose.yml     # Container orchestration
└── Dockerfile.*           # Container definitions
```

This demonstrates modern microservice communication patterns with gRPC for high-performance, real