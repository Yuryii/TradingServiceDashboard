using Dashboard.Models.ViewModels;

namespace Dashboard.Services.Interfaces;

public interface IMarketingDashboardService
{
    Task<MarketingDashboardViewModel> GetDashboardDataAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetTotalReachAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetEngagementRateAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetNewLeadsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetConversionsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetCplAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetRoasAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetRoiAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetCampaignPerformanceChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetChannelPerformanceChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetSpendVsRevenueChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<TopNItemDto>> GetActiveCampaignsAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<TableRowDto>> GetSocialMediaPerformanceAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<TopNItemDto>> GetBudgetAllocationAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<FunnelStageDto>> GetMarketingFunnelAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<ScatterPointDto>> GetCplVsConversionScatterAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<HeatmapDto> GetAdPerformanceByTimeHeatmapAsync(DateTime? fromDate = null, DateTime? toDate = null);
}
