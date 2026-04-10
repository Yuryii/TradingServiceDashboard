using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Dashboard.Data;
using Dashboard.Models;
using Dashboard.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Services;

public class TextToSqlService : ITextToSqlService
{
    private readonly ApplicationDbContext _context;
    private readonly IDatabaseSchemaService _schemaService;
    private readonly ISqlValidationService _sqlValidation;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TextToSqlService> _logger;
    private readonly IConfiguration _configuration;

    private string LlmEndpoint => _configuration["AIChat:Endpoint"] ?? "https://routerapi.vovantin.online/v1/chat/completions";
    private string ApiKey => _configuration["AIChat:ApiKey"] ?? "";
    private string Model => _configuration["AIChat:Model"] ?? "gpt-4o-mini";

    public TextToSqlService(
        ApplicationDbContext context,
        IDatabaseSchemaService schemaService,
        ISqlValidationService sqlValidation,
        IHttpClientFactory httpClientFactory,
        ILogger<TextToSqlService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _schemaService = schemaService;
        _sqlValidation = sqlValidation;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<Text2SqlResponse> AskAsync(Text2SqlRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Get schema for department
            var schemaPrompt = await _schemaService.GetSchemaPromptForDepartmentAsync(request.Department);

            // Step 2: Build SQL generation prompt
            var systemPrompt = BuildSqlGenerationPrompt(request.Department);
            var userMessage = $@"Câu hỏi: {request.Message}

Hãy tạo một câu truy vấn SQL dựa trên câu hỏi trên.
{schemaPrompt}";

            // Step 3: Call LLM to generate SQL
            var generatedSql = await CallLlmForSqlAsync(
                systemPrompt, userMessage, temperature: 0.2, cancellationToken);

            if (string.IsNullOrWhiteSpace(generatedSql))
            {
                return new Text2SqlResponse
                {
                    Success = false,
                    Error = "Không thể tạo câu truy vấn SQL từ câu hỏi của bạn.",
                    UsedFallback = false
                };
            }

            _logger.LogInformation("Generated SQL for department {Dept}: {Sql}",
                request.Department, generatedSql);

            // Step 4: Get allowed tables for validation
            var allowedTables = await GetAllowedTablesAsync(request.Department);

            // Step 5: Execute the query
            var (execSuccess, data, execError, formattedResult) =
                await _sqlValidation.ExecuteQueryAsync(generatedSql, allowedTables, cancellationToken);

            if (!execSuccess)
            {
                _logger.LogWarning("SQL execution failed for query: {Sql}. Error: {Error}",
                    generatedSql, execError);

                return new Text2SqlResponse
                {
                    Success = false,
                    GeneratedSql = generatedSql,
                    Error = execError ?? "Câu truy vấn không thể thực thi.",
                    UsedFallback = false
                };
            }

            // Step 6: Format natural language response + extract chart
            var nlResponse = await GenerateNaturalLanguageResponseAsync(
                request.Message, formattedResult!, request.Department, cancellationToken);

            var (textResponse, chartConfig) = ExtractChartConfig(nlResponse);

            // Build result: text response + optional chart marker
            var finalResult = string.IsNullOrEmpty(chartConfig)
                ? textResponse
                : textResponse + "\n\n[CHART_CONFIG:" + chartConfig + "]";

            return new Text2SqlResponse
            {
                Success = true,
                GeneratedSql = generatedSql,
                Result = finalResult,
                UsedFallback = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Text2Sql failed for user message: {Message}", request.Message);
            return new Text2SqlResponse
            {
                Success = false,
                Error = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại.",
                UsedFallback = true
            };
        }
    }

    public async IAsyncEnumerable<string> StreamAnswerAsync(
        Text2SqlRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return "**Đang phân tích câu hỏi và tạo truy vấn SQL...**\n\n";

        // Step 1: Schema
        yield return ":arrow_right: *Lấy cấu trúc database...*\n";
        var schemaPrompt = await _schemaService.GetSchemaPromptForDepartmentAsync(request.Department);
        yield return ":arrow_right: *Đã có schema cho phòng ban `{0}`...*\n".Replace("{0}", request.Department);

        // Step 2: Generate SQL
        yield return ":gear: *Đang gọi AI để tạo SQL...\n";
        var systemPrompt = BuildSqlGenerationPrompt(request.Department);
        var userMessage = $@"Câu hỏi: {request.Message}

{schemaPrompt}";

        var generatedSql = await CallLlmForSqlAsync(
            systemPrompt, userMessage, temperature: 0.2, cancellationToken);

        if (!string.IsNullOrWhiteSpace(generatedSql))
        {
            yield return ":white_check_mark: *Đã tạo SQL:\n```sql\n" + generatedSql + "\n```*\n\n";
        }
        else
        {
            yield return ":x: *Không thể tạo SQL. Đang thử phương pháp khác...*\n\n";
            yield return ":warning: Xin lỗi, tôi không thể tạo truy vấn cho câu hỏi này. Vui lòng thử hỏi cụ thể hơn.\n";
            yield return "[STREAM_END]";
            yield break;
        }

        // Step 3: Execute
        yield return ":floppy_disk: *Đang thực thi truy vấn...\n";
        var allowedTables = await GetAllowedTablesAsync(request.Department);
        var (execSuccess, data, execError, formattedResult) =
            await _sqlValidation.ExecuteQueryAsync(generatedSql, allowedTables, cancellationToken);

        if (!execSuccess)
        {
            yield return ":x: *Truy vấn thất bại: " + (execError ?? "Lỗi không xác định") + "*\n";
            yield return ":arrows_counterclockwise: *Đang thử phương pháp khác...*\n\n";
            yield return "[STREAM_END]";
            yield break;
        }

        yield return ":arrow_right: *Kết quả (" + (data as System.Data.DataTable)?.Rows.Count + " dòng):*\n\n";

        // Step 4: Stream natural language answer + chart config
        var nlResponse = await GenerateNaturalLanguageResponseAsync(
            request.Message, formattedResult!, request.Department, cancellationToken);

        // Parse chart config from response
        var (textResponse, chartConfig) = ExtractChartConfig(nlResponse);

        // Stream text first
        await foreach (var chunk in StreamString(textResponse))
        {
            yield return chunk;
        }

        // Stream chart placeholder so frontend can render it
        if (!string.IsNullOrEmpty(chartConfig))
        {
            yield return "\n\n[CHART_CONFIG:" + chartConfig + "]\n\n";
        }

        yield return "\n\n[STREAM_END]";
    }

    /// <summary>
    /// Tách chart config JSON ra khỏi text response.
    /// Marker: /* CHART_OUTPUT */ {json} /* CHART_END */
    /// </summary>
    private static (string Text, string? ChartConfig) ExtractChartConfig(string response)
    {
        var start = response.IndexOf("/* CHART_OUTPUT */", StringComparison.OrdinalIgnoreCase);
        if (start < 0) return (response, null);

        var end = response.IndexOf("/* CHART_END */", start, StringComparison.OrdinalIgnoreCase);
        if (end < 0) return (response, null);

        var jsonPart = response.Substring(start + "/* CHART_OUTPUT */".Length, end - start - "/* CHART_OUTPUT */".Length).Trim();
        var textPart = response.Substring(0, start).Trim();

        // Validate JSON
        try
        {
            using var doc = JsonDocument.Parse(jsonPart);
            var root = doc.RootElement;
            // Basic validation
            if (!root.TryGetProperty("chartType", out _) ||
                !root.TryGetProperty("series", out _))
            {
                return (response, null);
            }
            return (textPart, jsonPart);
        }
        catch
        {
            return (response, null);
        }
    }

    public async Task<string> GenerateSqlOnlyAsync(
        string question, string department, CancellationToken cancellationToken = default)
    {
        var schemaPrompt = await _schemaService.GetSchemaPromptForDepartmentAsync(department);
        var systemPrompt = BuildSqlGenerationPrompt(department);
        var userMessage = $@"Câu hỏi: {question}

{schemaPrompt}";

        return await CallLlmForSqlAsync(systemPrompt, userMessage, temperature: 0.2, cancellationToken);
    }

    private string BuildSqlGenerationPrompt(string department)
    {
        var deptLabel = department.ToLower() switch
        {
            "sales" => "Sales Dashboard - phân tích bán hàng",
            "finance" => "Finance Dashboard - phân tích tài chính",
            "marketing" => "Marketing Dashboard - phân tích marketing",
            "inventory" => "Inventory Dashboard - phân tích kho",
            "hr" => "Human Resources Dashboard - phân tích nhân sự",
            "cskh" => "Customer Service Dashboard - phân tích CSKH",
            "executive" => "Executive Dashboard - phân tích toàn công ty",
            _ => department
        };

        return $@"Bạn là một chuyên gia SQL. Nhiệm vụ của bạn: chuyển câu hỏi tiếng Việt/anh của người dùng thành câu truy vấn T-SQL.

Ngữ cảnh: {deptLabel}

## QUY TẮC BẮT BUỘC:
1. CHỈ sinh câu lệnh SELECT - tuyệt đối không UPDATE, DELETE, INSERT, DROP, ALTER, CREATE, TRUNCATE, EXEC, EXECUTE
2. LUÔN dùng TOP để giới hạn kết quả (tối đa TOP 100)
3. Dùng JOIN với ON rõ ràng, đặt alias cho mọi bảng
4. Tiền tệ luôn dùng decimal(18,2), format label có 'VNĐ' hoặc 'đ'
5. Ngày tháng: dùng GETDATE(), DATEADD(), DATEDIFF(), MONTH(), YEAR()
6. Nếu câu hỏi liên quan đến 'doanh số', 'revenue', 'tổng tiền' → SUM() trên TotalAmount
7. Nếu câu hỏi liên quan đến 'số lượng', 'count' → COUNT()
8. Nếu câu hỏi liên quan đến 'trung bình' → AVG()
9. Nếu câu hỏi liên quan đến 'tỷ lệ', 'tỷ suất' → AVG() hoặc SUM()/COUNT()
10. LUÔN lọc theo ngày gần đây (mặc định 12 tháng gần nhất) nếu câu hỏi liên quan đến thời gian
11. Nếu câu hỏi không rõ ràng, dùng điều kiện mặc định hợp lý và ghi chú

## ĐỊNH DẠNG TRẢ LỜI:
- Chỉ trả về đÚNG một câu SQL, không giải thích, không markdown code block
- Đặt câu SQL giữa 2 marker: /* SQL_OUTPUT */ và /* SQL_END */
- Ví dụ: /* SQL_OUTPUT */ SELECT TOP 10 ... /* SQL_END */
- KHÔNG thêm dòng trống, KHÔNG thêm dấu chấm phẩy cuối câu";
    }

    private async Task<string> CallLlmForSqlAsync(
        string systemPrompt, string userMessage, double temperature, CancellationToken cancellationToken)
    {
        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userMessage }
        };

