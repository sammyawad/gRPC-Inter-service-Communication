using Grpc.Core;
using GrpcService.Protos;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

//example https://github.com/grpc/grpc-dotnet/blob/master/examples/Mailer/Server/Services/MailerService.cs
namespace GrpcService.Services;

public class CommunicationServiceImpl : CommunicationService.CommunicationServiceBase
{
    private readonly ILogger<CommunicationServiceImpl> _logger;
    private readonly string _serverId;
    private readonly ConcurrentDictionary<string, IServerStreamWriter<ChatMessage>> _chatClients;
    private readonly DataStore _dataStore;
    private readonly IHubContext<GrpcService.Hubs.ChatWebSocketHub> _hubContext;

    public CommunicationServiceImpl(ILogger<CommunicationServiceImpl> logger, DataStore dataStore, IHubContext<GrpcService.Hubs.ChatWebSocketHub> hubContext)
    {
        _logger = logger;
        _serverId = Environment.MachineName + "_Server_" + Guid.NewGuid().ToString("N")[..8];
        _chatClients = new ConcurrentDictionary<string, IServerStreamWriter<ChatMessage>>();
        _dataStore = dataStore;
        _hubContext = hubContext;
    }

    // Bidirectional streaming method - matches your proto file
    public override async Task Chat(IAsyncStreamReader<ChatMessage> requestStream,
                                  IServerStreamWriter<ChatMessage> responseStream,
                                  ServerCallContext context)
    {
        var clientId = context.GetHttpContext()?.Connection?.Id ?? Guid.NewGuid().ToString("N")[..8];
        _logger.LogInformation($"Client {clientId} joined chat");

        // Add client to active connections
        _chatClients.TryAdd(clientId, responseStream);

        try
        {
            // Send welcome message
            var welcomeMessage = new ChatMessage
            {
                UserId = "System",
                Message = $"Welcome to the chat! Server ID: {_serverId}. Connected clients: {_chatClients.Count}",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            await responseStream.WriteAsync(welcomeMessage);

            // Process incoming messages using modern pattern
            await foreach (var message in requestStream.ReadAllAsync())
            {
                // Try to parse precise decimal and update data store and broadcast via SignalR
                var value = message.PreciseFractionDecimal;
                if (value.HasValue)
                {
                    _dataStore.Update(message.UserId, value.Value);
                    await _hubContext.Clients.All.SendAsync("DataUpdated", new {
                        UserId = message.UserId,
                        Value = value.Value,
                        Timestamp = DateTime.UtcNow
                    });
                }

                // Optional: also echo back to gRPC clients
                await BroadcastToAllClients(message);
            }
        }
        finally
        {
            // Clean up when client disconnects
            _chatClients.TryRemove(clientId, out _);
            _logger.LogInformation($"Client {clientId} left chat. Remaining clients: {_chatClients.Count}");
        }
    }

    // Health check method - matches your proto file
    public override Task<HealthResponse> HealthCheck(Empty request, ServerCallContext context)
    {
        var response = new HealthResponse
        {
            Status = "Healthy",
            ServerId = _serverId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        _logger.LogInformation($"Health check requested - Status: {response.Status}, Connected clients: {_chatClients.Count}");
        return Task.FromResult(response);
    }

    // Helper method to broadcast messages to all connected clients
    private async Task BroadcastToAllClients(ChatMessage message)
    {
        var disconnectedClients = new List<string>();

        foreach (var (clientId, stream) in _chatClients.ToArray())
        {
            try
            {
                await stream.WriteAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to send message to client {clientId}: {ex.Message}");
                disconnectedClients.Add(clientId);
            }
        }

        // Remove disconnected clients
        foreach (var clientId in disconnectedClients)
        {
            _chatClients.TryRemove(clientId, out _);
            _logger.LogInformation($"Removed disconnected client: {clientId}");
        }
    }
}
