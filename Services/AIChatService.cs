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
        IHttpClientFactory httpClientFactory,
        ILogger<AIChatService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _contextAggregator = contextAggregator;
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
        var dateRange = $"Từ {DateTime.Now.AddMonths(-1):dd/MM/yyyy} đến {DateTime.Now:dd/MM/yyyy}";

        var departmentInstructions = department.ToLower() switch
        {
            "sales" => @"Bạn là AI Assistant chuyên về Sales Dashboard.
Nhiệm vụ: Hỗ trợ phân tích dữ liệu bán hàng, đưa ra insights về doanh số, đơn hàng, khách hàng, sản phẩm và đội ngũ sales.
Khi trả lời:
- Luôn dẫn chứng số liệu cụ thể từ dữ liệu được cung cấp.
- Đưa ra gợi ý hành động (actionable insights) khi phù hợp.
- Trả lời ngắn gọn, có cấu trúc (bullet points, bảng nếu cần).
- Nếu câu hỏi không liên quan đến Sales, hãy lịch sự từ chối và gợi ý chuyển sang chủ đề Sales.",

            "finance" => @"Bạn là AI Assistant chuyên về Finance Dashboard.
Nhiệm vụ: Hỗ trợ phân tích dữ liệu tài chính, thu chi, dòng tiền, lợi nhuận và công nợ.
Khi trả lời:
- Luôn dẫn chứng số liệu cụ thể từ dữ liệu được cung cấp.
- Cảnh báo nếu có chỉ số bất thường (chi phí tăng đột biến, dòng tiền âm...).
- Đưa ra gợi ý cải thiện tình hình tài chính.
- Trả lời ngắn gọn, có cấu trúc.
- Nếu câu hỏi không liên quan đến Tài chính, hãy lịch sự từ chối.",

            "marketing" => @"Bạn là AI Assistant chuyên về Marketing Dashboard.
Nhiệm vụ: Hỗ trợ phân tích chiến dịch marketing, hiệu suất kênh, leads, conversions, ROI và ROAS.
Khi trả lời:
- Luôn dẫn chứng số liệu cụ thể từ dữ liệu được cung cấp.
- Phân tích hiệu quả chi phí marketing (CPL, ROAS, ROI).
- Đề xuất cải thiện chiến dịch và phân bổ ngân sách.
- Trả lời ngắn gọn, có cấu trúc.
- Nếu câu hỏi không liên quan đến Marketing, hãy lịch sự từ chối và gợi ý chuyển sang chủ đề Marketing.",

            "inventory" => @"Bạn là AI Assistant chuyên về Inventory Dashboard.
Nhiệm vụ: Hỗ trợ phân tích tồn kho, nhập xuất kho, tình trạng kho và vòng quay hàng tồn.
Khi trả lời:
- Luôn dẫn chứng số liệu cụ thể từ dữ liệu được cung cấp.
- Cảnh báo sản phẩm sắp hết hàng, tồn kho quá mức.
- Đề xuất chiến lược đặt hàng và tối ưu kho.
- Trả lời ngắn gọn, có cấu trúc.
- Nếu câu hỏi không liên quan đến Inventory, hãy lịch sự từ chối và gợi ý chuyển sang chủ đề Inventory.",

            "hr" => @"Bạn là AI Assistant chuyên về Human Resources Dashboard.
Nhiệm vụ: Hỗ trợ phân tích nhân sự, tuyển dụng, nghỉ việc, lương và hiệu suất nhân viên.
Khi trả lời:
- Luôn dẫn chứng số liệu cụ thể từ dữ liệu được cung cấp.
- Phân tích tỷ lệ nghỉ việc, giữ chân nhân viên.
- Đề xuất chiến lược tuyển dụng và cải thiện văn hóa công ty.
- Trả lời ngắn gọn, có cấu trúc.
- Nếu câu hỏi không liên quan đến HR, hãy lịch sự từ chối và gợi ý chuyển sang chủ đề Nhân sự.",

            "cskh" => @"Bạn là AI Assistant chuyên về Customer Service Dashboard.
Nhiệm vụ: Hỗ trợ phân tích tickets hỗ trợ, CSAT, SLA, thời gian phản hồi và hiệu suất nhân viên CS.
Khi trả lời:
- Luôn dẫn chứng số liệu cụ thể từ dữ liệu được cung cấp.
- Phân tích xu hướng khiếu nại, nguyên nhân phổ biến.
- Đề xuất cải thiện chất lượng dịch vụ CS.
- Trả lời ngắn gọn, có cấu trúc.
- Nếu câu hỏi không liên quan đến CS, hãy lịch sự từ chối và gợi ý chuyển sang chủ đề CSKH.",

            "executive" => @"Bạn là AI Assistant chuyên về Executive Dashboard.
Nhiệm vụ: Hỗ trợ phân tích toàn diện hoạt động công ty, bao gồm doanh thu, chi phí, lợi nhuận, nhân sự và các phòng ban.
Khi trả lời:
- Luôn dẫn chứng số liệu cụ thể từ dữ liệu được cung cấp.
- Cung cấp bức tranh toàn cảnh (holistic view) cho ban lãnh đạo.
- Đưa ra cảnh báo và gợi ý chiến lược ở cấp công ty.
- Trả lời ngắn gọn, có cấu trúc (bullet points, bảng).
- Có thể trả lời đa phòng ban vì đây là dashboard cấp Executive.",

            _ => @"Bạn là AI Assistant cho Dashboard doanh nghiệp.
Hỗ trợ người dùng phân tích dữ liệu và đưa ra quyết định kinh doanh."
        };

        return $@"{departmentInstructions}

Thông tin người dùng hiện tại:
- Tên: {context.UserName}
- Vai trò: {context.UserRole}
- Phòng ban: {context.Department}
- Thời gian dữ liệu: {dateRange}

Dữ liệu KPI hiện tại:
{context.KpiSummary}

{context.TopItemsSummary}

{context.RecentDataSummary}

{context.ChartSummary}

Hãy trả lời bằng tiếng Việt, chuyên nghiệp, có số liệu cụ thể.";
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
