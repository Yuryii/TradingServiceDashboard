using Dashboard.Models.ViewModels;

namespace Dashboard.Models.ViewModels;

public class InventoryDashboardViewModel
{
    // KPI Cards
    public KpiCardDto TotalItems { get; set; } = new();
    public KpiCardDto StockValue { get; set; } = new();
    public KpiCardDto InboundOrders { get; set; } = new();
    public KpiCardDto OutboundOrders { get; set; } = new();
    public KpiCardDto LowStockCount { get; set; } = new();
    public KpiCardDto StockUtilization { get; set; } = new();
    public KpiCardDto InventoryTurnover { get; set; } = new();
    public KpiCardDto FillRate { get; set; } = new();

    // Charts
    public ChartDataDto StockMovementChart { get; set; } = new();
    public ChartDataDto InventoryTrendChart { get; set; } = new();
    public ChartDataDto StockByCategoryChart { get; set; } = new();
    public ChartDataDto WarehouseUtilizationChart { get; set; } = new();
    public ChartDataDto InventoryTrendAreaChart { get; set; } = new();
    public ChartDataDto AbcAnalysisChart { get; set; } = new();
    public ChartDataDto DaysOfInventoryChart { get; set; } = new();
    public ChartDataDto StockoutTrendChart { get; set; } = new();
    public ChartDataDto ReorderPointChart { get; set; } = new();
    public ChartDataDto AgingInventoryChart { get; set; } = new();
    public ChartDataDto InboundOutboundChart { get; set; } = new();
    public ChartDataDto WarehouseProductivityChart { get; set; } = new();
    public HeatmapDto InventoryAccuracyHeatmap { get; set; } = new();

    // Tables/Lists
    public List<TopNItemDto> LowStockItems { get; set; } = new();
    public List<TopNItemDto> WarehouseStatus { get; set; } = new();
    public List<TopNItemDto> TopCategories { get; set; } = new();
    public List<TableRowDto> StockAlerts { get; set; } = new();

    // Additional
    public int StockUtilizationPercent { get; set; }
}
