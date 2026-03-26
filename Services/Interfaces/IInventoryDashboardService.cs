using Dashboard.Models.ViewModels;

namespace Dashboard.Services.Interfaces;

public interface IInventoryDashboardService
{
    Task<InventoryDashboardViewModel> GetDashboardDataAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetTotalItemsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetStockValueAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetInboundOrdersAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetOutboundOrdersAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetLowStockCountAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetStockUtilizationAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetInventoryTurnoverAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetFillRateAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetStockMovementChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetInventoryTrendChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetStockByCategoryChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetWarehouseUtilizationChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<TopNItemDto>> GetLowStockItemsAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<TopNItemDto>> GetWarehouseStatusAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<TopNItemDto>> GetTopCategoriesAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<TopNItemDto>> GetAbcAnalysisAsync(int count = 20, DateTime? fromDate = null, DateTime? toDate = null);
    Task<HeatmapDto> GetInventoryAccuracyHeatmapAsync(DateTime? fromDate = null, DateTime? toDate = null);
}
