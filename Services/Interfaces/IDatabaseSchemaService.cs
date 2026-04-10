namespace Dashboard.Services.Interfaces;

public class TableColumnInfo
{
    public string TableName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsForeignKey { get; set; }
    public string? ReferencedTable { get; set; }
    public string? ReferencedColumn { get; set; }
    public string? FriendlyDescription { get; set; }
}

public class TableSchemaInfo
{
    public string TableName { get; set; } = string.Empty;
    public string FriendlyName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<TableColumnInfo> Columns { get; set; } = new();
}

public interface IDatabaseSchemaService
{
    Task<string> GetSchemaPromptForDepartmentAsync(string department);
    Task<string> GetFullSchemaPromptAsync();
    Task<List<TableSchemaInfo>> GetSchemaForDepartmentAsync(string department);
    Task<Dictionary<string, List<string>>> GetTableRelationshipsAsync(string department);
}
