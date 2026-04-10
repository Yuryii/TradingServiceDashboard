namespace Dashboard.Services.Interfaces;

public class SqlValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SanitizedSql { get; set; }
    public bool WasModified { get; set; }
}

public interface ISqlValidationService
{
    SqlValidationResult Validate(string sql, List<string> allowedTables);
    string WrapWithRowLimit(string sql, int maxRows = 1000);
    bool IsReadOnlyQuery(string sql);
    Task<(bool Success, object? Data, string? Error, string? FormattedResult)> ExecuteQueryAsync(
        string sql,
        List<string> allowedTables,
        CancellationToken cancellationToken = default);
}
