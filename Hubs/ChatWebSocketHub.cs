//https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs?view=aspnetcore-9.0
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

namespace GrpcService.Hubs
{
    public class ChatWebSocketHub : Hub
    {
        private static readonly ConcurrentDictionary<string, UserInfo> _connectedUsers = new();
        private readonly ILogger<ChatWebSocketHub> _logger;

        public ChatWebSocketHub(ILogger<ChatWebSocketHub> logger)
        {
            _logger = logger;
        }

        public async Task JoinChat(string username, string avatar)
        {
            // Store user info
            _connectedUsers[Context.ConnectionId] = new UserInfo
            {
                Username = username,
                Avatar = avatar,
                ConnectionId = Context.ConnectionId
            };

            // Notify all clients that user joined (include ConnectionId)
            await Clients.All.SendAsync("UserJoined", new
            {
                Username = username,
                Avatar = avatar,
                ConnectionId = Context.ConnectionId,
                Message = $"{username} joined the chat",
                Timestamp = DateTime.UtcNow,
                Type = "system"
            });

            // Build current online users
            var onlineUsers = _connectedUsers.Values.Select(u => new
            {
                Id = u.ConnectionId,
                Username = u.Username,
                Avatar = u.Avatar
            }).ToList();

            // Send current users to the new client
            await Clients.Caller.SendAsync("OnlineUsersUpdate", onlineUsers);
            // Also broadcast updated presence to everyone
            await Clients.All.SendAsync("OnlineUsersUpdate", onlineUsers);

            _logger.LogInformation($"User {username} joined via WebSocket");
        }

        // Kept for compatibility, but now optional in a data-driven app
        public async Task SendMessage(string content)
        {
            if (_connectedUsers.TryGetValue(Context.ConnectionId, out var user))
            {
                var message = new
                {
                    Id = Guid.NewGuid().ToString(),
                    Username = user.Username,
                    Avatar = user.Avatar,
                    Content = content,
                    Timestamp = DateTime.UtcNow,
                    Type = "message",
                    UserId = Context.ConnectionId
                };

                await Clients.All.SendAsync("ReceiveMessage", message);
                _logger.LogInformation($"Message from {user.Username}: {content}");
            }
        }

        // Returns recent history per client for chart hydration
        public Task<object> GetCurrentData([FromServices] GrpcService.Services.DataStore dataStore)
        {
            var history = dataStore.SnapshotHistory()
                .Select(series => new
                {
                    UserId = series.Key,
                    Points = series.Value.Select(p => new { Timestamp = p.Timestamp, Value = p.Value }).ToList()
                })
                .ToList<object>();
            return Task.FromResult<object>(history);
        }

        public async Task SendTypingIndicator(bool isTyping)
        {
            if (_connectedUsers.TryGetValue(Context.ConnectionId, out var user))
            {
                await Clients.Others.SendAsync("UserTyping", new
                {
                    UserId = Context.ConnectionId,
                    Username = user.Username,
                    IsTyping = isTyping
                });
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connectedUsers.TryRemove(Context.ConnectionId, out var user))
            {
                // Notify all clients (include ConnectionId)
                await Clients.All.SendAsync("UserLeft", new
                {
                    Username = user.Username,
                    ConnectionId = Context.ConnectionId,
                    Message = $"{user.Username} left the chat",
                    Type = "system",
                    Timestamp = DateTime.UtcNow
                });

                // Broadcast updated presence to everyone
                var onlineUsers = _connectedUsers.Values.Select(u => new
                {
                    Id = u.ConnectionId,
                    Username = u.Username,
                    Avatar = u.Avatar
                }).ToList();
                await Clients.All.SendAsync("OnlineUsersUpdate", onlineUsers);

                _logger.LogInformation($"User {user.Username} disconnected");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }

    public class UserInfo
    {
        public string Username { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
    }
}