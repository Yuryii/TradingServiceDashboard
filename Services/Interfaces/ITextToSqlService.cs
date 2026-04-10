namespace Dashboard.Services.Interfaces;

public class Text2SqlRequest
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int SessionId { get; set; }
}

public class Text2SqlResponse
{
    public bool Success { get; set; }
    public string? GeneratedSql { get; set; }
    public string? Result { get; set; }
    public string? Error { get; set; }
    public bool UsedFallback { get; set; }
}

public interface ITextToSqlService
{
    Task<Text2SqlResponse> AskAsync(Text2SqlRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> StreamAnswerAsync(Text2SqlRequest request, CancellationToken cancellationToken = default);
    Task<string> GenerateSqlOnlyAsync(string question, string department, CancellationToken cancellationToken = default);
}
