using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Dashboard.Data;
using Dashboard.Models;
using Dashboard.Models.ViewModels;

namespace Dashboard.Services;

public class GlobalSearchService
{
    private const int MinQueryLength = 2;
    private const int MaxQueryLength = 80;
    private const int PerSourceLimit = 6;
    private const int MaxTotalHits = 48;

    private readonly ApplicationDbContext _db;
    private readonly LinkGenerator _link;

    public GlobalSearchService(ApplicationDbContext db, LinkGenerator link)
    {
        _db = db;
        _link = link;
    }

    public async Task<GlobalSearchResponseDto> SearchAsync(
        ClaimsPrincipal user,
        string? rawQuery,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var q = (rawQuery ?? "").Trim();
        if (q.Length < MinQueryLength || q.Length > MaxQueryLength)
            return new GlobalSearchResponseDto { Items = Array.Empty<GlobalSearchHitDto>() };

        var hits = new List<GlobalSearchHitDto>();
        var exec = user.IsInRole(SD.Role_Executive);
        var sales = user.IsInRole(SD.Role_Sales) || exec;
        var marketing = user.IsInRole(SD.Role_Marketing) || exec;
        var inventory = user.IsInRole(SD.Role_Inventory) || exec;
        var finance = user.IsInRole(SD.Role_Finance) || exec;
        var hr = user.IsInRole(SD.Role_HumanResources) || exec;
        var cs = user.IsInRole(SD.Role_CustomerService) || exec;

        AddDashboardShortcuts(hits, exec, sales, marketing, inventory, finance, hr, cs, q, httpContext);

        if (exec)
        {
            await AppendRegions(hits, q, httpContext, cancellationToken);
            await AppendBranches(hits, q, httpContext, cancellationToken);
            await AppendDepartments(hits, q, httpContext, cancellationToken);
        }

        if (sales)
        {
            await AppendCustomers(hits, q, httpContext, cancellationToken);
            await AppendCustomerGroups(hits, q, httpContext, cancellationToken);
            await AppendSalesChannels(hits, q, httpContext, cancellationToken);
            await AppendOpportunityStages(hits, q, httpContext, cancellationToken);
            await AppendOpportunities(hits, q, httpContext, cancellationToken);
            await AppendQuotes(hits, q, httpContext, cancellationToken);
            await AppendSalesOrders(hits, q, httpContext, cancellationToken);
            await AppendSalesInvoices(hits, q, httpContext, cancellationToken);
            await AppendSalesReturns(hits, q, httpContext, cancellationToken);
            await AppendCustomerPayments(hits, q, httpContext, cancellationToken);
        }

        if (marketing)
        {
            await AppendMarketingCampaigns(hits, q, httpContext, cancellationToken);
            await AppendMarketingLeads(hits, q, httpContext, cancellationToken);
            await AppendMarketingSpend(hits, q, httpContext, cancellationToken);
        }

        if (inventory)
        {
            await AppendProducts(hits, q, httpContext, cancellationToken);
            await AppendProductCategories(hits, q, httpContext, cancellationToken);
            await AppendWarehouses(hits, q, httpContext, cancellationToken);
            await AppendSuppliers(hits, q, httpContext, cancellationToken);
            await AppendPurchaseOrders(hits, q, httpContext, cancellationToken);
            await AppendPurchaseReceipts(hits, q, httpContext, cancellationToken);
            await AppendPurchaseInvoices(hits, q, httpContext, cancellationToken);
            await AppendSupplierPayments(hits, q, httpContext, cancellationToken);
        }

        if (finance)
        {
            await AppendExpenses(hits, q, httpContext, cancellationToken);
            await AppendExpenseCategories(hits, q, httpContext, cancellationToken);
        }

        if (hr)
        {
            await AppendPositions(hits, q, httpContext, cancellationToken);
            await AppendEmployees(hits, q, httpContext, cancellationToken);
            await AppendAttendances(hits, q, httpContext, cancellationToken);
            await AppendLeaveRequests(hits, q, httpContext, cancellationToken);
            await AppendPayrolls(hits, q, httpContext, cancellationToken);
            await AppendPerformanceReviews(hits, q, httpContext, cancellationToken);
            await AppendJobOpenings(hits, q, httpContext, cancellationToken);
            await AppendApplicants(hits, q, httpContext, cancellationToken);
        }

        if (cs)
            await AppendSupportTickets(hits, q, httpContext, cancellationToken);

        var list = hits.Take(MaxTotalHits).ToList();
        return new GlobalSearchResponseDto { Items = list, Truncated = list.Count >= MaxTotalHits };
    }

