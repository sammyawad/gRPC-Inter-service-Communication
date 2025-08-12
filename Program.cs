using GrpcService.Services;
using GrpcService.Client;
using GrpcService.Hubs;  
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
        var serverAddress = configuration["server"] ?? $"https://localhost:{port}";
        var wave = configuration["wave"] ?? "sine"; // sine | square | saw

        Console.WriteLine("=== gRPC Bidirectional Communication Demo ===");
        Console.WriteLine($"Mode: {mode}");
        Console.WriteLine($"Port: {port}");
        Console.WriteLine($"Server Address: {serverAddress}");
        Console.WriteLine("==============================================");

        try
        {
            if (mode.ToLower() == "server")
            {
                await RunServerAsync(port);
            }
            else if (mode.ToLower() == "client")
            {
                using var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (sender, e) =>
                {
                    // Prevent immediate process termination; perform graceful shutdown instead
                    e.Cancel = true;
                    if (!cts.IsCancellationRequested)
                    {
                        Console.WriteLine("\nCtrl+C pressed. Shutting down client...");
                        cts.Cancel();
                    }
                };

                await RunClientAsync(serverAddress, wave, cts.Token);
            }
            else
            {
                Console.WriteLine("Invalid mode. Use --mode=server or --mode=client");
                ShowUsage();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Application error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static async Task RunServerAsync(string port)
    {
        var builder = WebApplication.CreateBuilder();
        
        // Your existing gRPC service
        builder.Services.AddGrpc();
        
        // WebSocket 
        builder.Services.AddSignalR();

        // Data store for tracking client decimal values
        builder.Services.AddSingleton<Services.DataStore>();
        
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins(
                        "http://localhost:5173",  // Vue dev server
                        "http://localhost:5174",  // Alt Vue dev server
                        "http://localhost:3000",  // Docker frontend
                        "http://frontend:80"      // Docker internal network
                      )
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        // Configure Kestrel to support both gRPC (HTTP/2) and WebSocket (HTTP/1.1)
        builder.Services.Configure<KestrelServerOptions>(options =>
        {
            // Check if running in Docker (Production environment)
            if (builder.Environment.IsProduction())
            {
                // HTTP endpoint for health and SignalR
                options.ListenAnyIP(int.Parse(port), o => o.Protocols = HttpProtocols.Http1AndHttp2);
                // HTTPS endpoint dedicated to gRPC over HTTP/2
                options.ListenAnyIP(5001, o =>
                {
                    o.UseHttps();
                    o.Protocols = HttpProtocols.Http2;
                });
            }
            else
            {
                // HTTP endpoint for health and SignalR
                options.ListenLocalhost(int.Parse(port), o => o.Protocols = HttpProtocols.Http1AndHttp2);
                // HTTPS endpoint dedicated to gRPC over HTTP/2
                options.ListenLocalhost(5001, o =>
                {
                    o.UseHttps();
                    o.Protocols = HttpProtocols.Http2;
                });
            }
        });
        
        // Only set URLs for development - let Docker environment variable take precedence
        if (!builder.Environment.IsProduction())
        {
            // Keep HTTP on the chosen port for health checks and SignalR UI
            builder.WebHost.UseUrls($"http://localhost:{port}");
        }
        
        var app = builder.Build();
        // Log endpoints for convenience
        Console.WriteLine($"gRPC (HTTPS/2) endpoint at: https://localhost:5001");
        app.UseCors("AllowFrontend");
        app.MapGrpcService<CommunicationServiceImpl>();
        app.MapHub<ChatWebSocketHub>("/chathub");
        
        // ADD health check endpoint
        app.MapGet("/health", () => new { Status = "Healthy", Server = "gRPC Chat Demo" });
        
        Console.WriteLine($"gRPC Server starting on port {port}...");
        Console.WriteLine($"WebSocket endpoint available at: ws://localhost:{port}/chathub");
        Console.WriteLine($"Health check available at: http://localhost:{port}/health");
        
        await app.RunAsync();
    }

    private static async Task RunClientAsync(string serverAddress, string wave, CancellationToken cancellationToken)
    {
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole());
        services.AddSingleton<GrpcClientService>();

        var serviceProvider = services.BuildServiceProvider();
        var clientService = serviceProvider.GetRequiredService<GrpcClientService>();

        Console.WriteLine($"Starting gRPC client, connecting to {serverAddress}...");
        
        await clientService.RunBidirectionalCommunicationAsync(serverAddress, wave, cancellationToken);
        
        Console.WriteLine("Client stopped.");
    }

    private static void ShowUsage()
    {
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  Server mode:");
        Console.WriteLine("    dotnet run --mode=server [--port=5000]");
        Console.WriteLine("    - Starts gRPC server + WebSocket for browser clients");
        Console.WriteLine();
        Console.WriteLine("  Client mode:");
        Console.WriteLine("    dotnet run --mode=client [--server=https://localhost:5000]");
        Console.WriteLine("    - Connects as gRPC client for testing");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run --mode=server --port=5001");
        Console.WriteLine("  dotnet run --mode=client --server=https://localhost:5001");
        Console.WriteLine();
        Console.WriteLine("Note: This demo uses bidirectional streaming for real-time communication.");
        Console.WriteLine("      Browser clients connect via WebSocket, console clients via gRPC.");
    }
}