# gRPC-Inter-service-Communication
Objective: To build a distributed application consisting of two or more ASP.NET Core web servers that communicate with each other using gRPC. The project will demonstrate the use of Protocol Buffers for data serialization and will be configurable to run in either a "client" or "server" mode.
Key Components:
ASP.NET Core Web Servers: A single project will be created that can be executed in different modes.

gRPC: The framework will be used to establish a high-performance communication channel between the services.

Protocol Buffers (.proto files): These will define the data structures (messages) and service interfaces for the RPC calls.

Client/Server Modes: The application will be designed to run from the command line, with flags or arguments to specify whether it should act as a gRPC server or a gRPC client.

Multi-client Support: The project will be scalable to support communication between a single server and multiple clients simultaneously.

Deliverables:

A working ASP.NET Core application that can be run in client and server modes.

A defined .proto file that specifies the inter-service communication contract.

Code demonstrating the serialization and deserialization of data using Protocol Buffers.

A clear example of a client-server interaction via gRPC.
