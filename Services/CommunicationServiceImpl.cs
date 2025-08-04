using Grpc.Core;
using GrpcService.Protos;
using System.Collections.Concurrent;

namespace GrpcService.Services;

public class CommunicationServiceImpl : CommunicationService.CommunicationServiceBase
{
    private readonly ILogger<CommunicationServiceImpl> _logger;
    private readonly string _serverId;
    private readonly ConcurrentQueue<MessageResponse> _messages;
    private readonly ConcurrentDictionary<string, IServerStreamWriter<ChatMessage>> _chatClients;

    public CommunicationServiceImpl(ILogger<CommunicationServiceImpl> logger)
    {
        _logger = logger;
        _serverId = Environment.MachineName + "_" + Guid.NewGuid().ToString("N")[..8];
        _messages = new ConcurrentQueue<MessageResponse>();
        _chatClients = new ConcurrentDictionary<string, IServerStreamWriter<ChatMessage>>();
    }

    public override Task<MessageResponse> SendMessage(MessageRequest request, ServerCallContext context)
    {
        _logger.LogInformation($"Received message from {request.SenderId}: {request.Content}");

        var response = new MessageResponse
        {
            MessageId = Guid.NewGuid().ToString(),
            SenderId = request.SenderId,
            Content = $"Echo: {request.Content}",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Success = true,
            ErrorMessage = string.Empty
        };

        // Store the message for streaming
        _messages.Enqueue(response);

        _logger.LogInformation($"Responding to {request.SenderId} with message ID: {response.MessageId}");

        return Task.FromResult(response);
    }

    public override async Task GetMessages(Empty request, IServerStreamWriter<MessageResponse> responseStream, ServerCallContext context)
    {
        _logger.LogInformation("Client requested message stream");

        // Send existing messages
        var messageList = _messages.ToArray();
        foreach (var message in messageList)
        {
            if (context.CancellationToken.IsCancellationRequested)
                break;

            await responseStream.WriteAsync(message);
            await Task.Delay(100); // Small delay between messages
        }

        // Keep streaming new messages
        var lastCount = _messages.Count;
        while (!context.CancellationToken.IsCancellationRequested)
        {
            if (_messages.Count > lastCount)
            {
                var newMessages = _messages.Skip(lastCount).ToArray();
                foreach (var message in newMessages)
                {
                    await responseStream.WriteAsync(message);
                }
                lastCount = _messages.Count;
            }
            await Task.Delay(1000); // Check for new messages every second
        }

        _logger.LogInformation("Message stream ended");
    }

    public override async Task<MessageSummary> SendMultipleMessages(IAsyncStreamReader<MessageRequest> requestStream, ServerCallContext context)
    {
        _logger.LogInformation("Receiving multiple messages from client");

        var messageIds = new List<string>();
        var successCount = 0;
        var failCount = 0;

        while (await requestStream.MoveNext())
        {
            var request = requestStream.Current;
            try
            {
                var messageId = Guid.NewGuid().ToString();
                _logger.LogInformation($"Processing message {messageIds.Count + 1} from {request.SenderId}: {request.Content}");

                var response = new MessageResponse
                {
                    MessageId = messageId,
                    SenderId = request.SenderId,
                    Content = request.Content,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Success = true
                };

                _messages.Enqueue(response);
                messageIds.Add(messageId);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                failCount++;
            }
        }

        var summary = new MessageSummary
        {
            TotalMessages = messageIds.Count + failCount,
            SuccessfulMessages = successCount,
            FailedMessages = failCount
        };
        summary.MessageIds.AddRange(messageIds);

        _logger.LogInformation($"Processed {summary.TotalMessages} messages: {summary.SuccessfulMessages} successful, {summary.FailedMessages} failed");

        return summary;
    }

    public override async Task Chat(IAsyncStreamReader<ChatMessage> requestStream, IServerStreamWriter<ChatMessage> responseStream, ServerCallContext context)
    {
        var clientId = context.GetHttpContext().Connection.Id;
        _logger.LogInformation($"Client {clientId} joined chat");

        _chatClients.TryAdd(clientId, responseStream);

        try
        {
            // Send welcome message
            var welcomeMessage = new ChatMessage
            {
                UserId = "System",
                Message = $"Welcome to the chat! Server ID: {_serverId}",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            await responseStream.WriteAsync(welcomeMessage);

            // Process incoming messages
            while (await requestStream.MoveNext())
            {
                var message = requestStream.Current;
                _logger.LogInformation($"Chat message from {message.UserId}: {message.Message}");

                // Broadcast to all connected clients
                var broadcastMessage = new ChatMessage
                {
                    UserId = message.UserId,
                    Message = message.Message,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                var disconnectedClients = new List<string>();
                foreach (var (id, stream) in _chatClients)
                {
                    try
                    {
                        await stream.WriteAsync(broadcastMessage);
                    }
                    catch
                    {
                        disconnectedClients.Add(id);
                    }
                }

                // Remove disconnected clients
                foreach (var id in disconnectedClients)
                {
                    _chatClients.TryRemove(id, out _);
                }
            }
        }
        finally
        {
            _chatClients.TryRemove(clientId, out _);
            _logger.LogInformation($"Client {clientId} left chat");
        }
    }

    public override Task<HealthResponse> HealthCheck(Empty request, ServerCallContext context)
    {
        var response = new HealthResponse
        {
            Status = "Healthy",
            ServerId = _serverId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        _logger.LogInformation($"Health check requested - Status: {response.Status}");

        return Task.FromResult(response);
    }
}
