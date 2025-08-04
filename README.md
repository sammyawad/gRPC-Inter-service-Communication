# gRPC Inter-service Communication

A distributed application demonstrating ASP.NET Core web servers communicating via gRPC with Protocol Buffers for data serialization.

## Features

- **Single Executable**: Can run in both server and client modes
- **Multiple RPC Patterns**: Unary, Server Streaming, Client Streaming, and Bidirectional Streaming
- **Protocol Buffers**: Efficient data serialization
- **Multi-client Support**: Server can handle multiple concurrent clients
- **Health Checks**: Built-in health monitoring
- **Chat System**: Real-time bidirectional communication

## Project Structure

```
├── Program.cs                          # Main entry point with mode selection
├── GrpcService.csproj                  # Project file with gRPC dependencies
├── Protos/
│   └── communication.proto             # Protocol Buffer definitions
├── Services/
│   └── CommunicationServiceImpl.cs     # gRPC service implementation
└── Client/
    └── GrpcClientService.cs            # gRPC client implementation
```

## Protocol Buffer Definition

The `communication.proto` file defines:
- **CommunicationService**: Main service with multiple RPC methods
- **Message Types**: Request/Response structures for different communication patterns
- **Enums**: Message types (INFO, WARNING, ERROR, DEBUG)

## RPC Methods

### 1. Unary RPC - SendMessage
Simple request-response pattern for sending individual messages.

### 2. Server Streaming - GetMessages
Server streams all stored messages to the client.

### 3. Client Streaming - SendMultipleMessages
Client streams multiple messages to server, receives summary.

### 4. Bidirectional Streaming - Chat
Real-time chat system with message broadcasting to all connected clients.

### 5. Health Check - HealthCheck
Server status monitoring.

## Usage

### Server Mode
Start the gRPC server:
```bash
dotnet run --mode=server --port=5000
```

### Client Mode

#### Test Unary RPC:
```bash
dotnet run --mode=client --server=https://localhost:5000 --client-mode=unary
```

#### Test Server Streaming:
```bash
dotnet run --mode=client --server=https://localhost:5000 --client-mode=serverstream
```

#### Test Client Streaming:
```bash
dotnet run --mode=client --server=https://localhost:5000 --client-mode=clientstream
```

#### Test Bidirectional Streaming (Chat):
```bash
dotnet run --mode=client --server=https://localhost:5000 --client-mode=chat
```

#### Run All Tests:
```bash
dotnet run --mode=client --server=https://localhost:5000 --client-mode=all
```

## Multi-Client Demo

1. **Start Server**:
   ```bash
   dotnet run --mode=server --port=5000
   ```

2. **Start Multiple Clients** (in separate terminals):
   ```bash
   # Terminal 1
   dotnet run --mode=client --server=https://localhost:5000 --client-mode=chat
   
   # Terminal 2
   dotnet run --mode=client --server=https://localhost:5000 --client-mode=unary
   
   # Terminal 3
   dotnet run --mode=client --server=https://localhost:5000 --client-mode=all
   ```

## Key Technologies

- **ASP.NET Core 8.0**: Web framework
- **gRPC**: High-performance RPC framework
- **Protocol Buffers**: Data serialization
- **HTTP/2**: Transport protocol for gRPC
- **Concurrent Collections**: Thread-safe data structures

## Configuration Options

| Parameter | Description | Default |
|-----------|-------------|---------|
| `--mode` | Application mode (server/client) | server |
| `--port` | Server port | 5000 |
| `--server` | Server address for client | https://localhost:5000 |
| `--client-mode` | Client operation mode | unary |

## Development Notes

- Server uses HTTP/2 protocol required for gRPC
- Concurrent message storage using thread-safe collections
- Proper error handling and logging throughout
- Graceful handling of client disconnections
- Protocol Buffer code generation at build time

## Building and Running

1. **Restore Dependencies**:
   ```bash
   dotnet restore
   ```

2. **Build Project**:
   ```bash
   dotnet build
   ```

3. **Run Application**:
   ```bash
   dotnet run --mode=server
   ```

The Protocol Buffer compiler will automatically generate C# classes from the `.proto` file during build.
