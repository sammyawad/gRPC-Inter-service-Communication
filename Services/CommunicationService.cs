using Grpc.Core;
using GrpcService.Protos;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Google.Protobuf.WellKnownTypes;

namespace GrpcService.Services;

// Client presence tracking only
public static class ClientTracker
{
    private static readonly ConcurrentDictionary<string, byte> _dataClientPresence = new();
    
    public static bool TryAddClient(string userId) => _dataClientPresence.TryAdd(userId, 1);
    public static bool RemoveClient(string userId) => _dataClientPresence.TryRemove(userId, out _);
}

// gRPC Service
public class CommunicationServiceImpl : CommunicationService.CommunicationServiceBase
{
    private readonly IHubContext<DataHub> _hubContext;

    public CommunicationServiceImpl(IHubContext<DataHub> hubContext)
    {
        _hubContext = hubContext;
    }

    // Client-streaming: consume incoming messages and broadcast via SignalR, return Empty when done
    public override async Task<Empty> SendData(IAsyncStreamReader<ChatMessage> requestStream, ServerCallContext context)
    {
        string? dataUserId = null;

        try
        {
            await foreach (var message in requestStream.ReadAllAsync())
            {
                if (dataUserId is null && !string.IsNullOrWhiteSpace(message.UserId))
                {
                    dataUserId = message.UserId;
                    if (ClientTracker.TryAddClient(dataUserId))
                    {
                        await _hubContext.Clients.All.SendAsync("DataClientJoined", new {
                            UserId = dataUserId,
                            Username = dataUserId,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }

                var value = message.PreciseFractionDecimal;
                if (value.HasValue)
                {
                    await _hubContext.Clients.All.SendAsync("DataUpdated", new {
                        UserId = message.UserId,
                        Value = value.Value,
                        Timestamp = DateTime.UtcNow,
                        Mode = message.Message,
                        YMin = message.YMin,
                        YMax = message.YMax
                    });
                }
            }
        }
        finally
        {
            if (dataUserId is not null)
            {
                ClientTracker.RemoveClient(dataUserId);
                await _hubContext.Clients.All.SendAsync("DataClientLeft", new {
                    UserId = dataUserId,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        return new Empty();
    }
}

// WebSocket Hub
public class DataHub : Hub
{
    // Frontend handles its own data storage - no server-side history needed
}
