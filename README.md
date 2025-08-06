# gRPC Inter-service Communication Chat Demo

A real-time chat application demonstrating ASP.NET Core web servers communicating via gRPC with Protocol Buffers for efficient data serialization.

## Features

- **Single Executable**: Can run in both server and client modes
- **Real-time Chat**: Bidirectional streaming for instant messaging
- **Multi-client Support**: Server handles multiple concurrent chat clients
- **Protocol Buffers**: Efficient data serialization
- **Health Monitoring**: Built-in server health checks
- **Thread-safe**: Concurrent client management with safe message broadcasting

## Project Structure

```
├── Program.cs                          # Main entry point with server/client mode selection
├── GrpcService.csproj                  # Project file with gRPC dependencies
├── Protos/
│   └── communication.proto             # Protocol Buffer definitions for chat
├── Services/
│   └── CommunicationServiceImpl.cs     # gRPC service implementation (server-side)
└── Client/
    └── GrpcClientService.cs            # gRPC client implementation
```

## Protocol Buffer Definition

The `communication.proto` file defines:
- **CommunicationService**: Chat service with bidirectional streaming
- **ChatMessage**: Message structure with user ID, content, and timestamp
- **HealthResponse**: Server status and client count monitoring

## Communication Methods

### 1. Bidirectional Streaming Chat
Real-time chat system where:
- Clients can send messages instantly
- Server broadcasts messages to all connected clients
- Multiple clients can participate simultaneously

### 2. Health Check
Unary RPC for server status monitoring and client count.

## How to Run

### 1. Start the Chat Server
```bash
dotnet run --mode=server --port=5000
```

The server will start and display:
```
=== gRPC Bidirectional Communication Demo ===
Mode: server
Port: 5000
Server Address: https://localhost:5000
==============================================
gRPC Server starting on port 5000...
```

### 2. Connect Chat Clients

Open **multiple terminal windows** and run:

```bash
# Terminal 1 (Client 1)
dotnet run --mode=client --server=https://localhost:5000

# Terminal 2 (Client 2) 
dotnet run --mode=client --server=https://localhost:5000

# Terminal 3 (Client 3)
dotnet run --mode=client --server=https://localhost:5000
```

Each client will:
1. Connect to the server
2. Perform a health check
3. Join the chat room
4. Allow you to type messages that broadcast to all other clients

### 3. Chat in Real-time

Once connected, clients can:
- Type messages and press Enter to send
- See messages from all other connected clients instantly
- Type `quit` or `exit` to leave the chat

## Example Chat Session

**Server Terminal:**
```
gRPC Server starting on port 5000...
Client client_abc123 joined the chat
Client client_def456 joined the chat
Broadcasting message from client_abc123 to 2 clients
```

**Client 1 Terminal:**
```
Starting gRPC client, connecting to https://localhost:5000...
Health check successful. Server ID: SERVER_001, Connected clients: 1
You have joined the chat! Type messages to send (type 'quit' to exit):
> Hello everyone!
[client_def456]: Hi there!
```

**Client 2 Terminal:**
```
Starting gRPC client, connecting to https://localhost:5000...
Health check successful. Server ID: SERVER_001, Connected clients: 2
You have joined the chat! Type messages to send (type 'quit' to exit):
[client_abc123]: Hello everyone!
> Hi there!
```

## Configuration Options

| Parameter | Description | Default | Example |
|-----------|-------------|---------|---------|
| `--mode` | Application mode | server | `--mode=client` |
| `--port` | Server port (server mode) | 5000 | `--port=5001` |
| `--server` | Server address (client mode) | https://localhost:5000 | `--server=https://localhost:5001` |

## Key Technologies

- **ASP.NET Core 8.0**: Web framework with Kestrel server
- **gRPC**: High-performance RPC framework
- **Protocol Buffers**: Efficient binary serialization
- **HTTP/2**: Transport protocol for gRPC streaming
- **ConcurrentDictionary**: Thread-safe client management
- **Async Streams**: Modern C# async enumerable patterns

## Development Notes

- **Kestrel Web Server**: Built-in ASP.NET Core server handles gRPC over HTTP/2
- **Thread-Safe Broadcasting**: Uses `ConcurrentDictionary` for safe multi-client message delivery
- **Automatic Cleanup**: Clients are automatically removed when they disconnect
- **Error Handling**: Graceful handling of network failures and client disconnections
- **Protocol Buffer Generation**: C# classes auto-generated from `.proto` file during build

## Building and Running

1. **Clone/Download the project**

2. **Restore Dependencies**:
   ```bash
   dotnet restore
   ```

3. **Build Project**:
   ```bash
   dotnet build
   ```

4. **Start Server**:
   ```bash
   dotnet run --mode=server
   ```

5. **Connect Clients** (in separate terminals):
   ```bash
   dotnet run --mode=client
   ```

## Testing Multi-Client Chat

To test the full chat functionality:

1. **Start one server** in Terminal 1
2. **Start 3-4 clients** in separate terminals
3. **Type messages** in any client terminal
4. **Watch messages appear** in all other client terminals instantly
5. **Disconnect clients** and see automatic cleanup on server

This demonstrates real-time, multi-client communication using gRPC bidirectional streaming!