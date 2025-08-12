using System.Diagnostics;
using Grpc.Net.Client; //https://www.nuget.org/packages/Grpc.Net.Client
using GrpcService.Protos;
using Microsoft.Extensions.Logging;
using Grpc.Core;

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

    public async Task RunBidirectionalCommunicationAsync(string serverAddress, string wave, CancellationToken cancellationToken)
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
            
            // Start data generation stream
            await StartDataGeneration(client, wave, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Client operation canceled.");
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

    private async Task StartDataGeneration(CommunicationService.CommunicationServiceClient client, string wave, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Starting high-precision decimal data generation (1ms interval), wave={wave}...");

        using var call = client.Chat();

        // Background task to read any responses (optional; server may echo/broadcast)
        var readTask = Task.Run(async () =>
        {
            try
            {
                while (await call.ResponseStream.MoveNext(cancellationToken))
                {
                    var message = call.ResponseStream.Current;
                    _logger.LogDebug($"Received echo from server: fraction={message.PreciseFraction}");
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
            catch (RpcException rex) when (rex.StatusCode == StatusCode.Cancelled)
            {
                // Expected when the call is cancelled by the client during Ctrl+C
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Response reading ended: {ex.Message}");
            }
        }, cancellationToken);

        // Generate a deterministic decimal in [0,1] with high precision using integer math only.
        // We use a 18-digit fixed-point scale for precision without floating conversions.
        var sw = Stopwatch.StartNew();

        // Generate selected waveform with 1 second period
        while (!cancellationToken.IsCancellationRequested)
        {
            // Coarse 1ms pacing
            await Task.Delay(1, cancellationToken);

            // Phase in [0,1)
            double phase = (sw.Elapsed.TotalMilliseconds % 1000.0) / 1000.0;
            decimal valueDec;
            switch (wave.ToLowerInvariant())
            {
                case "square":
                    valueDec = phase < 0.5 ? 0m : 1m;
                    break;
                case "saw":
                case "sawtooth":
                    // Use decimal for sawtooth for exactness
                    valueDec = (decimal)phase; // cast is safe to ~15-16 digits; we reformat to string later
                    break;
                case "sine":
                default:
                    // Compute sine in double, then convert to decimal and clamp
                    double s = 0.5 * (1.0 + Math.Sin(2 * Math.PI * phase));
                    // Round to 18 decimal places to reduce binary->decimal artifacts
                    valueDec = Math.Round((decimal)s, 18, MidpointRounding.AwayFromZero);
                    break;
            }
            if (valueDec < 0m) valueDec = 0m;
            if (valueDec > 1m) valueDec = 1m;

            var dataMessage = new ChatMessage
            {
                UserId = _clientId,
                Message = wave, // carry graph type so UI can label legend (e.g., sine/square/saw)
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            dataMessage.PreciseFractionDecimal = valueDec;

            try
            {
                await call.RequestStream.WriteAsync(dataMessage, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when shutting down via Ctrl+C
                break;
            }
            catch (RpcException rex) when (rex.StatusCode == StatusCode.Cancelled)
            {
                // Expected when the client cancels the streaming call; do not log as error
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send data: {ex.Message}");
                break;
            }
        }

        try
        {
            await call.RequestStream.CompleteAsync();
        }
        catch (RpcException rex) when (rex.StatusCode == StatusCode.Cancelled)
        {
            // Expected if the call is already cancelled/closed on shutdown
        }
        catch (Exception)
        {
            // Ignore completion errors during shutdown
        }
        
        try
        {
            await readTask;
        }
        catch (OperationCanceledException)
        {
            // Ignore during shutdown
        }

        _logger.LogInformation("Data generation stream completed");
    }
}
