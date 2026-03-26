using Dashboard.Models.ViewModels;

namespace Dashboard.Services.Interfaces;

public interface ISalesDashboardService
{
    Task<SalesDashboardViewModel> GetDashboardDataAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetTotalSalesAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetTotalOrdersAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetNewCustomersAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetPendingDealsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetWinRateAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetAverageOrderValueAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetGrossMarginAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetSalesOverviewChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetRevenueBySalespersonChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetRevenueByChannelChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetPipelineByStageChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetForecastChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetOrderValueVsMarginScatterChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetParetoChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetSalesCycleBoxPlotChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<TopNItemDto>> GetTopProductsAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<TopNItemDto>> GetTopSalespersonsAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<TableRowDto>> GetRecentOrdersAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<FunnelStageDto>> GetSalesFunnelAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<ScatterPointDto>> GetOrderValueVsMarginScatterAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<TopNItemDto>> GetParetoByCustomerAsync(int count = 20, DateTime? fromDate = null, DateTime? toDate = null);
    Task<HeatmapDto> GetSalespersonProductHeatmapAsync(DateTime? fromDate = null, DateTime? toDate = null);
}
