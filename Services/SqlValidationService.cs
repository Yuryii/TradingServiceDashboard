using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using Dashboard.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Dashboard.Services;

public partial class SqlValidationService : ISqlValidationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SqlValidationService> _logger;

    private static readonly HashSet<string> DangerousKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "DROP", "DELETE", "UPDATE", "INSERT", "ALTER", "CREATE", "TRUNCATE",
        "EXEC", "EXECUTE", "xp_", "sp_", "sys.", "master.", "msdb.", "tempdb.",
        "restore", "backup", "shutdown", "grant", "revoke", "deny"
    };

    private static readonly char[] StatementTerminators = { ';' };

    [GeneratedRegex(@"\bGO\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex GoRegex();

    public SqlValidationService(IConfiguration configuration, ILogger<SqlValidationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public SqlValidationResult Validate(string sql, List<string> allowedTables)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return new SqlValidationResult
            {
                IsValid = false,
                ErrorMessage = "SQL query cannot be empty."
            };
        }

        var normalizedSql = sql.Trim();
        var upperSql = normalizedSql.ToUpperInvariant();

        // 1. Check for dangerous keywords
        foreach (var keyword in DangerousKeywords)
        {
            if (upperSql.Contains(keyword))
            {
                _logger.LogWarning("Dangerous keyword detected: {Keyword} in SQL: {Sql}", keyword, normalizedSql);
                return new SqlValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Query blocked: contains forbidden operation or keyword '{keyword}'."
                };
            }
        }

        // 2. Check for comment patterns
        if (normalizedSql.Contains("--") || normalizedSql.Contains("/*"))
        {
            return new SqlValidationResult
            {
                IsValid = false,
                ErrorMessage = "Query blocked: SQL comments are not allowed."
            };
        }

        // 3. Ensure it starts with SELECT (or WITH for CTE)
        if (!upperSql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) &&
            !upperSql.TrimStart().StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
        {
            return new SqlValidationResult
            {
                IsValid = false,
                ErrorMessage = "Query blocked: only SELECT queries are allowed."
            };
        }

        // 4. Check that all referenced tables are in the allowed list
        if (allowedTables.Count > 0)
        {
            var referencedTables = ExtractTableNames(normalizedSql);
            foreach (var table in referencedTables)
            {
                if (!allowedTables.Any(allowed =>
                    table.Equals(allowed, StringComparison.OrdinalIgnoreCase) ||
                    table.Equals("[" + allowed + "]", StringComparison.OrdinalIgnoreCase) ||
                    table.Equals("\"" + allowed + "\"", StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning("Table {Table} not in allowed list for this department", table);
                    return new SqlValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Query blocked: table '{table}' is not accessible in your department context."
                    };
                }
            }
        }

        // 5. Block semicolon-terminated multiple statements
        var semicolonCount = normalizedSql.Count(c => c == ';');
        if (semicolonCount > 0)
        {
            var parts = normalizedSql.Split(';', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                return new SqlValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Query blocked: multiple statements are not allowed."
                };
            }
        }

        // 6. Block GO batch separator
        if (GoRegex().IsMatch(normalizedSql))
        {
            return new SqlValidationResult
            {
                IsValid = false,
                ErrorMessage = "Query blocked: batch separator 'GO' is not allowed."
            };
        }

        // 7. Remove SQL_OUTPUT/SQL_END markers for execution (clean up)
        var sanitized = RemoveSqlMarkers(normalizedSql);

        // 8. Ensure TOP is applied (inject if missing to prevent huge result sets)
        if (!upperSql.Contains("TOP ", StringComparison.OrdinalIgnoreCase) &&
            !upperSql.Contains("FETCH ", StringComparison.OrdinalIgnoreCase))
        {
            sanitized = InjectTopClause(sanitized, 1000);
        }

        return new SqlValidationResult
        {
            IsValid = true,
            SanitizedSql = sanitized,
            WasModified = sanitized != normalizedSql
        };
    }

    public string WrapWithRowLimit(string sql, int maxRows = 1000)
    {
        var upperSql = sql.ToUpperInvariant();

        if (upperSql.Contains("TOP ", StringComparison.OrdinalIgnoreCase) ||
            upperSql.Contains("FETCH ", StringComparison.OrdinalIgnoreCase))
        {
            return sql;
        }

        return InjectTopClause(sql, maxRows);
    }

    public bool IsReadOnlyQuery(string sql)
    {
        var upper = sql.ToUpperInvariant().Trim();

        if (upper.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
            upper.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    public async Task<(bool Success, object? Data, string? Error, string? FormattedResult)> ExecuteQueryAsync(
        string sql,
        List<string> allowedTables,
        CancellationToken cancellationToken = default)
    {
        // Validate first
        var validation = Validate(sql, allowedTables);
        if (!validation.IsValid)
        {
            return (false, null, validation.ErrorMessage, null);
        }

        var finalSql = validation.SanitizedSql!;

        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            await using var conn = new SqlConnection(connectionString);

            await conn.OpenAsync(cancellationToken);

            await using var cmd = new SqlCommand(finalSql, conn);
            cmd.CommandTimeout = 30;
            cmd.CommandText = finalSql;

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            var dataTable = new DataTable();
            dataTable.Load(reader);

            if (dataTable.Rows.Count == 0)
            {
                return (true, null, null, "Không có dữ liệu phù hợp với yêu cầu của bạn.");
            }

            var formatted = FormatAsMarkdown(dataTable);
            return (true, dataTable, null, formatted);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL execution error: {Sql}", finalSql);
            return (false, null, $"Lỗi SQL: {ex.Message}", null);
        }
        catch (OperationCanceledException)
        {
            return (false, null, "Query timed out after 30 seconds. Please try a simpler query.", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error executing SQL: {Sql}", finalSql);
            return (false, null, $"Lỗi hệ thống: {ex.Message}", null);
        }
    }

    private static HashSet<string> ExtractTableNames(string sql)
    {
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var tablePattern = TableNameRegex();
        var matches = tablePattern.Matches(sql);
        foreach (Match match in matches)
        {
            var table = match.Groups[1].Value.Trim('[', '"', ']');
            if (!string.IsNullOrWhiteSpace(table) && !IsSqlKeyword(table))
            {
                tables.Add(table);
            }
        }

        return tables;
    }

    [GeneratedRegex(@"\b(?:FROM|JOIN|INTO|UPDATE)\s+(?:(\[[^\]]+\]|\""[^""]+\""|[A-Za-z_][A-Za-z0-9_]*))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex TableNameRegex();

    private static bool IsSqlKeyword(string word)
    {
        var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SELECT", "FROM", "WHERE", "AND", "OR", "NOT", "IN", "EXISTS",
            "BETWEEN", "LIKE", "IS", "NULL", "AS", "ON", "JOIN", "LEFT", "RIGHT",
            "INNER", "OUTER", "CROSS", "FULL", "UNION", "ALL", "DISTINCT",
            "ORDER", "BY", "GROUP", "HAVING", "TOP", "OFFSET", "FETCH", "NEXT",
            "ROWS", "ONLY", "CASE", "WHEN", "THEN", "ELSE", "END", "WITH",
            "INSERT", "UPDATE", "DELETE", "DECLARE", "SET", "EXEC", "PROCEDURE",
            "FUNCTION", "VIEW", "TRIGGER", "TABLE", "INDEX", "DATABASE", "SCHEMA",
            "IF", "BEGIN", "RETURN", "GO", "PRINT", "raiserror", "waitfor"
        };
        return keywords.Contains(word);
    }

    private static string RemoveSqlMarkers(string sql)
    {
        return sql
            .Replace("/* SQL_OUTPUT */", "")
            .Replace("/* SQL_END */", "")
            .Replace("/*SQL_OUTPUT*/", "")
            .Replace("/*SQL_END*/", "")
            .Trim();
    }

    private static string InjectTopClause(string sql, int maxRows)
    {
        var trimmed = sql.Trim();

        if (trimmed.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
        {
            var unionIdx = trimmed.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase);
            if (unionIdx > 0)
            {
                var cte = trimmed[..unionIdx].Trim();
                var rest = trimmed[unionIdx..].Trim();

                if (!rest.StartsWith("SELECT TOP ", StringComparison.OrdinalIgnoreCase))
                {
                    rest = "SELECT TOP " + maxRows + " " + rest.TrimStart();
                }

                return cte + " " + rest;
            }
        }

        if (trimmed.StartsWith("SELECT TOP ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("SELECT\tTOP ", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        var firstSelectIdx = trimmed.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase);
        if (firstSelectIdx >= 0)
        {
            var prefix = trimmed[..firstSelectIdx];
            var rest = trimmed[firstSelectIdx..].TrimStart();
            return prefix + "SELECT TOP " + maxRows + " " + rest;
        }

        return trimmed;
    }

    private static string FormatAsMarkdown(DataTable table)
    {
        if (table.Rows.Count == 0)
            return "No data found.";

        var sb = new StringBuilder();

        var headers = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName);
        sb.AppendLine("| " + string.Join(" | ", headers) + " |");
        sb.AppendLine("| " + string.Join(" | ", headers.Select(_ => "---")) + " |");

        var maxRows = Math.Min(table.Rows.Count, 100);
        for (int i = 0; i < maxRows; i++)
        {
            var row = table.Rows[i];
            var values = table.Columns.Cast<DataColumn>().Select(col =>
            {
                var val = row[col];
                if (val == null || val == DBNull.Value) return "";
                var str = val.ToString() ?? "";
                if (str.Length > 100) str = str[..97] + "...";
                return str.Replace("|", "\\|").Replace("\n", " ").Replace("\r", "");
            });
            sb.AppendLine("| " + string.Join(" | ", values) + " |");
        }

        if (table.Rows.Count > 100)
        {
            sb.AppendLine();
            sb.AppendLine($"*Hiển thị 100 / {table.Rows.Count} dòng. Vui lòng thêm điều kiện lọc để thu hẹp kết quả.*");
        }

        return sb.ToString();
    }
}
