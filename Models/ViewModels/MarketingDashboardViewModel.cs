using Dashboard.Models.ViewModels;

namespace Dashboard.Models.ViewModels;

public class MarketingDashboardViewModel
{
    // KPI Cards
    public KpiCardDto TotalReach { get; set; } = new();
    public KpiCardDto EngagementRate { get; set; } = new();
    public KpiCardDto NewLeads { get; set; } = new();
    public KpiCardDto Conversions { get; set; } = new();
    public KpiCardDto Cpl { get; set; } = new();
    public KpiCardDto Roas { get; set; } = new();
    public KpiCardDto Roi { get; set; } = new();
    public KpiCardDto CampaignBudgetUtilization { get; set; } = new();

    // Charts
    public ChartDataDto CampaignPerformanceChart { get; set; } = new();
    public ChartDataDto ChannelPerformanceChart { get; set; } = new();
    public ChartDataDto SpendVsRevenueChart { get; set; } = new();
    public ChartDataDto LeadTrendChart { get; set; } = new();
    public ChartDataDto MarketingFunnelChart { get; set; } = new();
    public ChartDataDto ChannelSpendRevenueChart { get; set; } = new();
    public ChartDataDto CplVsConversionScatterChart { get; set; } = new();
    public ChartDataDto TimeSlotHeatmapChart { get; set; } = new();
    public ChartDataDto LandingPageChart { get; set; } = new();
    public ChartDataDto CohortRetentionChart { get; set; } = new();
    public HeatmapDto CohortRetentionHeatmap { get; set; } = new();
    public HeatmapDto TimeSlotHeatmap { get; set; } = new();

    // KPI Cards (additional)
    public KpiCardDto Cac { get; set; } = new();

    // Tables/Lists
    public List<TopNItemDto> ActiveCampaigns { get; set; } = new();
    public List<TableRowDto> SocialMediaPerformance { get; set; } = new();
    public List<TopNItemDto> BudgetAllocation { get; set; } = new();
    public List<FunnelStageDto> MarketingFunnel { get; set; } = new();

    // Additional
    public int RoiAchievementPercent { get; set; }
}
