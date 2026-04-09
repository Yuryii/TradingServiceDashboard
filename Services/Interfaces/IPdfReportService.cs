namespace Dashboard.Services.Interfaces;

public interface IPdfReportService
{
    Task<byte[]> GenerateExecutivePdfAsync(Models.ViewModels.ExecutiveDashboardViewModel vm, DateTime? from, DateTime? to);
    Task<byte[]> GenerateSalesPdfAsync(Models.ViewModels.SalesDashboardViewModel vm, DateTime? from, DateTime? to);
    Task<byte[]> GenerateMarketingPdfAsync(Models.ViewModels.MarketingDashboardViewModel vm, DateTime? from, DateTime? to);
    Task<byte[]> GenerateInventoryPdfAsync(Models.ViewModels.InventoryDashboardViewModel vm, DateTime? from, DateTime? to);
    Task<byte[]> GenerateFinancePdfAsync(Models.ViewModels.FinanceDashboardViewModel vm, DateTime? from, DateTime? to);
    Task<byte[]> GenerateHrPdfAsync(Models.ViewModels.HumanResourcesDashboardViewModel vm, DateTime? from, DateTime? to);
    Task<byte[]> GenerateCustomerServicePdfAsync(Models.ViewModels.CustomerServiceDashboardViewModel vm, DateTime? from, DateTime? to);
}