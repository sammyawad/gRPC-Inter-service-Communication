using Grpc.Net.Client;
using GrpcService.Protos;
using Microsoft.Extensions.Logging;

namespace GrpcService.Client;

public class GrpcClientService
{
    private readonly ILogger<GrpcClientService> _logger;
    private readonly string _clientId;

    public GrpcClientService(ILogger<GrpcClientService> logger)
    {
        _logger = logger;
        _clientId = Environment.MachineName + "_Client_" + Guid.NewGuid().ToString("N")[..8];
    }

    public async Task RunClientAsync(string serverAddress, string mode)
    {
        using var channel = GrpcChannel.ForAddress(serverAddress);
        var client = new CommunicationService.CommunicationServiceClient(channel);

        _logger.LogInformation($"Connected to gRPC server at {serverAddress}");
        _logger.LogInformation($"Client ID: {_clientId}");

        try
        {
            // Health check first
            await PerformHealthCheck(client);

            switch (mode.ToLower())
            {
                case "unary":
                    await TestUnaryCall(client);
                    break;
                case "serverstream":
                    await TestServerStreaming(client);
                    break;
                case "clientstream":
                    await TestClientStreaming(client);
                    break;
                case "chat":
                    await TestBidirectionalStreaming(client);
                    break;
                case "all":
                    await TestUnaryCall(client);
                    await Task.Delay(2000);
                    await TestClientStreaming(client);
                    await Task.Delay(2000);
                    await TestServerStreaming(client);
                    break;
                default:
                    await TestUnaryCall(client);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during client operation");
        }
    }

    private async Task PerformHealthCheck(CommunicationService.CommunicationServiceClient client)
    {
        _logger.LogInformation("Performing health check...");
        
        var healthResponse = await client.HealthCheckAsync(new Empty());
        _logger.LogInformation($"Server Health: {healthResponse.Status}, Server ID: {healthResponse.ServerId}");
    }

    private async Task TestUnaryCall(CommunicationService.CommunicationServiceClient client)
    {
        _logger.LogInformation("Testing unary RPC call...");

        var request = new MessageRequest
        {
            SenderId = _clientId,
            Content = "Hello from gRPC client!",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Type = MessageType.Info
        };

        var response = await client.SendMessageAsync(request);
        
        _logger.LogInformation($"Received response - ID: {response.MessageId}, Content: {response.Content}, Success: {response.Success}");
    }

    private async Task TestServerStreaming(CommunicationService.CommunicationServiceClient client)
    {
        _logger.LogInformation("Testing server streaming RPC...");

        using var call = client.GetMessages(new Empty());
        var messageCount = 0;

        try
        {
            while (await call.ResponseStream.MoveNext(CancellationToken.None))
            {
                var message = call.ResponseStream.Current;
                messageCount++;
                _logger.LogInformation($"Streamed message {messageCount} - ID: {message.MessageId}, Content: {message.Content}");
                
                if (messageCount >= 5) // Limit to first 5 messages for demo
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Server streaming ended: {ex.Message}");
        }

        _logger.LogInformation($"Received {messageCount} messages from server stream");
    }

    private async Task TestClientStreaming(CommunicationService.CommunicationServiceClient client)
    {
        _logger.LogInformation("Testing client streaming RPC...");

        using var call = client.SendMultipleMessages();

        // Send multiple messages
        for (int i = 1; i <= 5; i++)
        {
            var request = new MessageRequest
            {
                SenderId = _clientId,
                Content = $"Streamed message {i} from client",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Type = MessageType.Info
            };

            await call.RequestStream.WriteAsync(request);
            _logger.LogInformation($"Sent message {i}");
            await Task.Delay(500); // Small delay between messages
        }

        await call.RequestStream.CompleteAsync();
        var summary = await call;

        _logger.LogInformation($"Client streaming completed - Total: {summary.TotalMessages}, Success: {summary.SuccessfulMessages}, Failed: {summary.FailedMessages}");
    }

    private async Task TestBidirectionalStreaming(CommunicationService.CommunicationServiceClient client)
    {
        _logger.LogInformation("Testing bidirectional streaming (chat)...");

        using var call = client.Chat();

        // Start reading responses in background
        var readTask = Task.Run(async () =>
        {
            try
            {
                while (await call.ResponseStream.MoveNext(CancellationToken.None))
                {
                    var message = call.ResponseStream.Current;
                    _logger.LogInformation($"Chat message from {message.UserId}: {message.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Chat reading ended: {ex.Message}");
            }
        });

        // Send some chat messages
        for (int i = 1; i <= 3; i++)
        {
            var chatMessage = new ChatMessage
            {
                UserId = _clientId,
                Message = $"Hello from client, message {i}",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            await call.RequestStream.WriteAsync(chatMessage);
            _logger.LogInformation($"Sent chat message {i}");
            await Task.Delay(2000);
        }

        await call.RequestStream.CompleteAsync();
        await readTask;

        _logger.LogInformation("Bidirectional streaming completed");
    }
}
