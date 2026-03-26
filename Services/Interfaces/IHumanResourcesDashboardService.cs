using Dashboard.Models.ViewModels;

namespace Dashboard.Services.Interfaces;

public interface IHumanResourcesDashboardService
{
    Task<HumanResourcesDashboardViewModel> GetDashboardDataAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetTotalEmployeesAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetDepartmentsCountAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetNewHiresAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetOpenPositionsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetRetentionRateAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetTurnoverRateAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetPendingLeaveRequestsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetWorkforceOverviewChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetHeadcountTrendChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetDepartmentDistributionChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetCompensationByDeptChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<TopNItemDto>> GetDepartmentDistributionAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<TableRowDto>> GetRecentHiresAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<TableRowDto>> GetLeaveRequestsAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<TopNItemDto>> GetJobOpeningsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<HeatmapDto> GetAttritionHeatmapAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<FunnelStageDto>> GetRecruitmentFunnelAsync(DateTime? fromDate = null, DateTime? toDate = null);
}