    private void AddDashboardShortcuts(
        List<GlobalSearchHitDto> hits,
        bool exec,
        bool sales,
        bool marketing,
        bool inventory,
        bool finance,
        bool hr,
        bool cs,
        string q,
        HttpContext http)
    {
        void TryAdd(bool allowed, string[] needles, string title, string subtitle, string controller, string action)
        {
            if (!allowed || hits.Count >= MaxTotalHits) return;
            if (!needles.Any(n => q.Contains(n, StringComparison.OrdinalIgnoreCase))) return;
            var url = ActionUrl(http, action, controller);
            if (url == "#") return;
            hits.Add(new GlobalSearchHitDto
            {
                Section = "Dashboard",
                Entity = "Navigation",
                Title = title,
                Subtitle = subtitle,
                Url = url
            });
        }

        TryAdd(true, new[] { "analytics", "dashboard", "home" }, "Analytics dashboard", "Main overview", "Dashboards", "Index");
        TryAdd(exec, new[] { "executive", "company" }, "Executive dashboard", "Company-wide KPIs", "Executive", "Index");
        TryAdd(sales, new[] { "sales", "revenue", "pipeline" }, "Sales dashboard", "Sales performance", "Sales", "Index");
        TryAdd(marketing, new[] { "marketing", "campaign", "lead" }, "Marketing dashboard", "Campaigns & leads", "Marketing", "Index");
        TryAdd(inventory, new[] { "inventory", "stock", "warehouse" }, "Inventory dashboard", "Stock overview", "Inventory", "Index");
        TryAdd(finance, new[] { "finance", "expense", "budget" }, "Finance dashboard", "Financial overview", "Finance", "Index");
        TryAdd(hr, new[] { "hr", "human resource", "payroll", "employee" }, "HR dashboard", "Human resources", "HumanResources", "Index");
        TryAdd(cs, new[] { "ticket", "support", "customer service" }, "Customer service dashboard", "Support overview", "CustomerService", "Index");
    }

    private string ActionUrl(HttpContext http, string action, string controller, object? routeValues = null) =>
        _link.GetPathByAction(http, action, controller, routeValues) ?? "#";

    private bool CanAdd(List<GlobalSearchHitDto> hits) => hits.Count < MaxTotalHits;

    private void AddHit(List<GlobalSearchHitDto> hits, HttpContext http, string entity, string title, string subtitle, string controller, string action, object routeValues)
    {
        if (!CanAdd(hits)) return;
        var url = ActionUrl(http, action, controller, routeValues);
        if (url == "#") return;
        hits.Add(new GlobalSearchHitDto
        {
            Section = "Data",
            Entity = entity,
            Title = title,
            Subtitle = subtitle,
            Url = url
        });
    }

    // --- Executive ---
    private async Task AppendRegions(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.Regions.AsNoTracking()
            .Where(r => r.RegionCode.Contains(q) || r.RegionName.Contains(q) || (r.Description != null && r.Description.Contains(q)))
            .OrderBy(r => r.RegionName).Take(PerSourceLimit).Select(r => new { r.RegionID, r.RegionName, r.RegionCode }).ToListAsync(ct);
        foreach (var r in rows)
            AddHit(hits, http, "Regions", r.RegionName, r.RegionCode, "ExecutiveCrud", "Regions_Edit", new { id = r.RegionID });
    }

    private async Task AppendBranches(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.Branches.AsNoTracking()
            .Where(b => b.BranchCode.Contains(q) || b.BranchName.Contains(q) || (b.City != null && b.City.Contains(q)))
            .OrderBy(b => b.BranchName).Take(PerSourceLimit).Select(b => new { b.BranchID, b.BranchName, b.BranchCode }).ToListAsync(ct);
        foreach (var b in rows)
            AddHit(hits, http, "Branches", b.BranchName, b.BranchCode, "ExecutiveCrud", "Branches_Edit", new { id = b.BranchID });
    }

