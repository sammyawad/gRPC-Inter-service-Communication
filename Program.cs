using GrpcService.Services;
using GrpcService.Client;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace GrpcService;

public class Program
{
    public static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddCommandLine(args)
            .Build();

        var mode = configuration["mode"] ?? "server";
        var port = configuration["port"] ?? "5000";
        var serverAddress = configuration["server"] ?? "https://localhost:5001";
        var wave = configuration["wave"] ?? "sine";
        
        decimal.TryParse(configuration["ymin"], out var ymin);
        decimal.TryParse(configuration["ymax"], out var ymax);
        if (ymin == 0 && ymax == 0) { ymin = 0; ymax = 1; }

        try
        {
            if (mode == "server")
            {
                await RunServerAsync(port);
            }
            else if (mode == "client")
            {
                using var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
                await RunClientAsync(serverAddress, wave, ymin, ymax, cts.Token);
            }
            else
            {
                ShowUsage();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static async Task RunServerAsync(string port)
    {
        var builder = WebApplication.CreateBuilder();
        
        builder.Services.AddGrpc();
        builder.Services.AddSignalR();
        
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("http://localhost:5173")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        builder.Services.Configure<KestrelServerOptions>(options =>
        {
            var portNum = int.Parse(port);
            options.ListenLocalhost(portNum, o => o.Protocols = HttpProtocols.Http1AndHttp2);
            options.ListenLocalhost(5001, o => { o.UseHttps(); o.Protocols = HttpProtocols.Http2; });
        });
        
        builder.WebHost.UseUrls($"http://localhost:{port}");
        
        var app = builder.Build();
        
        app.UseCors();
        app.MapGrpcService<CommunicationServiceImpl>();
        app.MapHub<DataHub>("/chathub");
        
        Console.WriteLine($"Server started on port {port}");
        await app.RunAsync();
    }

    private static async Task RunClientAsync(string serverAddress, string wave, decimal ymin, decimal ymax, CancellationToken cancellationToken)
    {
        var clientService = new GrpcClientService();
        await clientService.RunClientStreamingAsync(serverAddress, wave, ymin, ymax, cancellationToken);
    }

    private static void ShowUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  Server: dotnet run --mode=server [--port=5000]");
        Console.WriteLine("  Client: dotnet run --mode=client [--server=https://localhost:5001] [--wave=sine] [--ymin=0] [--ymax=1]");
    }
}