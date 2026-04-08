using System.Security.Claims;
using Dashboard.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Dashboard.Hubs;

[Authorize]
public class AIChatHub : Hub
{
    private readonly IAIChatService _chatService;
    private readonly ILogger<AIChatHub> _logger;

    public AIChatHub(IAIChatService chatService, ILogger<AIChatHub> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public async Task SendMessage(int sessionId, string department, string message)
    {
        var userId = GetUserId();
        var userName = Context.User?.FindFirst("UserName")?.Value
                      ?? Context.User?.FindFirst(ClaimTypes.Name)?.Value
                      ?? "User";
        var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value
                      ?? "Unknown";

        try
        {
            var request = new AIChatRequest
            {
                SessionId = sessionId,
                UserId = userId,
                UserName = userName,
                Role = userRole,
                Department = department,
                Message = message
            };

            await Clients.Caller.SendAsync("TypingIndicator", true);

            await foreach (var chunk in _chatService.StreamResponseAsync(request, Context.ConnectionAborted))
            {
                if (!string.IsNullOrEmpty(chunk))
                {
                    await Clients.Caller.SendAsync("ReceiveChunk", chunk);
                }
            }

            await Clients.Caller.SendAsync("StreamComplete");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Chat stream cancelled for session {SessionId}", sessionId);
            await Clients.Caller.SendAsync("StreamComplete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming chat for session {SessionId}", sessionId);
            await Clients.Caller.SendAsync("ReceiveError", "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại.");
            await Clients.Caller.SendAsync("StreamComplete");
        }
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        }
        await base.OnDisconnectedAsync(exception);
    }

    private string GetUserId()
    {
        return Context.User?.FindFirst("UserId")?.Value
               ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? string.Empty;
    }
}