    private async Task AppendDepartments(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.Departments.AsNoTracking()
            .Where(d => d.DepartmentCode.Contains(q) || d.DepartmentName.Contains(q) || (d.Description != null && d.Description.Contains(q)))
            .OrderBy(d => d.DepartmentName).Take(PerSourceLimit).Select(d => new { d.DepartmentID, d.DepartmentName, d.DepartmentCode }).ToListAsync(ct);
        foreach (var d in rows)
            AddHit(hits, http, "Departments", d.DepartmentName, d.DepartmentCode, "ExecutiveCrud", "Departments_Edit", new { id = d.DepartmentID });
    }

    // --- Sales ---
    private async Task AppendCustomers(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.Customers.AsNoTracking()
            .Where(c => c.CustomerCode.Contains(q) || c.CustomerName.Contains(q) || (c.Email != null && c.Email.Contains(q)) || (c.Phone != null && c.Phone.Contains(q)))
            .OrderBy(c => c.CustomerName).Take(PerSourceLimit).Select(c => new { c.CustomerID, c.CustomerName, c.CustomerCode }).ToListAsync(ct);
        foreach (var c in rows)
            AddHit(hits, http, "Customers", c.CustomerName, c.CustomerCode, "SalesCrud", "Customers_Edit", new { id = c.CustomerID });
    }

    private async Task AppendCustomerGroups(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.CustomerGroups.AsNoTracking()
            .Where(g => g.GroupCode.Contains(q) || g.GroupName.Contains(q) || (g.Description != null && g.Description.Contains(q)))
            .OrderBy(g => g.GroupName).Take(PerSourceLimit).Select(g => new { g.CustomerGroupID, g.GroupName, g.GroupCode }).ToListAsync(ct);
        foreach (var g in rows)
            AddHit(hits, http, "Customer groups", g.GroupName, g.GroupCode, "SalesCrud", "CustomerGroups_Edit", new { id = g.CustomerGroupID });
    }

    private async Task AppendSalesChannels(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.SalesChannels.AsNoTracking()
            .Where(s => s.ChannelCode.Contains(q) || s.ChannelName.Contains(q) || (s.Description != null && s.Description.Contains(q)))
            .OrderBy(s => s.ChannelName).Take(PerSourceLimit).Select(s => new { s.SalesChannelID, s.ChannelName, s.ChannelCode }).ToListAsync(ct);
        foreach (var s in rows)
            AddHit(hits, http, "Sales channels", s.ChannelName, s.ChannelCode, "SalesCrud", "SalesChannels_Edit", new { id = s.SalesChannelID });
    }

    private async Task AppendOpportunityStages(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.OpportunityStages.AsNoTracking()
            .Where(s => s.StageCode.Contains(q) || s.StageName.Contains(q))
            .OrderBy(s => s.StageOrder).Take(PerSourceLimit).Select(s => new { s.StageID, s.StageName, s.StageCode }).ToListAsync(ct);
        foreach (var s in rows)
            AddHit(hits, http, "Opportunity stages", s.StageName, s.StageCode, "SalesCrud", "OpportunityStages_Edit", new { id = s.StageID });
    }

    private async Task AppendOpportunities(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.Opportunities.AsNoTracking()
            .Include(o => o.Customer)
            .Where(o => o.OpportunityCode.Contains(q)
                || (o.Customer != null && o.Customer.CustomerName.Contains(q))
                || (o.SourceChannel != null && o.SourceChannel.Contains(q)))
            .OrderByDescending(o => o.CreatedAt).Take(PerSourceLimit)
            .Select(o => new { o.OpportunityID, o.OpportunityCode, CustomerName = o.Customer != null ? o.Customer.CustomerName : null }).ToListAsync(ct);
        foreach (var o in rows)
            AddHit(hits, http, "Opportunities", o.OpportunityCode, o.CustomerName ?? "", "SalesCrud", "Opportunities_Edit", new { id = o.OpportunityID });
    }

    private async Task AppendQuotes(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.Quotes.AsNoTracking()
            .Where(x => x.QuoteNumber.Contains(q))
            .OrderByDescending(x => x.QuoteDate).Take(PerSourceLimit).Select(x => new { x.QuoteID, x.QuoteNumber }).ToListAsync(ct);
        foreach (var x in rows)
            AddHit(hits, http, "Quotes", x.QuoteNumber, "Quote", "SalesCrud", "Quotes_Edit", new { id = x.QuoteID });
    }

