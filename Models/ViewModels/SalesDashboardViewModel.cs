using Dashboard.Models.ViewModels;

namespace Dashboard.Models.ViewModels;

public class SalesDashboardViewModel
{
    // KPI Cards
    public KpiCardDto TotalSales { get; set; } = new();
    public KpiCardDto TotalOrders { get; set; } = new();
    public KpiCardDto NewCustomers { get; set; } = new();
    public KpiCardDto PendingDeals { get; set; } = new();
    public KpiCardDto WinRate { get; set; } = new();
    public KpiCardDto AverageOrderValue { get; set; } = new();
    public KpiCardDto GrossMargin { get; set; } = new();
    public KpiCardDto SalesTargetAchievement { get; set; } = new();

    // Charts
    public ChartDataDto SalesOverviewChart { get; set; } = new();
    public ChartDataDto RevenueByChannelChart { get; set; } = new();
    public ChartDataDto PipelineByStageChart { get; set; } = new();
    public ChartDataDto RevenueBySalespersonChart { get; set; } = new();
    public ChartDataDto PipelineFunnelChart { get; set; } = new();
    public ChartDataDto ForecastChart { get; set; } = new();
    public ChartDataDto SalespersonBarChart { get; set; } = new();
    public ChartDataDto OrderMarginScatterChart { get; set; } = new();
    public ChartDataDto ParetoChart { get; set; } = new();
    public ChartDataDto SalesCycleBoxPlotChart { get; set; } = new();
    public ChartDataDto RevenueByChannelBarChart { get; set; } = new();
    public HeatmapDto SalespersonProductHeatmap { get; set; } = new();

    // Tables/Lists
    public List<TopNItemDto> TopProducts { get; set; } = new();
    public List<TableRowDto> RecentOrders { get; set; } = new();
    public List<TopNItemDto> TopSalespersons { get; set; } = new();
    public List<FunnelStageDto> SalesFunnel { get; set; } = new();

    // Additional
    public int TargetAchievementPercent { get; set; }
    public decimal PipelineValue { get; set; }
}
