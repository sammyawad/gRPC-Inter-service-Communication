using GrpcService.Services;
using GrpcService.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        var clientMode = configuration["client-mode"] ?? "unary";

        Console.WriteLine("=== gRPC Inter-service Communication Demo ===");
        Console.WriteLine($"Mode: {mode}");
        Console.WriteLine($"Port: {port}");
        Console.WriteLine($"Server Address: {serverAddress}");
        
        if (mode.ToLower() == "client")
        {
            Console.WriteLine($"Client Mode: {clientMode}");
        }
        
        Console.WriteLine("==========================================");

        try
        {
            if (mode.ToLower() == "server")
            {
                await RunServerAsync(port);
            }
            else if (mode.ToLower() == "client")
            {
                await RunClientAsync(serverAddress, clientMode);
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
        
        builder.Services.AddGrpc();
        builder.WebHost.UseUrls($"https://localhost:{port}");
        
        var app = builder.Build();
        app.MapGrpcService<CommunicationServiceImpl>();
        
        Console.WriteLine($"gRPC Server starting on port {port}...");
        await app.RunAsync();
    }

    private static async Task RunClientAsync(string serverAddress, string clientMode)
    {
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole());
        services.AddSingleton<GrpcClientService>();

        var serviceProvider = services.BuildServiceProvider();
        var clientService = serviceProvider.GetRequiredService<GrpcClientService>();

        Console.WriteLine($"Starting gRPC client, connecting to {serverAddress}...");
        
        await clientService.RunClientAsync(serverAddress, clientMode);
        
        Console.WriteLine("Client operations completed. Press any key to exit.");
        Console.ReadKey();
    }

    private static void ShowUsage()
    {
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  Server mode:");
        Console.WriteLine("    dotnet run --mode=server [--port=5000]");
        Console.WriteLine();
        Console.WriteLine("  Client mode:");
        Console.WriteLine("    dotnet run --mode=client [--server=https://localhost:5000] [--client-mode=unary|serverstream|clientstream|chat|all]");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run --mode=server --port=5001");
        Console.WriteLine("  dotnet run --mode=client --server=https://localhost:5001 --client-mode=chat");
        Console.WriteLine("  dotnet run --mode=client --server=https://localhost:5000 --client-mode=all");
        Console.WriteLine();
        Console.WriteLine("Client modes:");
        Console.WriteLine("  unary       - Simple request-response");
        Console.WriteLine("  serverstream - Server streaming messages");
        Console.WriteLine("  clientstream - Client streaming messages");
        Console.WriteLine("  chat        - Bidirectional streaming (chat)");
        Console.WriteLine("  all         - Run unary, client stream, then server stream");
    }
}