    private async Task AppendSalesOrders(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.SalesOrders.AsNoTracking()
            .Where(x => x.OrderNumber.Contains(q))
            .OrderByDescending(x => x.OrderDate).Take(PerSourceLimit).Select(x => new { x.SalesOrderID, x.OrderNumber }).ToListAsync(ct);
        foreach (var x in rows)
            AddHit(hits, http, "Sales orders", x.OrderNumber, "Order", "SalesCrud", "SalesOrders_Edit", new { id = x.SalesOrderID });
    }

    private async Task AppendSalesInvoices(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.SalesInvoices.AsNoTracking()
            .Where(x => x.InvoiceNumber.Contains(q))
            .OrderByDescending(x => x.InvoiceDate).Take(PerSourceLimit).Select(x => new { x.InvoiceID, x.InvoiceNumber }).ToListAsync(ct);
        foreach (var x in rows)
            AddHit(hits, http, "Sales invoices", x.InvoiceNumber, "Invoice", "SalesCrud", "SalesInvoices_Edit", new { id = x.InvoiceID });
    }

    private async Task AppendSalesReturns(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.SalesReturns.AsNoTracking()
            .Where(x => x.ReturnNumber.Contains(q))
            .OrderByDescending(x => x.ReturnDate).Take(PerSourceLimit).Select(x => new { x.ReturnID, x.ReturnNumber }).ToListAsync(ct);
        foreach (var x in rows)
            AddHit(hits, http, "Sales returns", x.ReturnNumber, "Return", "SalesCrud", "SalesReturns_Edit", new { id = x.ReturnID });
    }

    private async Task AppendCustomerPayments(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.CustomerPayments.AsNoTracking()
            .Where(x => x.PaymentNumber.Contains(q) || (x.ReferenceNumber != null && x.ReferenceNumber.Contains(q)) || (x.Notes != null && x.Notes.Contains(q)))
            .OrderByDescending(x => x.PaymentDate).Take(PerSourceLimit).Select(x => new { x.PaymentID, x.PaymentNumber }).ToListAsync(ct);
        foreach (var x in rows)
            AddHit(hits, http, "Customer payments", x.PaymentNumber, "Payment", "SalesCrud", "CustomerPayments_Edit", new { id = x.PaymentID });
    }

    // --- Marketing ---
    private async Task AppendMarketingCampaigns(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.MarketingCampaigns.AsNoTracking()
            .Where(c => c.CampaignCode.Contains(q) || c.CampaignName.Contains(q) || c.Channel.Contains(q) || (c.Objective != null && c.Objective.Contains(q)))
            .OrderBy(c => c.CampaignName).Take(PerSourceLimit).Select(c => new { c.CampaignID, c.CampaignName, c.CampaignCode }).ToListAsync(ct);
        foreach (var c in rows)
            AddHit(hits, http, "Campaigns", c.CampaignName, c.CampaignCode, "MarketingCrud", "MarketingCampaignsEdit", new { id = c.CampaignID });
    }

    private async Task AppendMarketingLeads(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.MarketingLeads.AsNoTracking()
            .Where(l => l.LeadCode.Contains(q) || l.LeadName.Contains(q) || (l.CompanyName != null && l.CompanyName.Contains(q)) || (l.Email != null && l.Email.Contains(q)) || (l.Phone != null && l.Phone.Contains(q)))
            .OrderByDescending(l => l.CreatedDate).Take(PerSourceLimit).Select(l => new { l.LeadID, l.LeadName, l.LeadCode }).ToListAsync(ct);
        foreach (var l in rows)
            AddHit(hits, http, "Leads", l.LeadName, l.LeadCode, "MarketingCrud", "MarketingLeadsEdit", new { id = l.LeadID });
    }

    private async Task AppendMarketingSpend(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.MarketingSpendDailies.AsNoTracking()
            .Include(s => s.Campaign)
            .Where(s => (s.Notes != null && s.Notes.Contains(q)) || (s.Campaign != null && s.Campaign.CampaignName.Contains(q)))
            .OrderByDescending(s => s.SpendDate).Take(PerSourceLimit)
            .Select(s => new { s.SpendID, CampaignName = s.Campaign != null ? s.Campaign.CampaignName : "Spend", s.SpendDate }).ToListAsync(ct);
        foreach (var s in rows)
            AddHit(hits, http, "Marketing spend", s.CampaignName, s.SpendDate.ToString("yyyy-MM-dd"), "MarketingCrud", "MarketingSpendDailiesEdit", new { id = s.SpendID });
    }

