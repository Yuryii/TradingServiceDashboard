using Dashboard.Models.ViewModels;

namespace Dashboard.Services.Interfaces;

public interface IExecutiveDashboardService
{
    Task<ExecutiveDashboardViewModel> GetDashboardDataAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetTotalRevenueAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetTotalExpensesAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetNetProfitAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetTotalEmployeesAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetCompanyGrowthAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetCompanyPerformanceChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetProfileReportChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetRevenueByChannelChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<DepartmentPerformanceDto>> GetDepartmentPerformanceAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<ActivityDto>> GetRecentActivitiesAsync(int count = 10);
    Task<List<AlertDto>> GetAlertsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<TopNItemDto>> GetTopProductsAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<TopNItemDto>> GetTopCustomersAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<TopNItemDto>> GetTopBranchesAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<FunnelStageDto>> GetSalesFunnelAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<HeatmapDto> GetChannelRegionHeatmapAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<BulletChartDto>> GetActualVsPlanAsync(DateTime? fromDate = null, DateTime? toDate = null);
}
