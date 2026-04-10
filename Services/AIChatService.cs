using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Dashboard.Data;
using Dashboard.Models;
using Dashboard.Models.ViewModels;
using Dashboard.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Services;

public class AIChatService : IAIChatService
{
    private readonly ApplicationDbContext _context;
    private readonly AIContextAggregator _contextAggregator;
    private readonly ITextToSqlService _textToSqlService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AIChatService> _logger;
    private readonly IConfiguration _configuration;

    private string LlmEndpoint => _configuration["AIChat:Endpoint"] ?? "https://routerapi.vovantin.online/v1/chat/completions";
    private string ApiKey => _configuration["AIChat:ApiKey"] ?? "";
    private string DefaultModel => _configuration["AIChat:Model"] ?? "gpt-4o-mini";
    private int MaxContextTokens => int.Parse(_configuration["AIChat:MaxTokens"] ?? "4000");
    private int MaxHistoryMessages => int.Parse(_configuration["AIChat:MaxHistoryMessages"] ?? "10");

    public AIChatService(
        ApplicationDbContext context,
        AIContextAggregator contextAggregator,
        ITextToSqlService textToSqlService,
        IHttpClientFactory httpClientFactory,
        ILogger<AIChatService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _contextAggregator = contextAggregator;
        _textToSqlService = textToSqlService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<ChatSessionDto> CreateSessionAsync(string userId, string department)
    {
        department = NormalizeDepartmentKey(department);

        var session = new AIChatSession
        {
            UserId = userId,
            Department = department,
            Title = $"Chat {department} - {DateTime.Now:dd/MM HH:mm}",
            CreatedAt = DateTime.UtcNow,
            LastMessageAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.AIChatSessions.Add(session);
        await _context.SaveChangesAsync();

        return MapToDto(session, 0);
    }

    public async Task<List<ChatSessionDto>> GetSessionsAsync(string userId, string? department = null)
    {
        var query = _context.AIChatSessions
            .Where(s => s.UserId == userId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(department))
        {
            var d = NormalizeDepartmentKey(department);
            query = query.Where(s => s.Department.ToLower() == d);
        }

        return await query
            .OrderByDescending(s => s.LastMessageAt)
            .Take(20)
            .Select(s => new ChatSessionDto
            {
                SessionId = s.SessionId,
                Department = s.Department,
                Title = s.Title,
                CreatedAt = s.CreatedAt,
                LastMessageAt = s.LastMessageAt,
                MessageCount = s.Messages.Count
            })
            .ToListAsync();
    }

    private static string NormalizeDepartmentKey(string department) =>
        department.Trim().ToLowerInvariant();

    public async Task<List<ChatMessageDto>> GetChatHistoryAsync(int sessionId, int page = 1, int pageSize = 50)
    {
        var messages = await _context.AIChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessageDto
            {
                MessageId = m.MessageId,
                Role = m.Role.ToString(),
                Content = m.Content,
                CreatedAt = m.CreatedAt
            })
            .ToList();
    }

    public async Task<bool> DeleteSessionAsync(int sessionId, string userId)
    {
        var session = await _context.AIChatSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.UserId == userId);

        if (session == null) return false;

        _context.AIChatSessions.Remove(session);
        await _context.SaveChangesAsync();
        return true;
    }

    public async IAsyncEnumerable<string> StreamResponseAsync(
        AIChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var contextData = await _contextAggregator.GetContextAsync(
            request.Department, request.UserName, request.Role);

        var history = await GetChatHistoryAsync(request.SessionId);
        var systemPrompt = BuildSystemPrompt(request.Department, contextData);
        var messages = BuildMessages(systemPrompt, history, request.Message);

        var client = _httpClientFactory.CreateClient("AIChat");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiKey);

        var payload = new
        {
            model = DefaultModel,
            messages,
            stream = true,
            max_tokens = MaxContextTokens,
            temperature = 0.7
        };