    // --- Inventory ---
    private async Task AppendProducts(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.Products.AsNoTracking()
            .Where(p => p.ProductCode.Contains(q) || p.ProductName.Contains(q) || (p.Brand != null && p.Brand.Contains(q)))
            .OrderBy(p => p.ProductName).Take(PerSourceLimit).Select(p => new { p.ProductID, p.ProductName, p.ProductCode }).ToListAsync(ct);
        foreach (var p in rows)
            AddHit(hits, http, "Products", p.ProductName, p.ProductCode, "InventoryCrud", "Products_Edit", new { id = p.ProductID });
    }

    private async Task AppendProductCategories(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.ProductCategories.AsNoTracking()
            .Where(c => c.CategoryCode.Contains(q) || c.CategoryName.Contains(q) || (c.Description != null && c.Description.Contains(q)))
            .OrderBy(c => c.CategoryName).Take(PerSourceLimit).Select(c => new { c.CategoryID, c.CategoryName, c.CategoryCode }).ToListAsync(ct);
        foreach (var c in rows)
            AddHit(hits, http, "Product categories", c.CategoryName, c.CategoryCode, "InventoryCrud", "ProductCategories_Edit", new { id = c.CategoryID });
    }

    private async Task AppendWarehouses(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.Warehouses.AsNoTracking()
            .Where(w => w.WarehouseCode.Contains(q) || w.WarehouseName.Contains(q) || (w.City != null && w.City.Contains(q)))
            .OrderBy(w => w.WarehouseName).Take(PerSourceLimit).Select(w => new { w.WarehouseID, w.WarehouseName, w.WarehouseCode }).ToListAsync(ct);
        foreach (var w in rows)
            AddHit(hits, http, "Warehouses", w.WarehouseName, w.WarehouseCode, "InventoryCrud", "Warehouses_Edit", new { id = w.WarehouseID });
    }

    private async Task AppendSuppliers(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.Suppliers.AsNoTracking()
            .Where(s => s.SupplierCode.Contains(q) || s.SupplierName.Contains(q) || (s.Email != null && s.Email.Contains(q)) || (s.Phone != null && s.Phone.Contains(q)))
            .OrderBy(s => s.SupplierName).Take(PerSourceLimit).Select(s => new { s.SupplierID, s.SupplierName, s.SupplierCode }).ToListAsync(ct);
        foreach (var s in rows)
            AddHit(hits, http, "Suppliers", s.SupplierName, s.SupplierCode, "InventoryCrud", "Suppliers_Edit", new { id = s.SupplierID });
    }

    private async Task AppendPurchaseOrders(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.PurchaseOrders.AsNoTracking()
            .Where(x => x.OrderNumber.Contains(q))
            .OrderByDescending(x => x.OrderDate).Take(PerSourceLimit).Select(x => new { x.PurchaseOrderID, x.OrderNumber }).ToListAsync(ct);
        foreach (var x in rows)
            AddHit(hits, http, "Purchase orders", x.OrderNumber, "PO", "InventoryCrud", "PurchaseOrders_Edit", new { id = x.PurchaseOrderID });
    }

    private async Task AppendPurchaseReceipts(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.PurchaseReceipts.AsNoTracking()
            .Where(x => x.ReceiptNumber.Contains(q))
            .OrderByDescending(x => x.ReceiptDate).Take(PerSourceLimit).Select(x => new { x.ReceiptID, x.ReceiptNumber }).ToListAsync(ct);
        foreach (var x in rows)
            AddHit(hits, http, "Purchase receipts", x.ReceiptNumber, "Receipt", "InventoryCrud", "PurchaseReceipts_Edit", new { id = x.ReceiptID });
    }

    private async Task AppendPurchaseInvoices(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.PurchaseInvoices.AsNoTracking()
            .Where(x => x.InvoiceNumber.Contains(q))
            .OrderByDescending(x => x.InvoiceDate).Take(PerSourceLimit).Select(x => new { x.InvoiceID, x.InvoiceNumber }).ToListAsync(ct);
        foreach (var x in rows)
            AddHit(hits, http, "Purchase invoices", x.InvoiceNumber, "Invoice", "InventoryCrud", "PurchaseInvoices_Edit", new { id = x.InvoiceID });
    }