        var payload = new
        {
            model = Model,
            messages,
            temperature,
            max_tokens = 2000,
            stream = false
        };

        var client = _httpClientFactory.CreateClient("AIChat");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiKey);

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

        // Extract SQL between markers
        return ExtractSqlFromResponse(reply);
    }

    private static string ExtractSqlFromResponse(string response)
    {
        var start = response.IndexOf("/* SQL_OUTPUT */", StringComparison.OrdinalIgnoreCase);
        var end = response.IndexOf("/* SQL_END */", StringComparison.OrdinalIgnoreCase);

        if (start >= 0 && end > start)
        {
            return response[(start + "/* SQL_OUTPUT */".Length)..end].Trim();
        }

        // Fallback: look for just SQL code block
        if (response.Contains("```sql", StringComparison.OrdinalIgnoreCase) ||
            response.Contains("```tsql", StringComparison.OrdinalIgnoreCase) ||
            response.Contains("```", StringComparison.OrdinalIgnoreCase))
        {
            var lines = response.Split('\n');
            var sqlLines = new List<string>();
            var inside = false;
            foreach (var line in lines)
            {
                if (line.Contains("```"))
                {
                    if (inside) break;
                    inside = true;
                    continue;
                }
                if (inside) sqlLines.Add(line);
            }
            if (sqlLines.Count > 0)
                return string.Join("\n", sqlLines).Trim();
        }

        // Last resort: return as-is if it looks like SQL
        var trimmed = response.Trim();
        if (trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            return trimmed;

        return string.Empty;
    }

    // Chart generation prompt section - use static readonly to avoid interpolation issues with JSON {} in string
    private static readonly string ChartGenerationPrompt = @"## CHART GENERATION (khi can ve bieu do):
Khi cau hoi yeu cau ve bieu do (keywords: 've', 'bieu do', 'chart', 'graph', 'theo thang', 'theo quy', 'so sanh', 'ti le', 'phan bo', 'xu huong', 'trend'):
1. Phan tich data de quyet dinh chart type phu hop:
   - so sanh theo category (thang, quy, phong ban, san pham) => dung 'bar' hoac 'horizontalBar'
   - xu huong theo thoi gian => dung 'line' hoac 'area'
   - ti le / phan bo (%) => dung 'pie' hoac 'donut'
2. Sau khi phan tich du lieu, bo sung JSON config vao cuoi response theo format:
   /* CHART_OUTPUT */{""chartType"":""VALUE"",""title"":""Tieu de"",""subtitle"":""Mo ta"",""xaxis"":""Nhan X"",""yaxis"":""Nhan Y"",""categories"":[""A"",""B""],""series"":[{""name"":""Series1"",""data"":[10,20]}],""colors"":[""#6965fd""],""unit"":""VND"",""height"":300}/* CHART_END */
3. Luon dat text phan tich TRUOC marker CHART_OUTPUT
4. Neu cau hoi khong can chart => KHONG output marker CHART_OUTPUT
5. series.data phai la mang so, categories phai la mang string
6. unit: dung 'VND' cho tien VND, '%' cho ti le, '$' cho USD, '' cho so luong
7. chartType chi duoc chon trong: bar, horizontalBar, line, area, pie, donut";

    private async Task<string> GenerateNaturalLanguageResponseAsync(
        string question, string tableResult, string department, CancellationToken cancellationToken)
    {
        var systemPrompt = @"Bạn là một trợ lý phân tích dữ liệu. Dựa trên kết quả truy vấn SQL, hãy trả lời câu hỏi của người dùng một cách tự nhiên bằng tiếng Việt.

QUY TẮC:
1. Trích dẫn CONCRETE số liệu từ bảng kết quả
2. Dùng định dạng bảng markdown để hiển thị dữ liệu
3. Nếu dữ liệu nhiều, chỉ hiển thị 5-10 dòng đầu tiên và ghi chú tổng số dòng
4. Thêm nhận xét ngắn gọn về xu hướng hoặc điểm nổi bật
5. Nếu không có dữ liệu, thông báo lịch sự và gợi ý câu hỏi khác
6. KHÔNG bịa đặt số liệu - chỉ dùng dữ liệu từ kết quả truy vấn
7. Trả lời NGẮN GỌN, đi thẳng vào vấn đề - tối đa 3-4 đoạn văn

" + ChartGenerationPrompt;

        var userMessage = $@"Câu hỏi: {question}

Kết quả truy vấn SQL:
{tableResult}

Hãy trả lời câu hỏi dựa trên kết quả trên.";

        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userMessage }
        };

        var payload = new
        {
            model = Model,
            messages,
            temperature = 0.4,
            max_tokens = 3000,
            stream = false
        };

        var client = _httpClientFactory.CreateClient("AIChat");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiKey);

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = await client.PostAsync(LlmEndpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement.GetProperty("choices")[0]
            .GetProperty("message").GetProperty("content").GetString() ?? tableResult;
    }

    private async Task<List<string>> GetAllowedTablesAsync(string department)
    {
        var schema = await _schemaService.GetSchemaForDepartmentAsync(department);
        return schema.Select(t => t.TableName).ToList();
    }

    private static async IAsyncEnumerable<string> StreamString(
        string text, [EnumeratorCancellation] CancellationToken _ = default)
    {
        var words = text.Split(' ');
        foreach (var word in words)
        {
            yield return word + " ";
            await Task.Delay(8);
        }
    }
}
