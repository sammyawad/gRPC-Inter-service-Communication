using System.Diagnostics;
using Grpc.Net.Client;
using GrpcService.Protos;
using Grpc.Core;
namespace GrpcService.Client;

public class GrpcClientService
{
    private readonly string _clientId;

    public GrpcClientService()
    {
        _clientId = Environment.MachineName + "_Client_" + Guid.NewGuid().ToString("N")[..8];
    }

    public async Task RunClientStreamingAsync(string serverAddress, string wave, decimal ymin, decimal ymax, CancellationToken cancellationToken)
    {
        var httpHandler = new HttpClientHandler();
        httpHandler.ServerCertificateCustomValidationCallback = 
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        using var channel = GrpcChannel.ForAddress(serverAddress, new GrpcChannelOptions { HttpHandler = httpHandler });
        var client = new CommunicationService.CommunicationServiceClient(channel);
        using var call = client.SendData(); // client-streaming: AsyncClientStreamingCall<ChatMessage, Empty>

        if (ymin > ymax) (ymin, ymax) = (ymax, ymin);
        var sw = Stopwatch.StartNew();

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1, cancellationToken);

            var phase = sw.Elapsed.TotalMilliseconds % 1000.0 / 1000.0;
            var baseValue = wave.ToLowerInvariant() switch
            {
                "square" => phase < 0.5 ? 0m : 1m,
                "saw" or "sawtooth" => (decimal)phase,
                _ => Math.Round((decimal)(0.5 * (1.0 + Math.Sin(2 * Math.PI * phase))), 18)
            };

            var mapped = Math.Round(ymin + baseValue * (ymax - ymin), 18);

            var message = new ChatMessage
            {
                UserId = _clientId,
                Message = wave,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                YMin = ymin.ToString("G29", System.Globalization.CultureInfo.InvariantCulture),
                YMax = ymax.ToString("G29", System.Globalization.CultureInfo.InvariantCulture)
            };
            message.PreciseFractionDecimal = mapped;

            try
            {
                await call.RequestStream.WriteAsync(message, cancellationToken);
            }
            catch (RpcException rex)
            {
                Console.WriteLine($"[client] RpcException: {rex.Status.StatusCode} - {rex.Status.Detail}");
                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        try { await call.RequestStream.CompleteAsync(); } catch { }
        try { _ = await call.ResponseAsync; } catch { }
    }
}
