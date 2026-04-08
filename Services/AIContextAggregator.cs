using System.Text;
using Dashboard.Models.ViewModels;
using Dashboard.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Dashboard.Services;

public class AIContextData
{
    public string Department { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public DateTime DateRange { get; set; } = DateTime.UtcNow;
    public string KpiSummary { get; set; } = string.Empty;
    public string TopItemsSummary { get; set; } = string.Empty;
    public string RecentDataSummary { get; set; } = string.Empty;
    public string ChartSummary { get; set; } = string.Empty;
}

public class AIContextAggregator
{
    private readonly ISalesDashboardService _salesService;
    private readonly IFinanceDashboardService _financeService;
    private readonly IMarketingDashboardService _marketingService;
    private readonly IInventoryDashboardService _inventoryService;
    private readonly IHumanResourcesDashboardService _hrService;
    private readonly ICustomerServiceDashboardService _csService;
    private readonly IExecutiveDashboardService _executiveService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AIContextAggregator> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public AIContextAggregator(
        ISalesDashboardService salesService,
        IFinanceDashboardService financeService,
        IMarketingDashboardService marketingService,
        IInventoryDashboardService inventoryService,
        IHumanResourcesDashboardService hrService,
        ICustomerServiceDashboardService csService,
        IExecutiveDashboardService executiveService,
        IMemoryCache cache,
        ILogger<AIContextAggregator> logger)
    {
        _salesService = salesService;
        _financeService = financeService;
        _marketingService = marketingService;
        _inventoryService = inventoryService;
        _hrService = hrService;
        _csService = csService;
        _executiveService = executiveService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<AIContextData> GetContextAsync(string department, string userName, string userRole, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var from = fromDate ?? DateTime.UtcNow.AddMonths(-1);
        var to = toDate ?? DateTime.UtcNow;

        var cacheKey = $"ai_context_{department.ToLower()}_{from:yyyyMMdd}_{to:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out AIContextData? cached) && cached != null)
        {
            cached.UserName = userName;
            cached.UserRole = userRole;
            return cached;
        }

        var context = new AIContextData
        {
            Department = department,
            UserName = userName,
            UserRole = userRole,
            DateRange = to
        };

        try
        {
            context.KpiSummary = department.ToLower() switch
            {
                "sales" => await BuildSalesKpiSummaryAsync(from, to),
                "finance" => await BuildFinanceKpiSummaryAsync(from, to),
                "marketing" => await BuildMarketingKpiSummaryAsync(from, to),
                "inventory" => await BuildInventoryKpiSummaryAsync(from, to),
                "hr" => await BuildHrKpiSummaryAsync(from, to),
                "cskh" => await BuildCsKpiSummaryAsync(from, to),
                "executive" => await BuildExecutiveKpiSummaryAsync(from, to),
                _ => "Dữ liệu tổng hợp từ nhiều phòng ban."
            };

            context.TopItemsSummary = department.ToLower() switch
            {
                "sales" => await BuildSalesTopItemsSummaryAsync(from, to),
                "finance" => await BuildFinanceTopItemsSummaryAsync(from, to),
                "marketing" => await BuildMarketingTopItemsSummaryAsync(from, to),
                "inventory" => await BuildInventoryTopItemsSummaryAsync(from, to),
                "hr" => await BuildHrTopItemsSummaryAsync(from, to),
                "cskh" => await BuildCsTopItemsSummaryAsync(from, to),
                "executive" => await BuildExecutiveTopItemsSummaryAsync(from, to),
                _ => string.Empty
            };

            context.RecentDataSummary = department.ToLower() switch
            {
                "sales" => await BuildSalesRecentSummaryAsync(from, to),
                "finance" => await BuildFinanceRecentSummaryAsync(from, to),
                "marketing" => await BuildMarketingRecentSummaryAsync(from, to),
                "inventory" => await BuildInventoryRecentSummaryAsync(from, to),
                "hr" => await BuildHrRecentSummaryAsync(from, to),
                "cskh" => await BuildCsRecentSummaryAsync(from, to),
                "executive" => await BuildExecutiveRecentSummaryAsync(from, to),
                _ => string.Empty
            };

            context.ChartSummary = department.ToLower() switch
            {
                "sales" => await BuildSalesChartSummaryAsync(from, to),
                "finance" => await BuildFinanceChartSummaryAsync(from, to),
                "marketing" => await BuildMarketingChartSummaryAsync(from, to),
                "inventory" => await BuildInventoryChartSummaryAsync(from, to),
                "hr" => await BuildHrChartSummaryAsync(from, to),
                "cskh" => await BuildCsChartSummaryAsync(from, to),
                "executive" => await BuildExecutiveChartSummaryAsync(from, to),
                _ => string.Empty
            };

            _cache.Set(cacheKey, context, CacheDuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building AI context for department {Department}", department);
            context.KpiSummary = "Dữ liệu đang được cập nhật. Vui lòng thử lại sau.";
        }

        return context;
    }

    // ==================== SALES ====================

    private async Task<string> BuildSalesKpiSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var totalSales = await _salesService.GetTotalSalesAsync(from, to);
        sb.AppendLine($"- Tổng doanh số: {totalSales.FormattedValue} (tăng {totalSales.GrowthPercent:+0;-0;0}% so với kỳ trước)");
        var totalOrders = await _salesService.GetTotalOrdersAsync(from, to);
        sb.AppendLine($"- Tổng đơn hàng: {totalOrders.FormattedValue} ({totalOrders.GrowthPercent:+0;-0;0}%)");
        var winRate = await _salesService.GetWinRateAsync(from, to);
        sb.AppendLine($"- Tỷ lệ thắng: {winRate.FormattedValue}");
        var margin = await _salesService.GetGrossMarginAsync(from, to);
        sb.AppendLine($"- Biên lợi nhuận gộp: {margin.FormattedValue}");
        var newCustomers = await _salesService.GetNewCustomersAsync(from, to);
        sb.AppendLine($"- Khách hàng mới: {newCustomers.FormattedValue}");
        var pendingDeals = await _salesService.GetPendingDealsAsync(from, to);
        sb.AppendLine($"- Cơ hội đang chờ: {pendingDeals.FormattedValue}");
        return sb.ToString();
    }

    private async Task<string> BuildSalesTopItemsSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var topProducts = await _salesService.GetTopProductsAsync(5, from, to);
        if (topProducts.Any())
        {
            sb.AppendLine("Top 5 sản phẩm bán chạy:");
            foreach (var p in topProducts) sb.AppendLine($"  - {p.Name}: {p.FormattedValue}");
        }
        var topSalespersons = await _salesService.GetTopSalespersonsAsync(5, from, to);
        if (topSalespersons.Any())
        {
            sb.AppendLine("Top 5 nhân viên xuất sắc:");
            foreach (var s in topSalespersons) sb.AppendLine($"  - {s.Name}: {s.FormattedValue}");
        }
        var pareto = await _salesService.GetParetoByCustomerAsync(5, from, to);
        if (pareto.Any())
        {
            sb.AppendLine("Top 5 khách hàng doanh thu cao nhất:");
            foreach (var c in pareto) sb.AppendLine($"  - {c.Name}: {c.FormattedValue}");
        }
        return sb.ToString();
    }

