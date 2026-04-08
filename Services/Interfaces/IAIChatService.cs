using Dashboard.Models;

namespace Dashboard.Services.Interfaces;

public class AIChatRequest
{
    public int SessionId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class AIChatResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? SessionId { get; set; }
}

public class ChatSessionDto
{
    public int SessionId { get; set; }
    public string Department { get; set; } = string.Empty;
    public string? Title { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastMessageAt { get; set; }
    public int MessageCount { get; set; }
}

public class ChatMessageDto
{
    public int MessageId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public interface IAIChatService
{
    Task<ChatSessionDto> CreateSessionAsync(string userId, string department);
    Task<List<ChatSessionDto>> GetSessionsAsync(string userId, string? department = null);
    Task<List<ChatMessageDto>> GetChatHistoryAsync(int sessionId, int page = 1, int pageSize = 50);
    Task<bool> DeleteSessionAsync(int sessionId, string userId);
    IAsyncEnumerable<string> StreamResponseAsync(AIChatRequest request, CancellationToken cancellationToken = default);
    Task<string> GetFullResponseAsync(AIChatRequest request, CancellationToken cancellationToken = default);
}
