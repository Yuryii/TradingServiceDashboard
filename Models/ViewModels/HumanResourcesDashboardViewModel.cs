using Dashboard.Models.ViewModels;

namespace Dashboard.Models.ViewModels;

public class HumanResourcesDashboardViewModel
{
    // KPI Cards
    public KpiCardDto TotalEmployees { get; set; } = new();
    public KpiCardDto Departments { get; set; } = new();
    public KpiCardDto NewHires { get; set; } = new();
    public KpiCardDto OpenPositions { get; set; } = new();
    public KpiCardDto RetentionRate { get; set; } = new();
    public KpiCardDto TurnoverRate { get; set; } = new();
    public KpiCardDto PendingLeaveRequests { get; set; } = new();
    public KpiCardDto AvgSalary { get; set; } = new();

    // Charts
    public ChartDataDto WorkforceOverviewChart { get; set; } = new();
    public ChartDataDto HeadcountTrendChart { get; set; } = new();
    public ChartDataDto DepartmentDistributionChart { get; set; } = new();
    public ChartDataDto CompensationByDeptChart { get; set; } = new();
    public ChartDataDto HeadcountTrendAreaChart { get; set; } = new();
    public ChartDataDto HeadcountStackedChart { get; set; } = new();
    public ChartDataDto TurnoverTrendChart { get; set; } = new();
    public ChartDataDto RecruitmentFunnelChart { get; set; } = new();
    public ChartDataDto RecruitmentMetricsChart { get; set; } = new();
    public ChartDataDto SalaryDistributionChart { get; set; } = new();
    public ChartDataDto TrainingChart { get; set; } = new();
    public ChartDataDto EngagementTrendChart { get; set; } = new();
    public HeatmapDto AttritionHeatmap { get; set; } = new();

    // Tables/Lists
    public List<TopNItemDto> DepartmentDistribution { get; set; } = new();
    public List<TableRowDto> RecentHires { get; set; } = new();
    public List<TableRowDto> LeaveRequests { get; set; } = new();
    public List<TopNItemDto> JobOpenings { get; set; } = new();

    // Additional
    public int RetentionRatePercent { get; set; }
}
