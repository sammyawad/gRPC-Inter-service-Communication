using Grpc.Net.Client; //https://www.nuget.org/packages/Grpc.Net.Client
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

    public async Task RunBidirectionalCommunicationAsync(string serverAddress)
    {
        // Create HTTP handler to bypass SSL certificate validation (development only)
        var httpHandler = new HttpClientHandler();
        httpHandler.ServerCertificateCustomValidationCallback = 
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        // Create gRPC channel with custom HTTP handler
        using var channel = GrpcChannel.ForAddress(serverAddress, new GrpcChannelOptions
        {
            HttpHandler = httpHandler
        });
        
        var client = new CommunicationService.CommunicationServiceClient(channel);

        _logger.LogInformation($"Connected to gRPC server at {serverAddress}");
        _logger.LogInformation($"Client ID: {_clientId}");

        try
        {
            // Health check first
            await PerformHealthCheck(client);
            
            // Run bidirectional streaming communication
            await StartBidirectionalChat(client);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bidirectional communication");
            throw;
        }
    }

    private async Task PerformHealthCheck(CommunicationService.CommunicationServiceClient client)
    {
        _logger.LogInformation("Performing health check...");
        
        var healthResponse = await client.HealthCheckAsync(new Empty());
        _logger.LogInformation($"Server Health: {healthResponse.Status}, Server ID: {healthResponse.ServerId}");
    }

    private async Task StartBidirectionalChat(CommunicationService.CommunicationServiceClient client)
    {
        _logger.LogInformation("Starting bidirectional streaming chat...");

        using var call = client.Chat();

        // Start reading responses in background
        var readTask = Task.Run(async () =>
        {
            try
            {
                while (await call.ResponseStream.MoveNext(CancellationToken.None))
                {
                    var message = call.ResponseStream.Current;
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message.UserId}: {message.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Chat reading ended: {ex.Message}");
            }
        });

        // Interactive message sending
        Console.WriteLine($"\n=== Chat Started (Client ID: {_clientId}) ===");
        Console.WriteLine("Type your messages (press 'quit' to exit):");
        Console.WriteLine("==========================================");

        string? input;
        while ((input = Console.ReadLine()) != null && input.ToLower() != "quit")
        {
            if (!string.IsNullOrWhiteSpace(input))
            {
                var chatMessage = new ChatMessage
                {
                    UserId = _clientId,
                    Message = input,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                try
                {
                    await call.RequestStream.WriteAsync(chatMessage);
                    _logger.LogInformation($"Sent: {input}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to send message: {ex.Message}");
                    break;
                }
            }
        }

        await call.RequestStream.CompleteAsync();
        await readTask;

        _logger.LogInformation("Bidirectional streaming chat completed");
    }
}