        var requestContent = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var httpResponse = await client.PostAsync(LlmEndpoint, requestContent, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        await using var responseStream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(responseStream);

        var fullResponse = new System.Text.StringBuilder();

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(line)) continue;

            if (line.StartsWith("data: "))
            {
                var data = line["data: ".Length..];
                if (data == "[DONE]") break;

                string? token = null;
                try
                {
                    using var doc = JsonDocument.Parse(data);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("choices", out var choices) &&
                        choices.GetArrayLength() > 0)
                    {
                        var delta = choices[0];
                        if (delta.TryGetProperty("delta", out var deltaObj) &&
                            deltaObj.TryGetProperty("content", out var contentToken))
                        {
                            token = contentToken.GetString();
                        }
                    }
                }
                catch (JsonException)
                {
                    token = null;
                }

                if (!string.IsNullOrEmpty(token))
                {
                    fullResponse.Append(token);
                    yield return token;
                }
            }
        }

        if (fullResponse.Length > 0)
        {
            await SaveMessageAsync(request.SessionId, ChatRole.Assistant, fullResponse.ToString());
        }
    }

    public async Task<string> GetFullResponseAsync(
        AIChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var contextData = await _contextAggregator.GetContextAsync(
            request.Department, request.UserName, request.Role);

        await SaveMessageAsync(request.SessionId, ChatRole.User, request.Message);

        var history = await GetChatHistoryAsync(request.SessionId);
        var systemPrompt = BuildSystemPrompt(request.Department, contextData);
        var messages = BuildMessages(systemPrompt, history, request.Message);

        var client = _httpClientFactory.CreateClient("AIChat");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiKey);

        var payload = new
        {
            model = DefaultModel,
            messages,
            stream = false,
            max_tokens = MaxContextTokens,
            temperature = 0.7
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = await client.PostAsync(LlmEndpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        var reply = root.GetProperty("choices")[0]
            .GetProperty("message").GetProperty("content").GetString() ?? "";

        await SaveMessageAsync(request.SessionId, ChatRole.Assistant, reply);
        return reply;
    }

    private string BuildSystemPrompt(string department, AIContextData context)
    {
        var dateRange = $"From {DateTime.Now.AddMonths(-1):dd/MM/yyyy} to {DateTime.Now:dd/MM/yyyy}";

        var departmentInstructions = department.ToLower() switch
        {
            "sales" => @"You are an AI Assistant specializing in Sales Dashboard.
Task: Support sales data analysis, provide insights on revenue, orders, customers, products, and sales team.
When responding:
- Always cite specific figures from the provided data.
- Provide actionable insights when appropriate.
- Respond concisely with structure (bullet points, tables if needed).
- If a question is unrelated to Sales, politely decline and suggest switching to Sales topics.",

            "finance" => @"You are an AI Assistant specializing in Finance Dashboard.
Task: Support financial data analysis, income/expense, cash flow, profit, and receivables.
When responding:
- Always cite specific figures from the provided data.
- Warn if there are abnormal indicators (sudden cost increases, negative cash flow...).
- Provide suggestions to improve financial situation.
- Respond concisely with structure.
- If a question is unrelated to Finance, politely decline.",

            "marketing" => @"You are an AI Assistant specializing in Marketing Dashboard.
Task: Support marketing campaign analysis, channel performance, leads, conversions, ROI and ROAS.
When responding:
- Always cite specific figures from the provided data.
- Analyze marketing cost effectiveness (CPL, ROAS, ROI).
- Suggest campaign improvements and budget allocation.
- Respond concisely with structure.
- If a question is unrelated to Marketing, politely decline and suggest switching to Marketing topics.",

            "inventory" => @"You are an AI Assistant specializing in Inventory Dashboard.
Task: Support inventory analysis, stock in/out, warehouse status, and inventory turnover.
When responding:
- Always cite specific figures from the provided data.
- Warn about products running low, overstock.
- Suggest ordering strategy and inventory optimization.
- Respond concisely with structure.
- If a question is unrelated to Inventory, politely decline and suggest switching to Inventory topics.",

            "hr" => @"You are an AI Assistant specializing in Human Resources Dashboard.
Task: Support HR analysis, recruitment, attrition, salary, and employee performance.
When responding:
- Always cite specific figures from the provided data.
- Analyze turnover rate, employee retention.
- Suggest recruitment strategies and improve company culture.
- Respond concisely with structure.
- If a question is unrelated to HR, politely decline and suggest switching to HR topics.",

            "cskh" => @"You are an AI Assistant specializing in Customer Service Dashboard.
Task: Support support ticket analysis, CSAT, SLA, response time, and CS employee performance.
When responding:
- Always cite specific figures from the provided data.
- Analyze complaint trends, common causes.
- Suggest improving service quality.
- Respond concisely with structure.
- If a question is unrelated to CS, politely decline and suggest switching to CS topics.",

            "executive" => @"You are an AI Assistant specializing in Executive Dashboard.
Task: Support comprehensive company operations analysis, including revenue, costs, profit, HR, and departments.
When responding:
- Always cite specific figures from the provided data.
- Provide holistic view for leadership.
- Issue warnings and strategic suggestions at company level.
- Respond concisely with structure (bullet points, tables).
- Can answer cross-department questions as this is an Executive dashboard.",

            _ => @"You are an AI Assistant for Enterprise Dashboard.
Support users in data analysis and business decision-making."
        };

        return $@"{departmentInstructions}

Current user information:
- Name: {context.UserName}
- Role: {context.UserRole}
- Department: {context.Department}
- Data time range: {dateRange}

Current KPI data:
{context.KpiSummary}

{context.TopItemsSummary}

{context.RecentDataSummary}

{context.ChartSummary}

Please respond in English, professionally, with specific figures.";
    }

    private List<object> BuildMessages(string systemPrompt, List<ChatMessageDto> history, string currentMessage)
    {
        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };

        foreach (var msg in history.TakeLast(MaxHistoryMessages))
        {
            messages.Add(new { role = msg.Role.ToLower(), content = msg.Content });
        }

        messages.Add(new { role = "user", content = currentMessage });

        return messages;
    }

    public async IAsyncEnumerable<string> StreamText2SqlResponseAsync(
        AIChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var text2SqlRequest = new Text2SqlRequest
        {
            SessionId = request.SessionId,
            UserId = request.UserId,
            UserName = request.UserName,
            Role = request.Role,
            Department = request.Department,
            Message = request.Message
        };

        await foreach (var chunk in _textToSqlService.StreamAnswerAsync(text2SqlRequest, cancellationToken))
        {
            if (chunk == "[STREAM_END]")
                yield break;

            yield return chunk;
        }
    }

    public async Task<Text2SqlResponse> GetText2SqlResponseAsync(
        AIChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var text2SqlRequest = new Text2SqlRequest
        {
            SessionId = request.SessionId,
            UserId = request.UserId,
            UserName = request.UserName,
            Role = request.Role,
            Department = request.Department,
            Message = request.Message
        };

        var result = await _textToSqlService.AskAsync(text2SqlRequest, cancellationToken);

        if (result.Success && !string.IsNullOrEmpty(result.Result))
        {
            await SaveMessageAsync(request.SessionId, ChatRole.Assistant, result.Result);
        }

        return result;
    }

    private async Task SaveMessageAsync(int sessionId, ChatRole role, string content)
    {
        var message = new AIChatMessage
        {
            SessionId = sessionId,
            Role = role,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        _context.AIChatMessages.Add(message);

        var session = await _context.AIChatSessions.FindAsync(sessionId);
        if (session != null)
            session.LastMessageAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    private static ChatSessionDto MapToDto(AIChatSession session, int messageCount)
    {
        return new ChatSessionDto
        {
            SessionId = session.SessionId,
            Department = session.Department,
            Title = session.Title,
            CreatedAt = session.CreatedAt,
            LastMessageAt = session.LastMessageAt,
            MessageCount = messageCount
        };
    }
}