    private async Task AppendSupplierPayments(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.SupplierPayments.AsNoTracking()
            .Where(x => x.PaymentNumber.Contains(q) || (x.ReferenceNumber != null && x.ReferenceNumber.Contains(q)) || (x.Notes != null && x.Notes.Contains(q)))
            .OrderByDescending(x => x.PaymentDate).Take(PerSourceLimit).Select(x => new { x.PaymentID, x.PaymentNumber }).ToListAsync(ct);
        foreach (var x in rows)
            AddHit(hits, http, "Supplier payments", x.PaymentNumber, "Payment", "InventoryCrud", "SupplierPayments_Edit", new { id = x.PaymentID });
    }

    // --- Finance ---
    private async Task AppendExpenses(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.Expenses.AsNoTracking()
            .Include(e => e.Employee)
            .Where(e => e.ExpenseNumber.Contains(q) || (e.Description != null && e.Description.Contains(q)) || (e.Employee != null && e.Employee.FullName.Contains(q)))
            .OrderByDescending(e => e.ExpenseDate).Take(PerSourceLimit)
            .Select(e => new { e.ExpenseID, e.ExpenseNumber, Name = e.Employee != null ? e.Employee.FullName : "" }).ToListAsync(ct);
        foreach (var e in rows)
            AddHit(hits, http, "Expenses", e.ExpenseNumber, e.Name, "FinanceCrud", "Expenses_Edit", new { id = e.ExpenseID });
    }

    private async Task AppendExpenseCategories(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.ExpenseCategories.AsNoTracking()
            .Where(c => c.CategoryCode.Contains(q) || c.CategoryName.Contains(q) || (c.Description != null && c.Description.Contains(q)))
            .OrderBy(c => c.CategoryName).Take(PerSourceLimit).Select(c => new { c.ExpenseCategoryID, c.CategoryName, c.CategoryCode }).ToListAsync(ct);
        foreach (var c in rows)
            AddHit(hits, http, "Expense categories", c.CategoryName, c.CategoryCode, "FinanceCrud", "ExpenseCategories_Edit", new { id = c.ExpenseCategoryID });
    }

    // --- HR ---
    private async Task AppendPositions(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.Positions.AsNoTracking()
            .Where(p => p.PositionCode.Contains(q) || p.PositionName.Contains(q) || (p.Description != null && p.Description.Contains(q)))
            .OrderBy(p => p.PositionName).Take(PerSourceLimit).Select(p => new { p.PositionID, p.PositionName, p.PositionCode }).ToListAsync(ct);
        foreach (var p in rows)
            AddHit(hits, http, "Positions", p.PositionName, p.PositionCode, "HumanResourcesCrud", "Positions_Edit", new { id = p.PositionID });
    }

    private async Task AppendEmployees(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.Employees.AsNoTracking()
            .Where(e => e.EmployeeCode.Contains(q) || e.FullName.Contains(q) || (e.Email != null && e.Email.Contains(q)) || (e.Phone != null && e.Phone.Contains(q)))
            .OrderBy(e => e.FullName).Take(PerSourceLimit).Select(e => new { e.EmployeeID, e.FullName, e.EmployeeCode }).ToListAsync(ct);
        foreach (var e in rows)
            AddHit(hits, http, "Employees", e.FullName, e.EmployeeCode, "HumanResourcesCrud", "Employees_Edit", new { id = e.EmployeeID });
    }

    private async Task AppendAttendances(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.Attendances.AsNoTracking()
            .Include(a => a.Employee)
            .Where(a => a.Status.Contains(q) || (a.Notes != null && a.Notes.Contains(q)) || (a.Employee != null && a.Employee.FullName.Contains(q)))
            .OrderByDescending(a => a.AttendanceDate).Take(PerSourceLimit)
            .Select(a => new { a.AttendanceID, Name = a.Employee != null ? a.Employee.FullName : "", a.AttendanceDate }).ToListAsync(ct);
        foreach (var a in rows)
            AddHit(hits, http, "Attendances", a.Name, a.AttendanceDate.ToString("yyyy-MM-dd"), "HumanResourcesCrud", "Attendances_Edit", new { id = a.AttendanceID });
    }

