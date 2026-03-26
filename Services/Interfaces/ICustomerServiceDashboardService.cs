using Dashboard.Models.ViewModels;

namespace Dashboard.Services.Interfaces;

public interface ICustomerServiceDashboardService
{
    Task<CustomerServiceDashboardViewModel> GetDashboardDataAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetTotalTicketsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetSatisfactionAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetResolvedTicketsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetPendingTicketsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetFirstResponseRateAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetAvgResolutionTimeAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetOpenTicketsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetSupportOverviewChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetTicketVolumeTrendChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetTicketByCategoryChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetChannelMixChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<TopNItemDto>> GetTicketCategoriesAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<TableRowDto>> GetRecentTicketsAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<TopNItemDto>> GetTopAgentsAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<FunnelStageDto>> GetTicketFunnelAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<TopNItemDto>> GetRootCauseParetoAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null);
    Task<HeatmapDto> GetChurnRiskHeatmapAsync(DateTime? fromDate = null, DateTime? toDate = null);
}
