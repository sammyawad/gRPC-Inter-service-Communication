//https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs?view=aspnetcore-9.0
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

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

            // Notify all clients that user joined
            await Clients.All.SendAsync("UserJoined", new
            {
                Username = username,
                Avatar = avatar,
                Message = $"{username} joined the chat",
                Timestamp = DateTime.UtcNow,
                Type = "system"
            });

            // Send current online users to the new client
            var onlineUsers = _connectedUsers.Values.Select(u => new
            {
                Id = u.ConnectionId,
                Username = u.Username,
                Avatar = u.Avatar
            }).ToList();

            await Clients.Caller.SendAsync("OnlineUsersUpdate", onlineUsers);

            _logger.LogInformation($"User {username} joined via WebSocket");
        }

        public async Task SendMessage(string content)
        {
            if (_connectedUsers.TryGetValue(Context.ConnectionId, out var user))
            {
                // Create message (this is where we bridge to your gRPC logic)
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

                // Broadcast to all WebSocket clients
                // This mimics what your gRPC bidirectional streaming does
                await Clients.All.SendAsync("ReceiveMessage", message);

                _logger.LogInformation($"Message from {user.Username}: {content}");
            }
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
                await Clients.All.SendAsync("UserLeft", new
                {
                    Username = user.Username,
                    Message = $"{user.Username} left the chat",
                    Type = "system",
                    Timestamp = DateTime.UtcNow
                });

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