    private async Task AppendLeaveRequests(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.LeaveRequests.AsNoTracking()
            .Include(l => l.Employee)
            .Where(l => l.LeaveType.Contains(q) || l.Status.Contains(q) || (l.Reason != null && l.Reason.Contains(q)) || (l.Employee != null && l.Employee.FullName.Contains(q)))
            .OrderByDescending(l => l.StartDate).Take(PerSourceLimit)
            .Select(l => new { l.LeaveRequestID, Name = l.Employee != null ? l.Employee.FullName : "", l.LeaveType }).ToListAsync(ct);
        foreach (var l in rows)
            AddHit(hits, http, "Leave requests", l.Name, l.LeaveType, "HumanResourcesCrud", "LeaveRequests_Edit", new { id = l.LeaveRequestID });
    }

    private async Task AppendPayrolls(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.Payrolls.AsNoTracking()
            .Include(p => p.Employee)
            .Where(p => p.PayrollPeriod.Contains(q) || (p.Employee != null && p.Employee.FullName.Contains(q)))
            .OrderByDescending(p => p.PaymentDate).Take(PerSourceLimit)
            .Select(p => new { p.PayrollID, p.PayrollPeriod, Name = p.Employee != null ? p.Employee.FullName : "" }).ToListAsync(ct);
        foreach (var p in rows)
            AddHit(hits, http, "Payrolls", p.PayrollPeriod, p.Name, "HumanResourcesCrud", "Payrolls_Edit", new { id = p.PayrollID });
    }

    private async Task AppendPerformanceReviews(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.PerformanceReviews.AsNoTracking()
            .Include(p => p.Employee)
            .Where(p => (p.Comments != null && p.Comments.Contains(q)) || (p.Goals != null && p.Goals.Contains(q)) || (p.Strengths != null && p.Strengths.Contains(q)) || (p.Employee != null && p.Employee.FullName.Contains(q)))
            .OrderByDescending(p => p.ReviewDate).Take(PerSourceLimit)
            .Select(p => new { p.ReviewID, Name = p.Employee != null ? p.Employee.FullName : "", p.ReviewDate }).ToListAsync(ct);
        foreach (var p in rows)
            AddHit(hits, http, "Performance reviews", p.Name, p.ReviewDate.ToString("yyyy-MM-dd"), "HumanResourcesCrud", "PerformanceReviews_Edit", new { id = p.ReviewID });
    }

    private async Task AppendJobOpenings(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.JobOpenings.AsNoTracking()
            .Where(j => j.Title.Contains(q) || (j.Location != null && j.Location.Contains(q)) || (j.JobDescription != null && j.JobDescription.Contains(q)))
            .OrderByDescending(j => j.PostedDate).Take(PerSourceLimit).Select(j => new { j.JobOpeningID, j.Title, j.EmploymentType }).ToListAsync(ct);
        foreach (var j in rows)
            AddHit(hits, http, "Job openings", j.Title, j.EmploymentType, "HumanResourcesCrud", "JobOpenings_Edit", new { id = j.JobOpeningID });
    }

    private async Task AppendApplicants(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.Applicants.AsNoTracking()
            .Where(a => a.FullName.Contains(q) || a.Email.Contains(q) || (a.Phone != null && a.Phone.Contains(q)))
            .OrderByDescending(a => a.ApplicantID).Take(PerSourceLimit).Select(a => new { a.ApplicantID, a.FullName, a.Email }).ToListAsync(ct);
        foreach (var a in rows)
            AddHit(hits, http, "Applicants", a.FullName, a.Email, "HumanResourcesCrud", "Applicants_Edit", new { id = a.ApplicantID });
    }

    // --- Customer service ---
    private async Task AppendSupportTickets(List<GlobalSearchHitDto> hits, string q, HttpContext http, CancellationToken ct)
    {
        if (!CanAdd(hits)) return;
        var rows = await _db.SupportTickets.AsNoTracking()
            .Include(t => t.Customer)
            .Where(t => t.TicketNumber.Contains(q) || t.Subject.Contains(q) || t.TicketType.Contains(q) || t.Priority.Contains(q) || t.Status.Contains(q)
                || (t.Customer != null && t.Customer.CustomerName.Contains(q)))
            .OrderByDescending(t => t.CreatedAt).Take(PerSourceLimit)
            .Select(t => new { t.TicketID, t.TicketNumber, t.Subject }).ToListAsync(ct);
        foreach (var t in rows)
            AddHit(hits, http, "Support tickets", t.TicketNumber, t.Subject, "CustomerServiceCrud", "SupportTickets_Edit", new { id = t.TicketID });
    }
}
