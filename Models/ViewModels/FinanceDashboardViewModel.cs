using Dashboard.Models.ViewModels;

namespace Dashboard.Models.ViewModels;

public class FinanceDashboardViewModel
{
    // KPI Cards
    public KpiCardDto TotalIncome { get; set; } = new();
    public KpiCardDto TotalExpenses { get; set; } = new();
    public KpiCardDto NetProfit { get; set; } = new();
    public KpiCardDto CashFlow { get; set; } = new();
    public KpiCardDto ProfitMargin { get; set; } = new();
    public KpiCardDto Dso { get; set; } = new();
    public KpiCardDto Dpo { get; set; } = new();
    public KpiCardDto ArBalance { get; set; } = new();

    // Charts
    public ChartDataDto FinancialOverviewChart { get; set; } = new();
    public ChartDataDto ExpenseBreakdownChart { get; set; } = new();
    public ChartDataDto CashflowTrendChart { get; set; } = new();
    public ChartDataDto ArApAgingChart { get; set; } = new();
    public ChartDataDto ProfitWaterfallChart { get; set; } = new();
    public ChartDataDto CashflowTrendAreaChart { get; set; } = new();
    public ChartDataDto DsoDpoTrendChart { get; set; } = new();
    public ChartDataDto CollectionsPerformanceChart { get; set; } = new();
    public ChartDataDto ExpenseTreemapChart { get; set; } = new();
    public ChartDataDto UnitEconomicsChart { get; set; } = new();
    public ChartDataDto CustomerProfitScatterChart { get; set; } = new();
    public ChartDataDto ArApAgingStackedChart { get; set; } = new();

    // Tables/Lists
    public List<TopNItemDto> ExpenseBreakdown { get; set; } = new();
    public List<TableRowDto> RecentTransactions { get; set; } = new();
    public List<TopNItemDto> MonthlyBudgetStatus { get; set; } = new();

    // Additional
    public int ProfitMarginPercent { get; set; }
}