    private async Task<string> BuildSalesRecentSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var recentOrders = await _salesService.GetRecentOrdersAsync(5, from, to);
        if (recentOrders.Any())
        {
            sb.AppendLine("5 đơn hàng gần nhất:");
            foreach (var o in recentOrders) sb.AppendLine($"  - {o.Column1}");
        }
        var funnel = await _salesService.GetSalesFunnelAsync(from, to);
        if (funnel.Any())
        {
            sb.AppendLine("Pipeline bán hàng:");
            foreach (var f in funnel) sb.AppendLine($"  - {f.Stage}: {f.Count} ({f.ConversionRate}%)");
        }
        return sb.ToString();
    }

    private async Task<string> BuildSalesChartSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var overviewChart = await _salesService.GetSalesOverviewChartAsync(from, to);
        if (overviewChart.Categories.Any())
        {
            sb.AppendLine($"Xu hướng doanh số ({overviewChart.Categories.First()} - {overviewChart.Categories.Last()}):");
            foreach (var series in overviewChart.Series)
                sb.AppendLine($"  - {series.Name}: {string.Join(", ", series.Data.Take(3))}...");
        }
        var channelChart = await _salesService.GetRevenueByChannelChartAsync(from, to);
        if (channelChart.Categories.Any())
        {
            sb.AppendLine("Doanh thu theo kênh:");
            for (int i = 0; i < channelChart.Categories.Count && i < channelChart.Series[0].Data.Count; i++)
                sb.AppendLine($"  - {channelChart.Categories[i]}: {channelChart.Series[0].Data[i]:N0}");
        }
        return sb.ToString();
    }

    // ==================== FINANCE ====================

    private async Task<string> BuildFinanceKpiSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var totalIncome = await _financeService.GetTotalIncomeAsync(from, to);
        sb.AppendLine($"- Tổng thu nhập: {totalIncome.FormattedValue} ({totalIncome.GrowthPercent:+0;-0;0}%)");
        var totalExpenses = await _financeService.GetTotalExpensesAsync(from, to);
        sb.AppendLine($"- Tổng chi phí: {totalExpenses.FormattedValue} ({totalExpenses.GrowthPercent:+0;-0;0}%)");
        var netProfit = await _financeService.GetNetProfitAsync(from, to);
        sb.AppendLine($"- Lợi nhuận ròng: {netProfit.FormattedValue} ({netProfit.GrowthPercent:+0;-0;0}%)");
        var cashFlow = await _financeService.GetCashFlowAsync(from, to);
        sb.AppendLine($"- Dòng tiền: {cashFlow.FormattedValue}");
        var profitMargin = await _financeService.GetProfitMarginAsync(from, to);
        sb.AppendLine($"- Tỷ lệ lợi nhuận: {profitMargin.FormattedValue}");
        var dso = await _financeService.GetDsoAsync(from, to);
        sb.AppendLine($"- DSO (ngày thu tiền): {dso.FormattedValue}");
        var dpo = await _financeService.GetDpoAsync(from, to);
        sb.AppendLine($"- DPO (ngày trả tiền): {dpo.FormattedValue}");
        var arBalance = await _financeService.GetArBalanceAsync(from, to);
        sb.AppendLine($"- Công nợ phải thu: {arBalance.FormattedValue}");
        return sb.ToString();
    }

    private async Task<string> BuildFinanceTopItemsSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var expenseBreakdown = await _financeService.GetExpenseBreakdownAsync(from, to);
        if (expenseBreakdown.Any())
        {
            sb.AppendLine("Chi phí theo danh mục:");
            foreach (var e in expenseBreakdown) sb.AppendLine($"  - {e.Name}: {e.FormattedValue}");
        }
        var budgetStatus = await _financeService.GetMonthlyBudgetStatusAsync(from, to);
        if (budgetStatus.Any())
        {
            sb.AppendLine("Tình trạng ngân sách:");
            foreach (var b in budgetStatus) sb.AppendLine($"  - {b.Name}: {b.FormattedValue}");
        }
        return sb.ToString();
    }

    private async Task<string> BuildFinanceRecentSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var transactions = await _financeService.GetRecentTransactionsAsync(5, from, to);
        if (transactions.Any())
        {
            sb.AppendLine("5 giao dịch gần nhất:");
            foreach (var t in transactions) sb.AppendLine($"  - {t.Column1}");
        }
        return sb.ToString();
    }

    private async Task<string> BuildFinanceChartSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var overviewChart = await _financeService.GetFinancialOverviewChartAsync(from, to);
        if (overviewChart.Categories.Any())
        {
            sb.AppendLine($"Xu hướng tài chính ({overviewChart.Categories.First()} - {overviewChart.Categories.Last()}):");
            foreach (var series in overviewChart.Series)
                sb.AppendLine($"  - {series.Name}: {string.Join(", ", series.Data.Take(3))}...");
        }
        var agingChart = await _financeService.GetArApAgingChartAsync(from, to);
        if (agingChart.Categories.Any())
        {
            sb.AppendLine("Công nợ phải thu theo kỳ hạn:");
            for (int i = 0; i < agingChart.Categories.Count && i < agingChart.Series[0].Data.Count; i++)
                sb.AppendLine($"  - {agingChart.Categories[i]}: {agingChart.Series[0].Data[i]:N0}");
        }
        return sb.ToString();
    }

    // ==================== MARKETING ====================

    private async Task<string> BuildMarketingKpiSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var reach = await _marketingService.GetTotalReachAsync(from, to);
        sb.AppendLine($"- Tổng Reach: {reach.FormattedValue} ({reach.GrowthPercent:+0;-0;0}%)");
        var engagement = await _marketingService.GetEngagementRateAsync(from, to);
        sb.AppendLine($"- Tỷ lệ Engagement: {engagement.FormattedValue}");
        var leads = await _marketingService.GetNewLeadsAsync(from, to);
        sb.AppendLine($"- Leads mới: {leads.FormattedValue} ({leads.GrowthPercent:+0;-0;0}%)");
        var conversions = await _marketingService.GetConversionsAsync(from, to);
        sb.AppendLine($"- Conversions: {conversions.FormattedValue} ({conversions.GrowthPercent:+0;-0;0}%)");
        var cpl = await _marketingService.GetCplAsync(from, to);
        sb.AppendLine($"- Chi phí mỗi Lead (CPL): {cpl.FormattedValue}");
        var roas = await _marketingService.GetRoasAsync(from, to);
        sb.AppendLine($"- ROAS: {roas.FormattedValue}");
        var roi = await _marketingService.GetRoiAsync(from, to);
        sb.AppendLine($"- ROI: {roi.FormattedValue}");
        return sb.ToString();
    }

    private async Task<string> BuildMarketingTopItemsSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var activeCampaigns = await _marketingService.GetActiveCampaignsAsync(5, from, to);
        if (activeCampaigns.Any())
        {
            sb.AppendLine("Top 5 chiến dịch hiệu quả:");
            foreach (var c in activeCampaigns) sb.AppendLine($"  - {c.Name}: {c.FormattedValue}");
        }
        var budgetAlloc = await _marketingService.GetBudgetAllocationAsync(from, to);
        if (budgetAlloc.Any())
        {
            sb.AppendLine("Phân bổ ngân sách:");
            foreach (var b in budgetAlloc) sb.AppendLine($"  - {b.Name}: {b.FormattedValue}");
        }
        return sb.ToString();
    }

    private async Task<string> BuildMarketingRecentSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var funnel = await _marketingService.GetMarketingFunnelAsync(from, to);
        if (funnel.Any())
        {
            sb.AppendLine("Marketing Funnel:");
            foreach (var f in funnel) sb.AppendLine($"  - {f.Stage}: {f.Count} ({f.ConversionRate}% chuyển đổi)");
        }
        var social = await _marketingService.GetSocialMediaPerformanceAsync(from, to);
        if (social.Any())
        {
            sb.AppendLine("Hiệu suất Social Media:");
            foreach (var s in social.Take(5)) sb.AppendLine($"  - {s.Column1}");
        }
        return sb.ToString();
    }

    private async Task<string> BuildMarketingChartSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var perfChart = await _marketingService.GetCampaignPerformanceChartAsync(from, to);
        if (perfChart.Categories.Any())
        {
            sb.AppendLine($"Xu hướng chiến dịch ({perfChart.Categories.First()} - {perfChart.Categories.Last()}):");
            foreach (var series in perfChart.Series)
                sb.AppendLine($"  - {series.Name}: {string.Join(", ", series.Data.Take(3))}...");
        }
        var spendVsRev = await _marketingService.GetSpendVsRevenueChartAsync(from, to);
        if (spendVsRev.Categories.Any())
        {
            sb.AppendLine("Chi phí vs Doanh thu:");
            for (int i = 0; i < spendVsRev.Categories.Count && i < spendVsRev.Series[0].Data.Count; i++)
                sb.AppendLine($"  - {spendVsRev.Categories[i]}: Chi phí {spendVsRev.Series[0].Data[i]:N0}, Doanh thu {spendVsRev.Series[1].Data[i]:N0}");
        }
        return sb.ToString();
    }

    // ==================== INVENTORY ====================

    private async Task<string> BuildInventoryKpiSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var totalItems = await _inventoryService.GetTotalItemsAsync(from, to);
        sb.AppendLine($"- Tổng sản phẩm: {totalItems.FormattedValue}");
        var stockValue = await _inventoryService.GetStockValueAsync(from, to);
        sb.AppendLine($"- Giá trị tồn kho: {stockValue.FormattedValue}");
        var inbound = await _inventoryService.GetInboundOrdersAsync(from, to);
        sb.AppendLine($"- Đơn nhập kho: {inbound.FormattedValue}");
        var outbound = await _inventoryService.GetOutboundOrdersAsync(from, to);
        sb.AppendLine($"- Đơn xuất kho: {outbound.FormattedValue}");
        var lowStock = await _inventoryService.GetLowStockCountAsync(from, to);
        sb.AppendLine($"- Sản phẩm dưới điểm đặt: {lowStock.FormattedValue}");
        var utilization = await _inventoryService.GetStockUtilizationAsync(from, to);
        sb.AppendLine($"- Tỷ lệ sử dụng kho: {utilization.FormattedValue}");
        var turnover = await _inventoryService.GetInventoryTurnoverAsync(from, to);
        sb.AppendLine($"- Vòng quay tồn kho: {turnover.FormattedValue}");
        var fillRate = await _inventoryService.GetFillRateAsync(from, to);
        sb.AppendLine($"- Tỷ lệ lấp đầy: {fillRate.FormattedValue}");
        return sb.ToString();
    }

    private async Task<string> BuildInventoryTopItemsSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var lowStockItems = await _inventoryService.GetLowStockItemsAsync(5, from, to);
        if (lowStockItems.Any())
        {
            sb.AppendLine("Top 5 sản phẩm sắp hết:");
            foreach (var i in lowStockItems) sb.AppendLine($"  - {i.Name}: {i.FormattedValue}");
        }
        var warehouseStatus = await _inventoryService.GetWarehouseStatusAsync(from, to);
        if (warehouseStatus.Any())
        {
            sb.AppendLine("Tình trạng kho:");
            foreach (var w in warehouseStatus) sb.AppendLine($"  - {w.Name}: {w.FormattedValue}");
        }
        var topCats = await _inventoryService.GetTopCategoriesAsync(5, from, to);
        if (topCats.Any())
        {
            sb.AppendLine("Top danh mục:");
            foreach (var c in topCats) sb.AppendLine($"  - {c.Name}: {c.FormattedValue}");
        }
        return sb.ToString();
    }

    private async Task<string> BuildInventoryRecentSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var abc = await _inventoryService.GetAbcAnalysisAsync(5, from, to);
        if (abc.Any())
        {
            sb.AppendLine("ABC Analysis (Top sản phẩm quan trọng):");
            foreach (var a in abc) sb.AppendLine($"  - {a.Name}: {a.FormattedValue}");
        }
        return sb.ToString();
    }

    private async Task<string> BuildInventoryChartSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var movement = await _inventoryService.GetStockMovementChartAsync(from, to);
        if (movement.Categories.Any())
        {
            sb.AppendLine($"Xu hướng nhập/xuất kho ({movement.Categories.First()} - {movement.Categories.Last()}):");
            foreach (var series in movement.Series)
                sb.AppendLine($"  - {series.Name}: {string.Join(", ", series.Data.Take(3))}...");
        }
        var byCategory = await _inventoryService.GetStockByCategoryChartAsync(from, to);
        if (byCategory.Categories.Any())
        {
            sb.AppendLine("Tồn kho theo danh mục:");
            for (int i = 0; i < byCategory.Categories.Count && i < byCategory.Series[0].Data.Count; i++)
                sb.AppendLine($"  - {byCategory.Categories[i]}: {byCategory.Series[0].Data[i]:N0}");
        }
        return sb.ToString();
    }

    // ==================== HUMAN RESOURCES ====================

    private async Task<string> BuildHrKpiSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var totalEmp = await _hrService.GetTotalEmployeesAsync(from, to);
        sb.AppendLine($"- Tổng nhân sự: {totalEmp.FormattedValue}");
        var deptCount = await _hrService.GetDepartmentsCountAsync(from, to);
        sb.AppendLine($"- Số phòng ban: {deptCount.FormattedValue}");
        var newHires = await _hrService.GetNewHiresAsync(from, to);
        sb.AppendLine($"- Nhân viên mới: {newHires.FormattedValue} ({newHires.GrowthPercent:+0;-0;0}%)");
        var openPos = await _hrService.GetOpenPositionsAsync(from, to);
        sb.AppendLine($"- Vị trí đang tuyển: {openPos.FormattedValue}");
        var retention = await _hrService.GetRetentionRateAsync(from, to);
        sb.AppendLine($"- Tỷ lệ giữ chân: {retention.FormattedValue}");
        var turnover = await _hrService.GetTurnoverRateAsync(from, to);
        sb.AppendLine($"- Tỷ lệ nghỉ việc: {turnover.FormattedValue}");
        var pendingLeave = await _hrService.GetPendingLeaveRequestsAsync(from, to);
        sb.AppendLine($"- Đơn nghỉ phép chờ duyệt: {pendingLeave.FormattedValue}");
        return sb.ToString();
    }

    private async Task<string> BuildHrTopItemsSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var deptDist = await _hrService.GetDepartmentDistributionAsync(from, to);
        if (deptDist.Any())
        {
            sb.AppendLine("Phân bổ nhân sự theo phòng ban:");
            foreach (var d in deptDist) sb.AppendLine($"  - {d.Name}: {d.FormattedValue}");
        }
        var jobOpenings = await _hrService.GetJobOpeningsAsync(from, to);
        if (jobOpenings.Any())
        {
            sb.AppendLine("Vị trí đang tuyển:");
            foreach (var j in jobOpenings) sb.AppendLine($"  - {j.Name}: {j.FormattedValue}");
        }
        return sb.ToString();
    }

    private async Task<string> BuildHrRecentSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var recentHires = await _hrService.GetRecentHiresAsync(5, from, to);
        if (recentHires.Any())
        {
            sb.AppendLine("5 nhân viên mới gần nhất:");
            foreach (var h in recentHires) sb.AppendLine($"  - {h.Column1}");
        }
        var leaveReqs = await _hrService.GetLeaveRequestsAsync(5, from, to);
        if (leaveReqs.Any())
        {
            sb.AppendLine("Đơn nghỉ phép gần đây:");
            foreach (var l in leaveReqs) sb.AppendLine($"  - {l.Column1}");
        }
        return sb.ToString();
    }

    private async Task<string> BuildHrChartSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var overview = await _hrService.GetWorkforceOverviewChartAsync(from, to);
        if (overview.Categories.Any())
        {
            sb.AppendLine($"Xu hướng nhân sự ({overview.Categories.First()} - {overview.Categories.Last()}):");
            foreach (var series in overview.Series)
                sb.AppendLine($"  - {series.Name}: {string.Join(", ", series.Data.Take(3))}...");
        }
        var deptDistChart = await _hrService.GetDepartmentDistributionChartAsync(from, to);
        if (deptDistChart.Categories.Any())
        {
            sb.AppendLine("Phân bổ theo phòng ban:");
            for (int i = 0; i < deptDistChart.Categories.Count && i < deptDistChart.Series[0].Data.Count; i++)
                sb.AppendLine($"  - {deptDistChart.Categories[i]}: {deptDistChart.Series[0].Data[i]:N0}");
        }
        return sb.ToString();
    }

    // ==================== CUSTOMER SERVICE ====================

    private async Task<string> BuildCsKpiSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var totalTickets = await _csService.GetTotalTicketsAsync(from, to);
        sb.AppendLine($"- Tổng Tickets: {totalTickets.FormattedValue} ({totalTickets.GrowthPercent:+0;-0;0}%)");
        var satisfaction = await _csService.GetSatisfactionAsync(from, to);
        sb.AppendLine($"- Mức độ hài lòng (CSAT): {satisfaction.FormattedValue}");
        var resolved = await _csService.GetResolvedTicketsAsync(from, to);
        sb.AppendLine($"- Đã giải quyết: {resolved.FormattedValue} ({resolved.GrowthPercent:+0;-0;0}%)");
        var pending = await _csService.GetPendingTicketsAsync(from, to);
        sb.AppendLine($"- Đang chờ: {pending.FormattedValue}");
        var firstResponse = await _csService.GetFirstResponseRateAsync(from, to);
        sb.AppendLine($"- Tỷ lệ phản hồi lần đầu: {firstResponse.FormattedValue}");
        var avgTime = await _csService.GetAvgResolutionTimeAsync(from, to);
        sb.AppendLine($"- Thời gian giải quyết TB: {avgTime.FormattedValue}");
        var open = await _csService.GetOpenTicketsAsync(from, to);
        sb.AppendLine($"- Tickets đang mở: {open.FormattedValue}");
        return sb.ToString();
    }

    private async Task<string> BuildCsTopItemsSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var topAgents = await _csService.GetTopAgentsAsync(5, from, to);
        if (topAgents.Any())
        {
            sb.AppendLine("Top 5 nhân viên CS xuất sắc:");
            foreach (var a in topAgents) sb.AppendLine($"  - {a.Name}: {a.FormattedValue}");
        }
        var rootCause = await _csService.GetRootCauseParetoAsync(5, from, to);
        if (rootCause.Any())
        {
            sb.AppendLine("Top nguyên nhân khiếu nại:");
            foreach (var r in rootCause) sb.AppendLine($"  - {r.Name}: {r.FormattedValue}");
        }
        return sb.ToString();
    }

    private async Task<string> BuildCsRecentSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var recentTickets = await _csService.GetRecentTicketsAsync(5, from, to);
        if (recentTickets.Any())
        {
            sb.AppendLine("5 tickets gần nhất:");
            foreach (var t in recentTickets) sb.AppendLine($"  - {t.Column1}");
        }
        var funnel = await _csService.GetTicketFunnelAsync(from, to);
        if (funnel.Any())
        {
            sb.AppendLine("Ticket Funnel:");
            foreach (var f in funnel) sb.AppendLine($"  - {f.Stage}: {f.Count} ({f.ConversionRate}% chuyển đổi)");
        }
        return sb.ToString();
    }

    private async Task<string> BuildCsChartSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var overview = await _csService.GetSupportOverviewChartAsync(from, to);
        if (overview.Categories.Any())
        {
            sb.AppendLine($"Xu hướng tickets ({overview.Categories.First()} - {overview.Categories.Last()}):");
            foreach (var series in overview.Series)
                sb.AppendLine($"  - {series.Name}: {string.Join(", ", series.Data.Take(3))}...");
        }
        var channelMix = await _csService.GetChannelMixChartAsync(from, to);
        if (channelMix.Categories.Any())
        {
            sb.AppendLine("Tickets theo kênh:");
            for (int i = 0; i < channelMix.Categories.Count && i < channelMix.Series[0].Data.Count; i++)
                sb.AppendLine($"  - {channelMix.Categories[i]}: {channelMix.Series[0].Data[i]:N0}");
        }
        return sb.ToString();
    }

    // ==================== EXECUTIVE ====================

    private async Task<string> BuildExecutiveKpiSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var revenue = await _executiveService.GetTotalRevenueAsync(from, to);
        sb.AppendLine($"- Tổng doanh thu: {revenue.FormattedValue} ({revenue.GrowthPercent:+0;-0;0}%)");
        var expenses = await _executiveService.GetTotalExpensesAsync(from, to);
        sb.AppendLine($"- Tổng chi phí: {expenses.FormattedValue} ({expenses.GrowthPercent:+0;-0;0}%)");
        var netProfit = await _executiveService.GetNetProfitAsync(from, to);
        sb.AppendLine($"- Lợi nhuận ròng: {netProfit.FormattedValue} ({netProfit.GrowthPercent:+0;-0;0}%)");
        var employees = await _executiveService.GetTotalEmployeesAsync(from, to);
        sb.AppendLine($"- Tổng nhân sự: {employees.FormattedValue}");
        var growth = await _executiveService.GetCompanyGrowthAsync(from, to);
        sb.AppendLine($"- Tăng trưởng công ty: {growth.FormattedValue}");
        return sb.ToString();
    }

    private async Task<string> BuildExecutiveTopItemsSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var deptPerf = await _executiveService.GetDepartmentPerformanceAsync(from, to);
        if (deptPerf.Any())
        {
            sb.AppendLine("Hiệu suất phòng ban:");
            foreach (var d in deptPerf) sb.AppendLine($"  - {d.DepartmentName}: {d.PerformancePercent}%");
        }
        var topProducts = await _executiveService.GetTopProductsAsync(5, from, to);
        if (topProducts.Any())
        {
            sb.AppendLine("Top 5 sản phẩm:");
            foreach (var p in topProducts) sb.AppendLine($"  - {p.Name}: {p.FormattedValue}");
        }
        var topCustomers = await _executiveService.GetTopCustomersAsync(5, from, to);
        if (topCustomers.Any())
        {
            sb.AppendLine("Top 5 khách hàng:");
            foreach (var c in topCustomers) sb.AppendLine($"  - {c.Name}: {c.FormattedValue}");
        }
        return sb.ToString();
    }

    private async Task<string> BuildExecutiveRecentSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var activities = await _executiveService.GetRecentActivitiesAsync(5);
        if (activities.Any())
        {
            sb.AppendLine("5 hoạt động gần nhất:");
            foreach (var a in activities) sb.AppendLine($"  - [{a.Category}] {a.Description}");
        }
        var alerts = await _executiveService.GetAlertsAsync(from, to);
        if (alerts.Any())
        {
            sb.AppendLine("Cảnh báo:");
            foreach (var a in alerts) sb.AppendLine($"  - [{a.Severity}] {a.Message}");
        }
        return sb.ToString();
    }

    private async Task<string> BuildExecutiveChartSummaryAsync(DateTime from, DateTime to)
    {
        var sb = new StringBuilder();
        var companyPerf = await _executiveService.GetCompanyPerformanceChartAsync(from, to);
        if (companyPerf.Categories.Any())
        {
            sb.AppendLine($"Xu hướng công ty ({companyPerf.Categories.First()} - {companyPerf.Categories.Last()}):");
            foreach (var series in companyPerf.Series)
                sb.AppendLine($"  - {series.Name}: {string.Join(", ", series.Data.Take(3))}...");
        }
        var deptPerfChart = await _executiveService.GetDepartmentPerformanceAsync(from, to);
        if (deptPerfChart.Any())
        {
            sb.AppendLine("Hiệu suất phòng ban:");
            foreach (var d in deptPerfChart)
                sb.AppendLine($"  - {d.DepartmentName}: {d.PerformancePercent}%");
        }
        return sb.ToString();
    }
}
