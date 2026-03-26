using Dashboard.Models.ViewModels;

namespace Dashboard.Services.Interfaces;

public interface IFinanceDashboardService
{
    Task<FinanceDashboardViewModel> GetDashboardDataAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetTotalIncomeAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetTotalExpensesAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetNetProfitAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetCashFlowAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetProfitMarginAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetDsoAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<KpiCardDto> GetDpoAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetFinancialOverviewChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetExpenseBreakdownChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetCashflowTrendChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ChartDataDto> GetArApAgingChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<TopNItemDto>> GetExpenseBreakdownAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<TableRowDto>> GetRecentTransactionsAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<TopNItemDto>> GetMonthlyBudgetStatusAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<WaterfallItemDto>> GetProfitWaterfallAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<ScatterPointDto>> GetRevenueVsProfitScatterAsync(DateTime? fromDate = null, DateTime? toDate = null);
}
