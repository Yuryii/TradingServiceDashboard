using Dashboard.Models.ViewModels;

namespace Dashboard.Models.ViewModels;

public class ExecutiveDashboardViewModel
{
    // KPI Cards
    public KpiCardDto TotalRevenue { get; set; } = new();
    public KpiCardDto TotalExpenses { get; set; } = new();
    public KpiCardDto NetProfit { get; set; } = new();
    public KpiCardDto TotalEmployees { get; set; } = new();
    public KpiCardDto CompanyGrowth { get; set; } = new();
    public KpiCardDto GrossMargin { get; set; } = new();
    public KpiCardDto LastYearRevenue { get; set; } = new();
    public KpiCardDto CurrentYearRevenue { get; set; } = new();
    public KpiCardDto CashFlow { get; set; } = new();
    public KpiCardDto YearlySummary { get; set; } = new();

    // Charts
    public ChartDataDto CompanyPerformanceChart { get; set; } = new();
    public ChartDataDto ProfileReportChart { get; set; } = new();
    public ChartDataDto RevenueByChannelChart { get; set; } = new();
    public ChartDataDto RevenueProfitAreaChart { get; set; } = new();
    public ChartDataDto BulletPlanChart { get; set; } = new();
    public ChartDataDto ProfitBridgeChart { get; set; } = new();
    public ChartDataDto TopProductsBarChart { get; set; } = new();
    public ChartDataDto TopCustomersBarChart { get; set; } = new();
    public ChartDataDto SalesFunnelChart { get; set; } = new();
    public HeatmapDto ChannelRegionHeatmap { get; set; } = new();

    // Additional charts
    public ChartDataDto MonthlyTargetVsActualChart { get; set; } = new();
    public ChartDataDto RevenueByBranchChart { get; set; } = new();

    // KPI Cards (additional)
    public KpiCardDto Ebitda { get; set; } = new();
    public KpiCardDto Dio { get; set; } = new();
    public KpiCardDto Dso { get; set; } = new();
    public KpiCardDto NpsScore { get; set; } = new();

    // Tables/Lists
    public List<DepartmentPerformanceDto> DepartmentPerformance { get; set; } = new();
    public List<ActivityDto> RecentActivities { get; set; } = new();

    // Alerts
    public List<AlertDto> Alerts { get; set; } = new();

    // Additional
    public int CompanyGrowthPercent { get; set; }
}
