using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Globalization;
using System.Reflection;

namespace Dashboard.Services;

public class ExcelCrudService
{
    private readonly Data.ApplicationDbContext _context;
    // Maps prop.Name (e.g. RegionID) -> code column name (e.g. RegionCode)
    private static readonly Dictionary<string, string> _codeColumnMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RegionID"] = "RegionCode",
        ["BranchID"] = "BranchCode",
        ["DepartmentID"] = "DepartmentCode",
        ["PositionID"] = "PositionCode",
        ["CustomerID"] = "CustomerCode",
        ["CustomerGroupID"] = "GroupCode",
        ["SalesChannelID"] = "ChannelCode",
        ["StageID"] = "StageCode",
        ["ProductID"] = "ProductCode",
        ["CategoryID"] = "CategoryCode",
        ["WarehouseID"] = "WarehouseCode",
        ["SupplierID"] = "SupplierCode",
        ["CampaignID"] = "CampaignCode",
        ["EmployeeID"] = "EmployeeCode",
    };

    // Reverse: code column -> prop name
    private static readonly Dictionary<string, string> _propNameByCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RegionCode"] = "RegionID",
        ["BranchCode"] = "BranchID",
        ["DepartmentCode"] = "DepartmentID",
        ["PositionCode"] = "PositionID",
        ["CustomerCode"] = "CustomerID",
        ["GroupCode"] = "CustomerGroupID",
        ["ChannelCode"] = "SalesChannelID",
        ["StageCode"] = "StageID",
        ["ProductCode"] = "ProductID",
        ["CategoryCode"] = "CategoryID",
        ["WarehouseCode"] = "WarehouseID",
        ["SupplierCode"] = "SupplierID",
        ["CampaignCode"] = "CampaignID",
        ["EmployeeCode"] = "EmployeeID",
    };

    private static readonly HashSet<string> _excludedProps = new(StringComparer.OrdinalIgnoreCase)
    {
        // PK / FK ID fields (auto-generated, not importable)
        "CustomerID","SalesOrderID","InvoiceID","PurchaseOrderID","QuoteID",
        "OpportunityID","StageID","ProductID","EmployeeID","SupplierID",
        "WarehouseID","CampaignID","LeadID","SpendID","ReturnID","PaymentID",
        "ReceiptID","BranchID","DepartmentID","PositionID","RegionID",
        "CategoryID","AttendanceID","LeaveRequestID","PayrollID",
        "ReviewID","JobOpeningID","ApplicantID","TicketID","TargetID",
        "InventoryID","SnapshotID","TransactionID","KpiTargetID","LeaveRequestID",
        // Audit fields
        "CreatedAt","UpdatedAt","CreatedBy","ModifiedBy",
        // Navigation / collection properties (not scalar values)
        "Customer","Branch","Department","Position","Employee",
        "Supplier","Product","Warehouse","Region",
        "CustomerGroup","SalesChannel","OpportunityStage","Campaign",
        "Opportunity","Quote","SalesInvoice","SalesReturn","CustomerPayment",
        "PurchaseInvoice","PurchaseReceipt","SupplierPayment","SalesOrder",
        "PurchaseOrder","ExpenseCategory","Expense",
        "MarketingLead","MarketingCampaign","MarketingSpendDaily",
        "Inventory","InventorySnapshot","StockTransaction",
        "LeaveRequest","Payroll","PerformanceReview","Applicant","JobOpening",
        "SupportTicket","KpiTarget","Attendance","LeaveRequestsApproved",
        "ExpenseCategory","DimDate","Notification","NotificationConfig",
        "User","Subordinates","OwnedOpportunities","AssignedLeads","SalesOrders",
        "Quotes","PurchaseOrdersRequested","PurchaseOrdersApproved","Expenses",
        "ExpensesApproved","KpiTargets","Attendances","LeaveRequests","Applicants",
        "AssignedTickets","StockTransactions","StageHistoryChanges","Snapshots",
        "StageHistory","InvoiceDetails","OrderDetails","ReturnDetails",
        "ReceiptDetails","Opportunities","Customers","Suppliers","Employees",
        "Warehouses","Branches","Departments","Positions","JobOpenings",
        "Applicants","Notifications","AssignedToEmployee","AssignedEmployee",
        "CreatedByEmployee","RequestedByEmployee","ApprovedByEmployee","ReviewedByEmployee",
        "ReferredByEmployee","ConvertedCustomer","SalesEmployee","ProcessedByEmployee",
        "ReceivedByEmployee","Manager","Category","Supplier","Lead","Campaign",
        "OrderDetails","InvoiceDetails","ReturnDetails","ReceiptDetails",
        "PurchaseOrderDetail","SalesInvoiceDetail","SalesOrderDetail","SalesReturnDetail",
        "PurchasesInvoices","Purchasesinvoices","Purchaseinvoices",
        "Navigation","Navigations"
    };

    private static readonly Dictionary<string, LookupConfig> _lookupConfigs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RegionCode"]      = new("Regions",       "RegionCode",   "RegionID"),
        ["BranchCode"]      = new("Branches",       "BranchCode",   "BranchID"),
        ["DepartmentCode"]  = new("Departments",    "DepartmentCode","DepartmentID"),
        ["PositionCode"]    = new("Positions",      "PositionCode", "PositionID"),
        ["CustomerCode"]    = new("Customers",       "CustomerCode", "CustomerID"),
        ["GroupCode"]       = new("CustomerGroups", "GroupCode",    "CustomerGroupID"),
        ["ChannelCode"]     = new("SalesChannels",   "ChannelCode",  "SalesChannelID"),
        ["StageCode"]       = new("OpportunityStages","StageCode",   "StageID"),
        ["ProductCode"]     = new("Products",         "ProductCode",  "ProductID"),
        ["CategoryCode"]    = new("ProductCategories","CategoryCode","CategoryID"),
        ["WarehouseCode"]  = new("Warehouses",       "WarehouseCode","WarehouseID"),
        ["SupplierCode"]   = new("Suppliers",         "SupplierCode", "SupplierID"),
        ["CampaignCode"]   = new("MarketingCampaigns","CampaignCode","CampaignID"),
        ["EmployeeCode"]   = new("Employees",         "EmployeeCode","EmployeeID"),
        ["OrderNumber"]    = null!, // handled specially
        ["QuoteNumber"]    = null!,
        ["InvoiceNumber"]  = null!,
        ["ReceiptNumber"]  = null!,
        ["ReturnNumber"]   = null!,
        ["PaymentNumber"]  = null!,
        ["TicketNumber"]   = null!,
        ["OpportunityCode"]= null!,
    };

    private record LookupConfig(string TableName, string CodeColumn, string IdColumn);

    public ExcelCrudService(Data.ApplicationDbContext context)
    {
        _context = context;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<ExcelReadResult> ReadExcelAsync(Stream stream)
    {
        try
        {
            using var package = new ExcelPackage(stream);
            var sheet = package.Workbook.Worksheets.FirstOrDefault();
            if (sheet == null)
                return new ExcelReadResult { Error = "No sheet found in the file." };

            if (sheet.Dimension == null || sheet.Dimension.Rows < 2)
                return new ExcelReadResult { Error = "File must have at least a header row and one data row." };

            var headers = new List<string>();
            for (int col = 1; col <= sheet.Dimension.Columns; col++)
            {
                headers.Add(sheet.Cells[1, col].Text.Trim());
            }

            var rows = new List<Dictionary<string, string?>>();
            for (int row = 2; row <= sheet.Dimension.Rows; row++)
            {
                var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                bool hasData = false;
                for (int col = 1; col <= headers.Count; col++)
                {
                    var val = sheet.Cells[row, col].Text?.Trim();
                    dict[headers[col - 1]] = val;
                    if (!string.IsNullOrEmpty(val)) hasData = true;
                }
                if (hasData) rows.Add(dict);
            }

            return new ExcelReadResult
            {
                Success = true,
                Headers = headers,
                Rows = rows,
                TotalRows = rows.Count
            };
        }
        catch (Exception ex)
        {
            return new ExcelReadResult { Error = $"Error reading file: {ex.Message}" };
        }
    }

    public async Task<ExcelValidationResult> ValidateAsync(
        List<Dictionary<string, string?>> rows,
        string entityType)
    {
        entityType = NormalizeEntityType(entityType);
        var entityProps = GetEntityProperties(entityType);
        if (entityProps == null)
            return new ExcelValidationResult { Error = $"Unknown entity type: {entityType}" };

        var errors = new List<string>();
        var validRows = new List<Dictionary<string, string?>>();

        // Pre-load lookup caches
        var lookupCaches = BuildLookupCaches(entityType);

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowErrors = ValidateRow(row, entityProps, lookupCaches, entityType, i + 2);
            if (rowErrors.Count > 0)
                errors.AddRange(rowErrors);
            else
                validRows.Add(row);
        }

        return new ExcelValidationResult
        {
            Success = errors.Count == 0,
            ValidRows = validRows,
            InvalidRows = rows.Count - validRows.Count,
            TotalRows = rows.Count,
            Errors = errors
        };
    }

    public async Task<ExcelImportResult> ImportAsync(
        List<Dictionary<string, string?>> rows,
        string entityType)
    {
        entityType = NormalizeEntityType(entityType);
        var entityProps = GetEntityProperties(entityType);
        if (entityProps == null)
            return new ExcelImportResult { Error = $"Unknown entity type: {entityType}" };

        var lookupCaches = BuildLookupCaches(entityType);
        var successCount = 0;
        var failCount = 0;
        var errors = new List<string>();
        var addedEntities = new List<object>();

        foreach (var row in rows)
        {
            try
            {
                var entity = CreateEntityFromRow(row, entityProps, lookupCaches);
                _context.Add(entity);
                addedEntities.Add(entity);
                successCount++;
            }
            catch (Exception ex)
            {
                failCount++;
                errors.Add($"Row error: {ex.Message}");
            }
        }

        if (successCount > 0)
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Rollback: remove added entities
                foreach (var e in addedEntities)
                {
                    _context.Entry(e).State = EntityState.Detached;
                }
                return new ExcelImportResult
                {
                    Error = $"Database error: {ex.Message}",
                    SuccessCount = 0,
                    FailCount = rows.Count
                };
            }
        }

        return new ExcelImportResult
        {
            Success = failCount == 0,
            SuccessCount = successCount,
            FailCount = failCount,
            Errors = errors
        };
    }

    public async Task<byte[]> GenerateTemplateAsync(string entityType)
    {
        entityType = NormalizeEntityType(entityType);
        var entityProps = GetEntityProperties(entityType);
        if (entityProps == null)
            throw new ArgumentException($"Unknown entity type: {entityType}");

        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add(entityType);

        // Column headers (only importable fields)
        var headers = new List<string>();
        var propIndex = new Dictionary<string, PropertyInfo>();

        for (int i = 0; i < entityProps.Count; i++)
        {
            var prop = entityProps[i];
            var name = prop.Name;

            // For FK, show the Code column instead of ID
            if (_codeColumnMap.TryGetValue(name, out var codeCol))
            {
                name = codeCol;
            }

            headers.Add(name);
            propIndex[name] = prop;
        }

        for (int col = 0; col < headers.Count; col++)
        {
            sheet.Cells[1, col + 1].Value = headers[col];
            sheet.Cells[1, col + 1].Style.Font.Bold = true;
            sheet.Cells[1, col + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            sheet.Cells[1, col + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
        }

        // Add a sample row with example values
        AddSampleRow(sheet, entityProps, 2);

        // Add instruction row
        sheet.Row(3).Hidden = true; // hide sample row but keep it for reference

        // Auto-fit columns
        sheet.Cells[sheet.Dimension.Address].AutoFitColumns();

        return package.GetAsByteArray();
    }

    public async Task<byte[]> ExportAsync(string entityType)
    {
        entityType = NormalizeEntityType(entityType);
        var entityProps = GetEntityProperties(entityType);
        if (entityProps == null)
            throw new ArgumentException($"Unknown entity type: {entityType}");

        var queryable = GetEntityQueryable(entityType);
        if (queryable == null)
            throw new ArgumentException($"Cannot query entity type: {entityType}");

        var data = await GetEntityData(entityType);

        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add(entityType);

        // Headers
        for (int col = 0; col < entityProps.Count; col++)
        {
            sheet.Cells[1, col + 1].Value = entityProps[col].Name;
            sheet.Cells[1, col + 1].Style.Font.Bold = true;
            sheet.Cells[1, col + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            sheet.Cells[1, col + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
        }

        // Data rows
        for (int row = 0; row < data.Count; row++)
        {
            var dict = (Dictionary<string, object?>)data[row]!;
            for (int col = 0; col < entityProps.Count; col++)
            {
                var val = dict.TryGetValue(entityProps[col].Name, out var v) ? v : null;
                sheet.Cells[row + 2, col + 1].Value = val?.ToString() ?? "";
            }
        }

        sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        return package.GetAsByteArray();
    }

    // --- Private helper methods ---

    private static readonly Dictionary<string, string> _pluralToSingular = new(StringComparer.OrdinalIgnoreCase)
    {
        ["regions"]="Region",["region"]="Region",
        ["branches"]="Branch",["branch"]="Branch",
        ["departments"]="Department",["department"]="Department",
        ["positions"]="Position",["position"]="Position",
        ["employees"]="Employee",["employee"]="Employee",
        ["customers"]="Customer",["customer"]="Customer",
        ["customergroups"]="CustomerGroup",["customergroup"]="CustomerGroup",
        ["saleschannels"]="SalesChannel",["saleschannel"]="SalesChannel",
        ["opportunitystages"]="OpportunityStage",["opportunitystage"]="OpportunityStage",
        ["opportunities"]="Opportunity",["opportunity"]="Opportunity",
        ["quotes"]="Quote",["quote"]="Quote",
        ["salesorders"]="SalesOrder",["salesorder"]="SalesOrder",
        ["salesinvoices"]="SalesInvoice",["salesinvoice"]="SalesInvoice",
        ["salesreturns"]="SalesReturn",["salesreturn"]="SalesReturn",
        ["customerpayments"]="CustomerPayment",["customerpayment"]="CustomerPayment",
        ["products"]="Product",["product"]="Product",
        ["productcategories"]="ProductCategory",["productcategory"]="ProductCategory",
        ["warehouses"]="Warehouse",["warehouse"]="Warehouse",
        ["suppliers"]="Supplier",["supplier"]="Supplier",
        ["purchaseorders"]="PurchaseOrder",["purchaseorder"]="PurchaseOrder",
        ["purchasereceipts"]="PurchaseReceipt",["purchasereceipt"]="PurchaseReceipt",
        ["purchaseinvoices"]="PurchaseInvoice",["purchaseinvoice"]="PurchaseInvoice",
        ["supplierpayments"]="SupplierPayment",["supplierpayment"]="SupplierPayment",
        ["expensecategories"]="ExpenseCategory",["expensecategory"]="ExpenseCategory",
        ["expenses"]="Expense",["expense"]="Expense",
        ["attendances"]="Attendance",["attendance"]="Attendance",
        ["leaverequests"]="LeaveRequest",["leaverequest"]="LeaveRequest",
        ["payrolls"]="Payroll",["payroll"]="Payroll",
        ["performancereviews"]="PerformanceReview",["performancereview"]="PerformanceReview",
        ["jobopenings"]="JobOpening",["jobopening"]="JobOpening",
        ["applicants"]="Applicant",["applicant"]="Applicant",
        ["supporttickets"]="SupportTicket",["supportticket"]="SupportTicket",
        ["marketingcampaigns"]="MarketingCampaign",["marketingcampaign"]="MarketingCampaign",
        ["marketingleads"]="MarketingLead",["marketinglead"]="MarketingLead",
        ["marketingspenddailies"]="MarketingSpendDaily",["marketingspenddaily"]="MarketingSpendDaily",
        ["inventories"]="Inventory",["inventory"]="Inventory",
        ["inventorysnapshots"]="InventorySnapshot",["inventorysnapshot"]="InventorySnapshot",
        ["stocktransactions"]="StockTransaction",["stocktransaction"]="StockTransaction",
        ["kpitargets"]="KpiTarget",["kpitarget"]="KpiTarget",
        ["expenses"]="Expense",
    };

    private static string NormalizeEntityType(string entityType)
    {
        if (string.IsNullOrWhiteSpace(entityType)) return entityType;
        if (_pluralToSingular.TryGetValue(entityType.Trim(), out var singular))
            return singular;
        return char.ToUpper(entityType.Trim()[0]) + entityType.Trim()[1..];
    }

    private List<PropertyInfo>? GetEntityProperties(string entityType)
    {
        var type = Type.GetType($"Dashboard.Models.{entityType}");
        if (type == null) return null;
        return type.GetProperties()
            .Where(p => !_excludedProps.Contains(p.Name))
            .ToList();
    }

    private Dictionary<string, Dictionary<string, int>> BuildLookupCaches(string entityType)
    {
        var caches = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
        var props = GetEntityProperties(entityType) ?? new();

        foreach (var prop in props)
        {
            if (_lookupConfigs.TryGetValue(prop.Name, out var cfg) && cfg != null)
            {
                var cache = GetLookupCache(cfg.TableName, cfg.CodeColumn, cfg.IdColumn);
                if (cache != null)
                    caches[prop.Name] = cache;
            }
        }
        return caches;
    }

    private Dictionary<string, int>? GetLookupCache(string tableName, string codeCol, string idCol)
    {
        try
        {
            return tableName.ToLower() switch
            {
                "regions"        => _context.Regions.AsNoTracking()
                    .ToDictionary(r => r.RegionCode ?? "", r => r.RegionID, StringComparer.OrdinalIgnoreCase),
                "branches"       => _context.Branches.AsNoTracking()
                    .ToDictionary(b => b.BranchCode ?? "", b => b.BranchID, StringComparer.OrdinalIgnoreCase),
                "departments"    => _context.Departments.AsNoTracking()
                    .ToDictionary(d => d.DepartmentCode ?? "", d => d.DepartmentID, StringComparer.OrdinalIgnoreCase),
                "positions"      => _context.Positions.AsNoTracking()
                    .ToDictionary(p => p.PositionCode ?? "", p => p.PositionID, StringComparer.OrdinalIgnoreCase),
                "customers"      => _context.Customers.AsNoTracking()
                    .ToDictionary(c => c.CustomerCode ?? "", c => c.CustomerID, StringComparer.OrdinalIgnoreCase),
                "customergroups" => _context.CustomerGroups.AsNoTracking()
                    .ToDictionary(g => g.GroupCode ?? "", g => g.CustomerGroupID, StringComparer.OrdinalIgnoreCase),
                "saleschannels"  => _context.SalesChannels.AsNoTracking()
                    .ToDictionary(c => c.ChannelCode ?? "", c => c.SalesChannelID, StringComparer.OrdinalIgnoreCase),
                "opportunitystages" => _context.OpportunityStages.AsNoTracking()
                    .ToDictionary(s => s.StageCode ?? "", s => s.StageID, StringComparer.OrdinalIgnoreCase),
                "products"       => _context.Products.AsNoTracking()
                    .ToDictionary(p => p.ProductCode ?? "", p => p.ProductID, StringComparer.OrdinalIgnoreCase),
                "productcategories" => _context.ProductCategories.AsNoTracking()
                    .ToDictionary(c => c.CategoryCode ?? "", c => c.CategoryID, StringComparer.OrdinalIgnoreCase),
                "warehouses"     => _context.Warehouses.AsNoTracking()
                    .ToDictionary(w => w.WarehouseCode ?? "", w => w.WarehouseID, StringComparer.OrdinalIgnoreCase),
                "suppliers"      => _context.Suppliers.AsNoTracking()
                    .ToDictionary(s => s.SupplierCode ?? "", s => s.SupplierID, StringComparer.OrdinalIgnoreCase),
                "marketingcampaigns" => _context.MarketingCampaigns.AsNoTracking()
                    .ToDictionary(c => c.CampaignCode ?? "", c => c.CampaignID, StringComparer.OrdinalIgnoreCase),
                "employees"      => _context.Employees.AsNoTracking()
                    .ToDictionary(e => e.EmployeeCode ?? "", e => e.EmployeeID, StringComparer.OrdinalIgnoreCase),
                "opportunities" => null,
                "quotes"         => null,
                "salesorders"    => null,
                "salesinvoices"  => null,
                "purchasereceipts" => null,
                "purchasesinvoices" => null,
                "salesreturns"   => null,
                "customerpayments" => null,
                "supplierpayments" => null,
                "attendances"    => null,
                "leaverequests"  => null,
                "payrolls"       => null,
                "performancereviews" => null,
                "jobopenings"    => null,
                "applicants"     => null,
                "supporttickets" => null,
                "marketingleads" => null,
                "marketingspenddailies" => null,
                "expenses"       => null,
                "expensecategories" => null,
                "inventory"      => null,
                "inventorysnapshots" => null,
                "stocktransactions" => null,
                "kpitargets"     => null,
                _ => null
            };
        }
        catch { return null; }
    }

    private List<string> ValidateRow(
        Dictionary<string, string?> row,
        List<PropertyInfo> props,
        Dictionary<string, Dictionary<string, int>> lookupCaches,
        string entityType,
        int rowNumber)
    {
        var errors = new List<string>();

        foreach (var prop in props)
        {
            // Find the actual code column name used in the Excel file
            var codeColName = _codeColumnMap.TryGetValue(prop.Name, out var ccn) ? ccn : prop.Name;
            var cellVal = row.TryGetValue(codeColName, out var v) ? v : null;

            // FK with code lookup: validate the code exists
            if (_propNameByCode.TryGetValue(codeColName, out var propName) && _lookupConfigs.TryGetValue(propName, out var cfg) && cfg != null)
            {
                if (!string.IsNullOrWhiteSpace(cellVal))
                {
                    if (!lookupCaches.TryGetValue(propName, out var cache) || !cache.TryGetValue(cellVal!, out _))
                    {
                        errors.Add($"Row {rowNumber}: '{codeColName}' = '{cellVal}' not found in {cfg.TableName}");
                    }
                }
                continue;
            }

            // If no code lookup for this prop and prop is an int ID (like SalesOrderID),
            // skip it — user must fill in the ID value directly, or leave blank
            var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            if (underlyingType == typeof(int) && _excludedProps.Contains(prop.Name))
                continue;

            // Skip nullable / blank
            if (string.IsNullOrWhiteSpace(cellVal))
                continue;

            // Type validation
            try
            {
                if (underlyingType == typeof(bool))
                {
                    var lower = cellVal.ToLower();
                    if (lower != "true" && lower != "false" && lower != "1" && lower != "0" && lower != "yes" && lower != "no")
                        errors.Add($"Row {rowNumber}: '{codeColName}' must be True/False (got: {cellVal})");
                }
                else if (underlyingType == typeof(decimal))
                {
                    if (!decimal.TryParse(cellVal, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                        errors.Add($"Row {rowNumber}: '{codeColName}' must be a decimal number (got: {cellVal})");
                }
                else if (underlyingType == typeof(int))
                {
                    if (!int.TryParse(cellVal, out _))
                        errors.Add($"Row {rowNumber}: '{codeColName}' must be an integer (got: {cellVal})");
                }
                else if (underlyingType == typeof(long))
                {
                    if (!long.TryParse(cellVal, out _))
                        errors.Add($"Row {rowNumber}: '{codeColName}' must be a long integer (got: {cellVal})");
                }
                else if (underlyingType == typeof(DateTime))
                {
                    if (!DateTime.TryParse(cellVal, CultureInfo.InvariantCulture, DateTimeStyles.None, out _) &&
                        !DateTime.TryParseExact(cellVal, new[] { "yyyy-MM-dd","dd/MM/yyyy","MM/dd/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                        errors.Add($"Row {rowNumber}: '{codeColName}' must be a date (got: {cellVal})");
                }
            }
            catch
            {
                errors.Add($"Row {rowNumber}: Error validating '{codeColName}'");
            }
        }

        return errors;
    }

    private object CreateEntityFromRow(
        Dictionary<string, string?> row,
        List<PropertyInfo> props,
        Dictionary<string, Dictionary<string, int>> lookupCaches)
    {
        var typeName = props[0].DeclaringType!.FullName!;
        var entity = Activator.CreateInstance(Type.GetType(typeName)!);
        var entityType = Type.GetType(typeName)!;

        foreach (var prop in props)
        {
            // Find the actual code column name used in the Excel file
            var codeColName = _codeColumnMap.TryGetValue(prop.Name, out var ccn) ? ccn : prop.Name;
            var cellVal = row.TryGetValue(codeColName, out var v) ? v : null;

            // If prop is an excluded FK ID (no code lookup) and no value, skip
            var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            if (underlyingType == typeof(int) && _excludedProps.Contains(prop.Name) && string.IsNullOrWhiteSpace(cellVal))
                continue;

            // FK: code -> ID
            if (_propNameByCode.TryGetValue(codeColName, out var propName) && _lookupConfigs.TryGetValue(propName, out var cfg) && cfg != null)
            {
                if (!string.IsNullOrWhiteSpace(cellVal) && lookupCaches.TryGetValue(propName, out var cache) && cache.TryGetValue(cellVal!, out var id))
                    prop.SetValue(entity, id);
                continue;
            }

            // Plain value conversion
            if (string.IsNullOrWhiteSpace(cellVal)) continue;
            object? value = null;

            if (underlyingType == typeof(string))
                value = cellVal;
            else if (underlyingType == typeof(bool))
                value = bool.TryParse(cellVal, out var b) ? b : (cellVal == "1" || cellVal.Equals("yes", StringComparison.OrdinalIgnoreCase));
            else if (underlyingType == typeof(decimal))
                value = decimal.TryParse(cellVal, NumberStyles.Any, CultureInfo.InvariantCulture, out var dec) ? dec : 0;
            else if (underlyingType == typeof(int))
                value = int.TryParse(cellVal, out var ii) ? ii : 0;
            else if (underlyingType == typeof(long))
                value = long.TryParse(cellVal, out var ll) ? ll : 0L;
            else if (underlyingType == typeof(DateTime))
            {
                if (DateTime.TryParse(cellVal, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    value = dt;
                else if (DateTime.TryParseExact(cellVal, new[] { "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt2))
                    value = dt2;
            }
            else if (underlyingType == typeof(double))
                value = double.TryParse(cellVal, NumberStyles.Any, CultureInfo.InvariantCulture, out var dbl) ? dbl : 0.0;
            else
                value = cellVal;

            prop.SetValue(entity, value);
        }

        // Set CreatedAt = DateTime.UtcNow for entities that have it
        var createdAt = entityType.GetProperty("CreatedAt");
        if (createdAt != null && createdAt.PropertyType == typeof(DateTime))
            createdAt.SetValue(entity, DateTime.UtcNow);

        return entity!;
    }

    private void AddSampleRow(ExcelWorksheet sheet, List<PropertyInfo> props, int rowNum)
    {
        foreach (var prop in props)
        {
            var underlying = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            object? sample = null;

            if (underlying == typeof(string))
            {
                var maxLen = prop.GetCustomAttribute<System.ComponentModel.DataAnnotations.MaxLengthAttribute>()?.Length ?? 50;
                sample = new string('X', Math.Min(maxLen, 10));
            }
            else if (underlying == typeof(bool))
                sample = "True";
            else if (underlying == typeof(decimal))
                sample = "0.00";
            else if (underlying == typeof(int))
                sample = "0";
            else if (underlying == typeof(DateTime))
                sample = "2026-01-01";

            var colIdx = props.IndexOf(prop) + 1;
            sheet.Cells[rowNum, colIdx].Value = sample?.ToString() ?? "";
            sheet.Cells[rowNum, colIdx].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            sheet.Cells[rowNum, colIdx].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
        }
    }

    private IQueryable? GetEntityQueryable(string entityType)
    {
        return entityType.ToLower() switch
        {
            "region"        => _context.Regions.AsNoTracking(),
            "branch"        => _context.Branches.AsNoTracking(),
            "department"    => _context.Departments.AsNoTracking(),
            "position"      => _context.Positions.AsNoTracking(),
            "employee"      => _context.Employees.AsNoTracking(),
            "customer"      => _context.Customers.AsNoTracking(),
            "customergroup" => _context.CustomerGroups.AsNoTracking(),
            "saleschannel"  => _context.SalesChannels.AsNoTracking(),
            "opportunitystage" => _context.OpportunityStages.AsNoTracking(),
            "opportunity"  => _context.Opportunities.AsNoTracking(),
            "quote"         => _context.Quotes.AsNoTracking(),
            "salesorder"   => _context.SalesOrders.AsNoTracking(),
            "salesinvoicedetail" => null,
            "salesorderdetail" => null,
            "salesinvoice" => _context.SalesInvoices.AsNoTracking(),
            "salesreturn"  => _context.SalesReturns.AsNoTracking(),
            "customerpayment" => _context.CustomerPayments.AsNoTracking(),
            "product"      => _context.Products.AsNoTracking(),
            "productcategory" => _context.ProductCategories.AsNoTracking(),
            "warehouse"    => _context.Warehouses.AsNoTracking(),
            "supplier"     => _context.Suppliers.AsNoTracking(),
            "purchaseorder" => _context.PurchaseOrders.AsNoTracking(),
            "purchasereceipt" => _context.PurchaseReceipts.AsNoTracking(),
            "purchaseinvoice" => _context.PurchaseInvoices.AsNoTracking(),
            "supplierpayment" => _context.SupplierPayments.AsNoTracking(),
            "expensecategory" => _context.ExpenseCategories.AsNoTracking(),
            "expense"      => _context.Expenses.AsNoTracking(),
            "attendance"   => _context.Attendances.AsNoTracking(),
            "leaverequest" => _context.LeaveRequests.AsNoTracking(),
            "payroll"      => _context.Payrolls.AsNoTracking(),
            "performancereview" => _context.PerformanceReviews.AsNoTracking(),
            "jobopening"   => _context.JobOpenings.AsNoTracking(),
            "applicant"    => _context.Applicants.AsNoTracking(),
            "supportticket" => _context.SupportTickets.AsNoTracking(),
            "marketingcampaign" => _context.MarketingCampaigns.AsNoTracking(),
            "marketinglead" => _context.MarketingLeads.AsNoTracking(),
            "marketingspenddaily" => _context.MarketingSpendDailies.AsNoTracking(),
            "inventory"    => _context.Inventories.AsNoTracking(),
            "inventorysnapshot" => _context.InventorySnapshots.AsNoTracking(),
            "stocktransaction" => _context.StockTransactions.AsNoTracking(),
            "kpitarget"    => _context.KpiTargets.AsNoTracking(),
            _ => null
        };
    }

    private async Task<List<Dictionary<string, object?>>> GetEntityData(string entityType)
    {
        var data = new List<Dictionary<string, object?>>();
        var type = Type.GetType($"Dashboard.Models.{entityType}");
        if (type == null) return data;

        var props = GetEntityProperties(entityType) ?? new();

        switch (entityType.ToLower())
        {
            // Executive
            case "region":        data = await _context.Regions.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "branch":        data = await _context.Branches.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "department":    data = await _context.Departments.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;

            // Sales
            case "customer":      data = await _context.Customers.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "customergroup": data = await _context.CustomerGroups.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "saleschannel":  data = await _context.SalesChannels.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "opportunitystage": data = await _context.OpportunityStages.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "opportunity":  data = await _context.Opportunities.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "quote":        data = await _context.Quotes.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "salesorder":   data = await _context.SalesOrders.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "salesinvoice": data = await _context.SalesInvoices.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "salesreturn":  data = await _context.SalesReturns.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "customerpayment": data = await _context.CustomerPayments.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;

            // Marketing
            case "marketingcampaign": data = await _context.MarketingCampaigns.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "marketinglead":    data = await _context.MarketingLeads.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "marketingspenddaily": data = await _context.MarketingSpendDailies.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;

            // Inventory
            case "product":        data = await _context.Products.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "productcategory": data = await _context.ProductCategories.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "warehouse":      data = await _context.Warehouses.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "supplier":      data = await _context.Suppliers.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "purchaseorder":  data = await _context.PurchaseOrders.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "purchasereceipt": data = await _context.PurchaseReceipts.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "purchaseinvoice": data = await _context.PurchaseInvoices.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "supplierpayment": data = await _context.SupplierPayments.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "inventory":     data = await _context.Inventories.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "inventorysnapshot": data = await _context.InventorySnapshots.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "stocktransaction": data = await _context.StockTransactions.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;

            // HR
            case "position":      data = await _context.Positions.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "employee":      data = await _context.Employees.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "attendance":     data = await _context.Attendances.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "leaverequest":  data = await _context.LeaveRequests.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "payroll":       data = await _context.Payrolls.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "performancereview": data = await _context.PerformanceReviews.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "jobopening":   data = await _context.JobOpenings.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "applicant":     data = await _context.Applicants.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;

            // Finance
            case "expensecategory": data = await _context.ExpenseCategories.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;
            case "expense":      data = await _context.Expenses.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;

            // Customer Service
            case "supportticket": data = await _context.SupportTickets.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;

            // KPI
            case "kpitarget":    data = await _context.KpiTargets.AsNoTracking().Select(r => PropDict(type, props, r)).ToListAsync(); break;

            default: break;
        }

        return data;
    }

    private Dictionary<string, object?> PropDict(Type type, List<PropertyInfo> props, object entity)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in props)
        {
            try { dict[p.Name] = p.GetValue(entity); } catch { dict[p.Name] = null; }
        }
        return dict;
    }
}

#region Result DTOs

public class ExcelReadResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<string> Headers { get; set; } = new();
    public List<Dictionary<string, string?>> Rows { get; set; } = new();
    public int TotalRows { get; set; }
}

public class ExcelValidationResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<Dictionary<string, string?>> ValidRows { get; set; } = new();
    public int TotalRows { get; set; }
    public int InvalidRows { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class ExcelImportResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public List<string> Errors { get; set; } = new();
}

#endregion
