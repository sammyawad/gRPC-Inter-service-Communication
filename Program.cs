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
                await RunClientAsync(serverAddress);
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
        
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins("http://localhost:5173") // Vue dev server
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        // Configure Kestrel to support both gRPC (HTTP/2) and WebSocket (HTTP/1.1)
        builder.Services.Configure<KestrelServerOptions>(options =>
        {
            options.ListenLocalhost(int.Parse(port), o => o.Protocols = HttpProtocols.Http1AndHttp2);
        });
        // ← END of new services ↑
        
        builder.WebHost.UseUrls($"http://localhost:{port}");
        
        var app = builder.Build();
        app.UseCors("AllowFrontend");
        app.MapGrpcService<CommunicationServiceImpl>();
        app.MapHub<ChatWebSocketHub>("/chathub");
        
        // ← ADD health check endpoint ↓
        app.MapGet("/health", () => new { Status = "Healthy", Server = "gRPC Chat Demo" });
        
        Console.WriteLine($"gRPC Server starting on port {port}...");
        Console.WriteLine($"WebSocket endpoint available at: wss://localhost:{port}/chathub");
        Console.WriteLine($"Health check available at: https://localhost:{port}/health");
        
        await app.RunAsync();
    }

    private static async Task RunClientAsync(string serverAddress)
    {
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole());
        services.AddSingleton<GrpcClientService>();

        var serviceProvider = services.BuildServiceProvider();
        var clientService = serviceProvider.GetRequiredService<GrpcClientService>();

        Console.WriteLine($"Starting gRPC client, connecting to {serverAddress}...");
        
        await clientService.RunBidirectionalCommunicationAsync(serverAddress);
        
        Console.WriteLine("Communication completed. Press any key to exit.");
        Console.ReadKey();
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