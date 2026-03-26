using Dashboard.Models.ViewModels;

namespace Dashboard.Models.ViewModels;

public class CustomerServiceDashboardViewModel
{
    // KPI Cards
    public KpiCardDto TotalTickets { get; set; } = new();
    public KpiCardDto Satisfaction { get; set; } = new();
    public KpiCardDto ResolvedTickets { get; set; } = new();
    public KpiCardDto PendingTickets { get; set; } = new();
    public KpiCardDto FirstResponseRate { get; set; } = new();
    public KpiCardDto AvgResolutionTime { get; set; } = new();
    public KpiCardDto OpenTickets { get; set; } = new();
    public KpiCardDto AvgResponseTime { get; set; } = new();

    // Charts
    public ChartDataDto SupportOverviewChart { get; set; } = new();
    public ChartDataDto TicketVolumeTrendChart { get; set; } = new();
    public ChartDataDto TicketByCategoryChart { get; set; } = new();
    public ChartDataDto ChannelMixChart { get; set; } = new();
    public ChartDataDto TicketVolumeLineChart { get; set; } = new();
    public ChartDataDto SlaComplianceChart { get; set; } = new();
    public ChartDataDto ResponseTimeChart { get; set; } = new();
    public ChartDataDto BacklogAgingChart { get; set; } = new();
    public ChartDataDto ChannelMixStackedChart { get; set; } = new();
    public ChartDataDto CsatNpsTrendChart { get; set; } = new();
    public ChartDataDto RootCauseParetoChart { get; set; } = new();
    public HeatmapDto ChurnRiskHeatmap { get; set; } = new();

    // Tables/Lists
    public List<TopNItemDto> TicketCategories { get; set; } = new();
    public List<TableRowDto> RecentTickets { get; set; } = new();
    public List<TopNItemDto> TopAgents { get; set; } = new();
    public List<FunnelStageDto> TicketFunnel { get; set; } = new();

    // Additional
    public int FirstResponseRatePercent { get; set; }
    public decimal ResolutionRate { get; set; }
}
