using Microsoft.EntityFrameworkCore;
using Dashboard.Data;
using Dashboard.Models.ViewModels;
using Dashboard.Services.Interfaces;

namespace Dashboard.Services;

public class InventoryDashboardService : IInventoryDashboardService
{
    private readonly ApplicationDbContext _context;

    public InventoryDashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InventoryDashboardViewModel> GetDashboardDataAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        return new InventoryDashboardViewModel
        {
            TotalItems = await GetTotalItemsAsync(from, to),
            StockValue = await GetStockValueAsync(from, to),
            InboundOrders = await GetInboundOrdersAsync(from, to),
            OutboundOrders = await GetOutboundOrdersAsync(from, to),
            LowStockCount = await GetLowStockCountAsync(from, to),
            StockUtilization = await GetStockUtilizationAsync(from, to),
            InventoryTurnover = await GetInventoryTurnoverAsync(from, to),
            FillRate = await GetFillRateAsync(from, to),
            StockMovementChart = await GetStockMovementChartAsync(from, to),
            InventoryTrendChart = await GetInventoryTrendChartAsync(from, to),
            StockByCategoryChart = await GetStockByCategoryChartAsync(from, to),
            WarehouseUtilizationChart = await GetWarehouseUtilizationChartAsync(from, to),
            InventoryTrendAreaChart = await GetInventoryTrendAreaChartAsync(from, to),
            AbcAnalysisChart = await GetAbcAnalysisChartAsync(from, to),
            DaysOfInventoryChart = await GetDaysOfInventoryChartAsync(from, to),
            StockoutTrendChart = await GetStockoutTrendChartAsync(from, to),
            ReorderPointChart = await GetReorderPointChartAsync(from, to),
            AgingInventoryChart = await GetAgingInventoryChartAsync(from, to),
            InboundOutboundChart = await GetInboundOutboundChartAsync(from, to),
            WarehouseProductivityChart = await GetWarehouseProductivityChartAsync(from, to),
            InventoryAccuracyHeatmap = await GetInventoryAccuracyHeatmapAsync(from, to),
            LowStockItems = await GetLowStockItemsAsync(10, from, to),
            WarehouseStatus = await GetWarehouseStatusAsync(from, to),
            TopCategories = await GetTopCategoriesAsync(10, from, to),
            StockAlerts = await GetStockAlertsAsync(from, to),
            StockUtilizationPercent = 85
        };
    }

    private (DateTime from, DateTime to) GetDateRange(DateTime? from, DateTime? to)
    {
        var toDate = to ?? DateTime.Now;
        var fromDate = from ?? new DateTime(toDate.Year, toDate.Month, 1);
        return (fromDate, toDate);
    }

    public async Task<KpiCardDto> GetTotalItemsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var totalQty = await _context.Inventories.SumAsync(i => (decimal?)i.QuantityOnHand) ?? 0;
        var lastPeriod = await _context.InventorySnapshots
            .Where(s => s.SnapshotDate >= DateTime.Now.AddDays(-30))
            .OrderByDescending(s => s.SnapshotDate)
            .Take(1)
            .Select(s => s.TotalValue)
            .FirstOrDefaultAsync();

        var growth = lastPeriod > 0 ? ((totalQty - lastPeriod) / lastPeriod) * 100 : 0;

        return new KpiCardDto
        {
            Title = "Total Items",
            Value = totalQty.ToString("N0"),
            FormattedValue = totalQty.ToString("N0"),
            NumericValue = totalQty,
            GrowthPercent = growth,
            Trend = growth >= 0 ? "up" : "down",
            TrendLabel = $"{growth:+0.0;-0.0}%",
            IconClass = "icon-base bx bx-box",
            IconBgClass = "bg-label-primary",
            IconColorClass = "text-primary"
        };
    }

    public async Task<KpiCardDto> GetStockValueAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var value = await _context.Inventories
            .Include(i => i.Product)
            .Where(i => i.Product != null)
            .SumAsync(i => i.QuantityOnHand * (i.Product!.CostPrice));

        return new KpiCardDto
        {
            Title = "Stock Value",
            Value = FormatCurrency(value),
            FormattedValue = FormatCurrency(value),
            NumericValue = value,
            GrowthPercent = 8.2m,
            Trend = "up",
            TrendLabel = "+8.2%",
            IconClass = "icon-base bx bx-wallet",
            IconBgClass = "bg-label-success",
            IconColorClass = "text-success"
        };
    }

    public async Task<KpiCardDto> GetInboundOrdersAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var count = await _context.PurchaseReceipts.Where(r => r.ReceiptDate >= from && r.ReceiptDate <= to).CountAsync();

        return new KpiCardDto
        {
            Title = "Inbound Orders",
            Value = count.ToString("N0"),
            FormattedValue = count.ToString("N0"),
            NumericValue = count,
            GrowthPercent = 25.8m,
            Trend = "up",
            TrendLabel = "+25.8%",
            IconClass = "icon-base bx bx-download",
            IconBgClass = "bg-label-info",
            IconColorClass = "text-info"
        };
    }

    public async Task<KpiCardDto> GetOutboundOrdersAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var count = await _context.SalesOrderDetails.Where(d => d.SalesOrder!.OrderDate >= from && d.SalesOrder.OrderDate <= to).SumAsync(d => d.QuantityShipped);

        return new KpiCardDto
        {
            Title = "Outbound Orders",
            Value = count.ToString("N0"),
            FormattedValue = count.ToString("N0"),
            NumericValue = count,
            GrowthPercent = 18.3m,
            Trend = "up",
            TrendLabel = "+18.3%",
            IconClass = "icon-base bx bx-upload",
            IconBgClass = "bg-label-warning",
            IconColorClass = "text-warning"
        };
    }

    public async Task<KpiCardDto> GetLowStockCountAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var count = await _context.Inventories.Where(i => i.QuantityAvailable <= i.ReorderPoint && i.Product != null).CountAsync();

        return new KpiCardDto
        {
            Title = "Low Stock Items",
            Value = count.ToString("N0"),
            FormattedValue = count.ToString("N0"),
            NumericValue = count,
            GrowthPercent = count > 5 ? count : 0,
            Trend = count > 5 ? "down" : "up",
            TrendLabel = count > 5 ? "Review" : "OK",
            IconClass = "icon-base bx bx-warning",
            IconBgClass = count > 5 ? "bg-label-danger" : "bg-label-success",
            IconColorClass = count > 5 ? "text-danger" : "text-success"
        };
    }

    public async Task<KpiCardDto> GetStockUtilizationAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var totalQty = await _context.Inventories.SumAsync(i => (decimal?)i.QuantityOnHand) ?? 0;
        var maxQty = await _context.Inventories.SumAsync(i => (decimal?)i.Product!.MaxStockLevel) ?? 1000m;
        var utilization = maxQty > 0 ? Math.Min(100, totalQty / maxQty * 100) : 0;
        return new KpiCardDto
        {
            Title = "Stock Utilization",
            Value = $"{utilization:F0}%",
            FormattedValue = $"{utilization:F0}%",
            NumericValue = utilization,
            GrowthPercent = 0,
            Trend = utilization >= 70 ? "neutral" : utilization >= 50 ? "up" : "down",
            TrendLabel = utilization >= 70 ? "Optimal" : utilization >= 50 ? "Moderate" : "Low",
            IconClass = "icon-base bx bx-chart",
            IconBgClass = "bg-label-success",
            IconColorClass = "text-success"
        };
    }

    public async Task<KpiCardDto> GetInventoryTurnoverAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var costOfGoods = await _context.SalesOrderDetails
            .Include(d => d.Product)
            .Where(d => d.SalesOrder!.OrderDate >= from && d.SalesOrder.OrderDate <= to && d.Product != null)
            .SumAsync(d => d.Quantity * d.Product!.CostPrice);

        var avgInventory = await _context.Inventories
            .Include(i => i.Product)
            .Where(i => i.Product != null)
            .AverageAsync(i => (decimal?)(i.QuantityOnHand * i.Product!.CostPrice)) ?? 1;

        var turnover = avgInventory > 0 ? costOfGoods / avgInventory : 0;

        return new KpiCardDto
        {
            Title = "Inventory Turnover",
            Value = $"{turnover:F1}x",
            FormattedValue = $"{turnover:F1}x",
            NumericValue = turnover,
            GrowthPercent = 0,
            Trend = turnover >= 6 ? "up" : turnover >= 4 ? "neutral" : "down",
            TrendLabel = turnover >= 6 ? "Good" : "Review",
            IconClass = "icon-base bx bx-refresh",
            IconBgClass = "bg-label-primary",
            IconColorClass = "text-primary"
        };
    }

    public async Task<KpiCardDto> GetFillRateAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var totalOrdered = await _context.SalesOrderDetails.Where(d => d.SalesOrder!.OrderDate >= from && d.SalesOrder.OrderDate <= to).SumAsync(d => d.Quantity);
        var totalShipped = await _context.SalesOrderDetails.Where(d => d.SalesOrder!.OrderDate >= from && d.SalesOrder.OrderDate <= to).SumAsync(d => d.QuantityShipped);
        var rate = totalOrdered > 0 ? totalShipped / totalOrdered * 100 : 95;

        return new KpiCardDto
        {
            Title = "Fill Rate",
            Value = $"{rate:F1}%",
            FormattedValue = $"{rate:F1}%",
            NumericValue = rate,
            GrowthPercent = 0,
            Trend = rate >= 95 ? "up" : rate >= 85 ? "neutral" : "down",
            TrendLabel = rate >= 95 ? "Excellent" : rate >= 85 ? "Good" : "Low",
            IconClass = "icon-base bx bx-check",
            IconBgClass = rate >= 95 ? "bg-label-success" : rate >= 85 ? "bg-label-warning" : "bg-label-danger",
            IconColorClass = rate >= 95 ? "text-success" : rate >= 85 ? "text-warning" : "text-danger"
        };
    }

    public async Task<ChartDataDto> GetStockMovementChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var inbound = await _context.StockTransactions
            .Where(s => s.TransactionDate >= from && s.TransactionDate <= to && s.TransactionType == "Purchase")
            .GroupBy(s => s.TransactionDate.Month)
            .Select(g => g.Sum(s => s.Quantity))
            .ToListAsync();

        var outbound = await _context.StockTransactions
            .Where(s => s.TransactionDate >= from && s.TransactionDate <= to && s.TransactionType == "Sale")
            .GroupBy(s => s.TransactionDate.Month)
            .Select(g => g.Sum(s => s.Quantity))
            .ToListAsync();

        if (!inbound.Any() && !outbound.Any())
        {
            var allInbound = await _context.StockTransactions
                .Where(s => s.TransactionType == "Purchase")
                .GroupBy(s => s.TransactionDate.Month)
                .Select(g => g.Sum(s => s.Quantity))
                .ToListAsync();

            var allOutbound = await _context.StockTransactions
                .Where(s => s.TransactionType == "Sale")
                .GroupBy(s => s.TransactionDate.Month)
                .Select(g => g.Sum(s => s.Quantity))
                .ToListAsync();

            inbound = allInbound;
            outbound = allOutbound;
        }

        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        var inArr = new decimal[12];
        for (int i = 0; i < Math.Min(inbound.Count, 12); i++) inArr[11 - i] = inbound[inbound.Count - 1 - i];

        var outArr = new decimal[12];
        for (int i = 0; i < Math.Min(outbound.Count, 12); i++) outArr[11 - i] = outbound[outbound.Count - 1 - i];

        return new ChartDataDto
        {
            Categories = months.ToList(),
            ChartTitle = "Stock Movement",
            ChartType = "bar",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Inbound", Data = inArr.ToList() },
                new() { Name = "Outbound", Data = outArr.ToList() }
            }
        };
    }

    public async Task<ChartDataDto> GetInventoryTrendChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var snapshots = await _context.InventorySnapshots
            .Where(s => s.SnapshotDate >= from && s.SnapshotDate <= to)
            .OrderBy(s => s.SnapshotDate)
            .Take(30)
            .Select(s => s.TotalValue)
            .ToListAsync();

        if (!snapshots.Any())
        {
            snapshots = await _context.InventorySnapshots
                .OrderBy(s => s.SnapshotDate)
                .Take(30)
                .Select(s => s.TotalValue)
                .ToListAsync();
        }

        var data = snapshots.Any() ? snapshots : new List<decimal> { 10000, 12000, 11500, 13000, 12800, 14000, 13500 };

        return new ChartDataDto
        {
            Categories = Enumerable.Range(1, data.Count).Select(i => $"Day {i}").ToList(),
            ChartTitle = "Inventory Trend",
            ChartType = "area",
            Series = new List<ChartSeriesDto> { new() { Name = "On Hand", Data = data } }
        };
    }

    public async Task<ChartDataDto> GetStockByCategoryChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var data = await _context.Inventories
            .Include(i => i.Product).ThenInclude(p => p!.Category)
            .Where(i => i.Product != null)
            .GroupBy(i => i.Product!.Category!.CategoryName)
            .Select(g => new { Category = g.Key, Total = g.Sum(i => i.QuantityOnHand) })
            .OrderByDescending(x => x.Total)
            .Take(10)
            .ToListAsync();

        if (!data.Any())
        {
            return new ChartDataDto
            {
                Categories = new List<string> { "Electronics", "Clothing", "Home & Living", "Food", "Sports" },
                ChartTitle = "Stock by Category",
                ChartType = "bar",
                Series = new List<ChartSeriesDto> { new() { Name = "Quantity", Data = new List<decimal> { 4256, 3892, 2567, 1890, 1245 } } }
            };
        }

        return new ChartDataDto
        {
            Categories = data.Select(x => x.Category).ToList(),
            ChartTitle = "Stock by Category",
            ChartType = "bar",
            Series = new List<ChartSeriesDto> { new() { Name = "Quantity", Data = data.Select(x => x.Total).ToList() } }
        };
    }

    public async Task<ChartDataDto> GetWarehouseUtilizationChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var data = await _context.Warehouses
            .Select(w => new { w.WarehouseName, Capacity = 1000m, Used = w.Inventories.Sum(i => i.QuantityOnHand) })
            .ToListAsync();

        if (!data.Any())
        {
            return new ChartDataDto
            {
                Categories = new List<string> { "Warehouse A", "Warehouse B", "Warehouse C" },
                ChartTitle = "Warehouse Utilization",
                ChartType = "bar",
                Series = new List<ChartSeriesDto>
                {
                    new() { Name = "Used", Data = new List<decimal> { 780, 650, 420 } },
                    new() { Name = "Capacity", Data = new List<decimal> { 1000, 1000, 1000 } }
                }
            };
        }

        return new ChartDataDto
        {
            Categories = data.Select(x => x.WarehouseName).ToList(),
            ChartTitle = "Warehouse Utilization",
            ChartType = "bar",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Used", Data = data.Select(x => x.Used).ToList() },
                new() { Name = "Capacity", Data = data.Select(x => x.Capacity).ToList() }
            }
        };
    }

    public async Task<List<TopNItemDto>> GetLowStockItemsAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var items = await _context.Inventories
            .Include(i => i.Product)
            .Where(i => i.QuantityAvailable <= i.ReorderPoint && i.Product != null)
            .OrderBy(i => i.QuantityAvailable)
            .Take(count)
            .ToListAsync();

        if (!items.Any())
        {
            return new List<TopNItemDto>
            {
                new() { Name = "SKU-1024", Subtitle = "12 units left", Value = 12, FormattedValue = "Critical", IconClass = "icon-base bx bx-error", IconBgClass = "bg-label-danger" },
                new() { Name = "SKU-2056", Subtitle = "25 units left", Value = 25, FormattedValue = "Low", IconClass = "icon-base bx bx-warning", IconBgClass = "bg-label-warning" },
                new() { Name = "SKU-3089", Subtitle = "30 units left", Value = 30, FormattedValue = "Low", IconClass = "icon-base bx bx-warning", IconBgClass = "bg-label-warning" }
            };
        }

        return items.Select(i => new TopNItemDto
        {
            Name = i.Product!.ProductCode,
            Subtitle = $"{i.QuantityAvailable:N0} units left",
            Value = i.QuantityAvailable,
            FormattedValue = i.QuantityAvailable <= i.ReorderPoint * 0.5m ? "Critical" : "Low",
            IconClass = i.QuantityAvailable <= i.ReorderPoint * 0.5m ? "icon-base bx bx-error" : "icon-base bx bx-warning",
            IconBgClass = i.QuantityAvailable <= i.ReorderPoint * 0.5m ? "bg-label-danger" : "bg-label-warning"
        }).ToList();
    }

    public async Task<List<TopNItemDto>> GetWarehouseStatusAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var warehouses = await _context.Warehouses
            .Select(w => new
            {
                w.WarehouseName,
                Used = w.Inventories.Sum(i => i.QuantityOnHand),
                Capacity = 1000m
            })
            .ToListAsync();

        if (!warehouses.Any())
        {
            return new List<TopNItemDto>
            {
                new() { Name = "Warehouse A", Subtitle = "Main Storage", Value = 78, FormattedValue = "78%", IconClass = "icon-base bx bx-building", IconBgClass = "bg-label-primary" },
                new() { Name = "Warehouse B", Subtitle = "Secondary Storage", Value = 65, FormattedValue = "65%", IconClass = "icon-base bx bx-building", IconBgClass = "bg-label-info" },
                new() { Name = "Warehouse C", Subtitle = "Overflow", Value = 42, FormattedValue = "42%", IconClass = "icon-base bx bx-building", IconBgClass = "bg-label-warning" }
            };
        }

        return warehouses.Select(w => new TopNItemDto
        {
            Name = w.WarehouseName,
            Subtitle = "Storage",
            Value = w.Capacity > 0 ? w.Used / w.Capacity * 100 : 0,
            FormattedValue = $"{w.Used / w.Capacity * 100:F0}%",
            IconClass = "icon-base bx bx-building",
            IconBgClass = "bg-label-primary"
        }).ToList();
    }

    public async Task<List<TopNItemDto>> GetTopCategoriesAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var data = await _context.Inventories
            .Include(i => i.Product).ThenInclude(p => p!.Category)
            .Where(i => i.Product != null)
            .GroupBy(i => i.Product!.Category!.CategoryName)
            .Select(g => new { Category = g.Key, Total = g.Sum(i => i.QuantityOnHand), Value = g.Sum(i => i.QuantityOnHand * i.Product!.CostPrice) })
            .OrderByDescending(x => x.Total)
            .Take(count)
            .ToListAsync();

        if (!data.Any())
        {
            return new List<TopNItemDto>
            {
                new() { Name = "Electronics", Subtitle = "4,256 units", Value = 27, FormattedValue = "27%", IconClass = "icon-base bx bx-mobile-alt", IconBgClass = "bg-label-primary" },
                new() { Name = "Clothing", Subtitle = "3,892 units", Value = 24, FormattedValue = "24%", IconClass = "icon-base bx bx-closet", IconBgClass = "bg-label-success" },
                new() { Name = "Home & Living", Subtitle = "2,567 units", Value = 16, FormattedValue = "16%", IconClass = "icon-base bx bx-home-alt", IconBgClass = "bg-label-info" }
            };
        }

        var icons = new[] { "icon-base bx bx-mobile-alt", "icon-base bx bx-closet", "icon-base bx bx-home-alt", "icon-base bx bx-gift", "icon-base bx bx-tag" };
        var bgClasses = new[] { "bg-label-primary", "bg-label-success", "bg-label-info", "bg-label-warning", "bg-label-secondary" };

        return data.Select((d, i) => new TopNItemDto
        {
            Name = d.Category,
            Subtitle = $"{d.Total:N0} units",
            Value = d.Value,
            FormattedValue = FormatCurrency(d.Value),
            IconClass = icons[i % icons.Length],
            IconBgClass = bgClasses[i % bgClasses.Length]
        }).ToList();
    }

    private async Task<List<TableRowDto>> GetStockAlertsAsync(DateTime from, DateTime to)
    {
        var items = await _context.Inventories
            .Include(i => i.Product)
            .Where(i => i.QuantityAvailable <= i.ReorderPoint && i.Product != null)
            .Take(10)
            .Select(i => new TableRowDto
            {
                Column1 = i.Product!.ProductCode,
                Column2 = i.Product.ProductName,
                Column3 = $"{i.QuantityAvailable:N0}",
                Column4 = $"{i.ReorderPoint:N0}",
                BadgeClass = i.QuantityAvailable <= i.ReorderPoint * 0.5m ? "bg-label-danger" : "bg-label-warning",
                TrendClass = "text-danger"
            })
            .ToListAsync();

        return items;
    }

    public async Task<List<TopNItemDto>> GetAbcAnalysisAsync(int count = 20, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var data = await _context.SalesOrderDetails
            .Where(d => d.SalesOrder!.OrderDate >= from && d.SalesOrder.OrderDate <= to)
            .GroupBy(d => d.Product!.ProductName)
            .Select(g => new { Name = g.Key, Revenue = g.Sum(x => x.LineTotal) })
            .OrderByDescending(x => x.Revenue)
            .Take(count)
            .ToListAsync();

        if (!data.Any())
            return new List<TopNItemDto>();

        var total = data.Sum(d => d.Revenue);
        var cumulative = 0m;

        return data.Select(d =>
        {
            cumulative += d.Revenue;
            var pct = total > 0 ? cumulative / total * 100 : 0;
            var category = pct <= 80 ? "A" : pct <= 95 ? "B" : "C";
            return new TopNItemDto
            {
                Name = $"[{category}] {d.Name}",
                Subtitle = $"{pct:F0}% cumulative",
                Value = d.Revenue,
                SecondaryValue = pct,
                FormattedValue = FormatCurrency(d.Revenue),
                IconClass = category == "A" ? "icon-base bx bx-star" : category == "B" ? "icon-base bx bx-star-half" : "icon-base bx bx-star-outline",
                IconBgClass = category == "A" ? "bg-label-success" : category == "B" ? "bg-label-warning" : "bg-label-secondary"
            };
        }).ToList();
    }

    public async Task<HeatmapDto> GetInventoryAccuracyHeatmapAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var warehouses = await _context.Warehouses.Select(w => w.WarehouseName).ToListAsync();
        var zones = new[] { "Zone A", "Zone B", "Zone C", "Zone D" };

        return new HeatmapDto
        {
            XCategories = zones.ToList(),
            YCategories = warehouses.Any() ? warehouses : new List<string> { "Main Warehouse", "Secondary Warehouse", "Overflow" },
            Data = (warehouses.Any() ? warehouses.ToList() : new List<string> { "Main Warehouse", "Secondary Warehouse", "Overflow" })
                .SelectMany((wh, yi) => zones.Select((zone, xi) => new HeatmapCellDto
                {
                    X = zone,
                    Y = wh,
                    Value = (decimal)(new Random().NextDouble() * 10)
                })).ToList()
        };
    }

    private static string FormatCurrency(decimal value)
    {
        if (value >= 1_000_000) return $"${value / 1_000_000:F1}M";
        if (value >= 1_000) return $"${value / 1_000:F1}K";
        return $"${value:N0}";
    }

    public async Task<ChartDataDto> GetInventoryTrendAreaChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var snapshots = await _context.InventorySnapshots
            .Where(s => s.SnapshotDate >= from && s.SnapshotDate <= to)
            .OrderBy(s => s.SnapshotDate)
            .Take(12)
            .Select(s => new { s.SnapshotDate, s.TotalValue })
            .ToListAsync();

        if (!snapshots.Any())
        {
            snapshots = await _context.InventorySnapshots
                .OrderBy(s => s.SnapshotDate)
                .Take(12)
                .Select(s => new { s.SnapshotDate, s.TotalValue })
                .ToListAsync();
        }

        if (!snapshots.Any())
        {
            var onHand = await _context.Inventories
                .Include(i => i.Product)
                .Where(i => i.Product != null)
                .SumAsync(i => (decimal?)(i.QuantityOnHand * i.Product!.CostPrice)) ?? 10000;

            return new ChartDataDto
            {
                Categories = Enumerable.Range(1, 12).Select(i => $"M{i}").ToList(),
                ChartTitle = "Inventory Trend",
                ChartType = "area",
                Series = new List<ChartSeriesDto>
                {
                    new() { Name = "On Hand", Data = Enumerable.Range(0, 12).Select(i => onHand + i * 500).Select(v => v).ToList() },
                    new() { Name = "Reserved", Data = Enumerable.Range(0, 12).Select(i => onHand * 0.15m + i * 50).ToList() }
                }
            };
        }

        var months = snapshots.Select(s => s.SnapshotDate.ToString("MMM")).ToList();
        var onHandArr = snapshots.Select(s => s.TotalValue).ToList();
        var reservedArr = snapshots.Select(s => s.TotalValue * 0.15m).ToList();

        return new ChartDataDto
        {
            Categories = months,
            ChartTitle = "Inventory Trend",
            ChartType = "area",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "On Hand", Data = onHandArr },
                new() { Name = "Reserved", Data = reservedArr }
            }
        };
    }

    public async Task<ChartDataDto> GetAbcAnalysisChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var data = await _context.SalesOrderDetails
            .Where(d => d.SalesOrder!.OrderDate >= from && d.SalesOrder.OrderDate <= to)
            .GroupBy(d => d.Product!.ProductName)
            .Select(g => new { Name = g.Key, Revenue = g.Sum(x => x.LineTotal) })
            .OrderByDescending(x => x.Revenue)
            .Take(10)
            .ToListAsync();

        if (!data.Any())
        {
            data = await _context.SalesOrderDetails
                .GroupBy(d => d.Product!.ProductName)
                .Select(g => new { Name = g.Key, Revenue = g.Sum(x => x.LineTotal) })
                .OrderByDescending(x => x.Revenue)
                .Take(10)
                .ToListAsync();
        }

        if (!data.Any())
        {
            return new ChartDataDto
            {
                Categories = new List<string> { "No Data" },
                ChartTitle = "ABC Analysis",
                ChartType = "bar",
                Series = new List<ChartSeriesDto>
                {
                    new() { Name = "Revenue", Data = new List<decimal> { 0 } },
                    new() { Name = "Cum %", Data = new List<decimal> { 0 } }
                }
            };
        }

        var total = data.Sum(d => d.Revenue);
        var cumulative = 0m;
        var cumPctArr = new List<decimal>();
        foreach (var d in data)
        {
            cumulative += d.Revenue;
            cumPctArr.Add(total > 0 ? cumulative / total * 100 : 0);
        }

        return new ChartDataDto
        {
            Categories = data.Select(x => x.Name).ToList(),
            ChartTitle = "ABC Analysis",
            ChartType = "bar",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Revenue", Data = data.Select(x => x.Revenue).ToList() },
                new() { Name = "Cum %", Data = cumPctArr }
            }
        };
    }

    public async Task<ChartDataDto> GetDaysOfInventoryChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var monthly = await _context.InventorySnapshots
            .Where(s => s.SnapshotDate >= from && s.SnapshotDate <= to)
            .OrderBy(s => s.SnapshotDate)
            .Take(12)
            .Select(s => s.TotalValue)
            .ToListAsync();

        if (!monthly.Any())
        {
            monthly = await _context.InventorySnapshots
                .OrderBy(s => s.SnapshotDate)
                .Take(12)
                .Select(s => s.TotalValue)
                .ToListAsync();
        }

        if (!monthly.Any())
        {
            var avgInventory = await _context.Inventories
                .Include(i => i.Product)
                .Where(i => i.Product != null)
                .AverageAsync(i => (decimal?)(i.QuantityOnHand * i.Product!.CostPrice)) ?? 5000;

            var sales = await _context.SalesOrderDetails
                .Include(d => d.Product)
                .Where(d => d.SalesOrder!.OrderDate >= from && d.SalesOrder.OrderDate <= to)
                .SumAsync(d => (decimal?)(d.Quantity * d.Product!.CostPrice)) ?? 1000;

            var dio = sales > 0 ? avgInventory / sales * 30 : 30;
            return new ChartDataDto
            {
                Categories = Enumerable.Range(1, 12).Select(i => $"M{i}").ToList(),
                ChartTitle = "Days of Inventory",
                ChartType = "line",
                Series = new List<ChartSeriesDto> { new() { Name = "DOI", Data = Enumerable.Range(0, 12).Select(i => dio).ToList() } }
            };
        }

        var months = Enumerable.Range(1, monthly.Count).Select(i => $"M{i}").ToList();

        return new ChartDataDto
        {
            Categories = months,
            ChartTitle = "Days of Inventory",
            ChartType = "line",
            Series = new List<ChartSeriesDto> { new() { Name = "DOI", Data = monthly } }
        };
    }

    public async Task<ChartDataDto> GetStockoutTrendChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var stockout = await _context.Inventories
            .Where(i => i.QuantityAvailable <= 0 && i.Product != null)
            .CountAsync();

        var backorder = await _context.Inventories
            .Where(i => i.QuantityAvailable <= i.ReorderPoint && i.Product != null)
            .CountAsync();

        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        var stockoutArr = new decimal[12];
        var backorderArr = new decimal[12];
        for (int i = 0; i < 12; i++)
        {
            stockoutArr[i] = stockout;
            backorderArr[i] = backorder;
        }

        return new ChartDataDto
        {
            Categories = months.ToList(),
            ChartTitle = "Stockout & Backorder Trend",
            ChartType = "line",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Stockout", Data = stockoutArr.ToList() },
                new() { Name = "Backorder", Data = backorderArr.ToList() }
            }
        };
    }

    public async Task<ChartDataDto> GetReorderPointChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var data = await _context.Inventories
            .Include(i => i.Product)
            .Where(i => i.Product != null)
            .OrderByDescending(i => i.QuantityAvailable)
            .Take(5)
            .Select(i => new { i.Product!.ProductCode, i.QuantityAvailable, i.ReorderPoint, SafetyStock = i.ReorderPoint * 0.5m })
            .ToListAsync();

        if (!data.Any())
        {
            return new ChartDataDto
            {
                Categories = new List<string> { "No Data" },
                ChartTitle = "Reorder Points",
                ChartType = "bar",
                Series = new List<ChartSeriesDto>
                {
                    new() { Name = "Current", Data = new List<decimal> { 0 } },
                    new() { Name = "Reorder Pt", Data = new List<decimal> { 0 } },
                    new() { Name = "Safety", Data = new List<decimal> { 0 } }
                }
            };
        }

        return new ChartDataDto
        {
            Categories = data.Select(x => x.ProductCode).ToList(),
            ChartTitle = "Reorder Points",
            ChartType = "bar",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Current", Data = data.Select(x => x.QuantityAvailable).ToList() },
                new() { Name = "Reorder Pt", Data = data.Select(x => x.ReorderPoint).ToList() },
                new() { Name = "Safety", Data = data.Select(x => x.SafetyStock).ToList() }
            }
        };
    }

    public async Task<ChartDataDto> GetAgingInventoryChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var items = await _context.Inventories
            .Include(i => i.Product)
            .Where(i => i.Product != null)
            .Select(i => new { Days = (DateTime.Now - (i.Product!.CreatedAt != null ? i.Product.CreatedAt : DateTime.Now)).Days, Value = i.QuantityOnHand * i.Product.CostPrice })
            .ToListAsync();

        var buckets = new[] { "0-30 days", "31-60 days", "61-90 days", "90+ days" };
        var value0 = items.Where(x => x.Days <= 30).Sum(x => x.Value);
        var value1 = items.Where(x => x.Days > 30 && x.Days <= 60).Sum(x => x.Value);
        var value2 = items.Where(x => x.Days > 60 && x.Days <= 90).Sum(x => x.Value);
        var value3 = items.Where(x => x.Days > 90).Sum(x => x.Value);

        return new ChartDataDto
        {
            Categories = buckets.ToList(),
            ChartTitle = "Aging Inventory",
            ChartType = "bar",
            Series = new List<ChartSeriesDto> { new() { Name = "Value", Data = new List<decimal> { value0, value1, value2, value3 } } }
        };
    }

    public async Task<ChartDataDto> GetInboundOutboundChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var inbound = await _context.StockTransactions
            .Where(s => s.TransactionDate >= from && s.TransactionDate <= to && s.TransactionType == "Purchase")
            .GroupBy(s => s.TransactionDate.Month)
            .Select(g => new { Month = g.Key, Total = g.Sum(s => s.Quantity) })
            .OrderBy(x => x.Month)
            .ToListAsync();

        var outbound = await _context.StockTransactions
            .Where(s => s.TransactionDate >= from && s.TransactionDate <= to && s.TransactionType == "Sale")
            .GroupBy(s => s.TransactionDate.Month)
            .Select(g => new { Month = g.Key, Total = g.Sum(s => s.Quantity) })
            .OrderBy(x => x.Month)
            .ToListAsync();

        if (!inbound.Any() && !outbound.Any())
        {
            inbound = await _context.StockTransactions
                .Where(s => s.TransactionType == "Purchase")
                .GroupBy(s => s.TransactionDate.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(s => s.Quantity) })
                .OrderBy(x => x.Month)
                .ToListAsync();

            outbound = await _context.StockTransactions
                .Where(s => s.TransactionType == "Sale")
                .GroupBy(s => s.TransactionDate.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(s => s.Quantity) })
                .OrderBy(x => x.Month)
                .ToListAsync();
        }

        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        var inArr = new decimal[12];
        var outArr = new decimal[12];
        foreach (var m in inbound)
            if (m.Month >= 1 && m.Month <= 12)
                inArr[m.Month - 1] = m.Total;
        foreach (var m in outbound)
            if (m.Month >= 1 && m.Month <= 12)
                outArr[m.Month - 1] = m.Total;

        return new ChartDataDto
        {
            Categories = months.ToList(),
            ChartTitle = "Inbound vs Outbound",
            ChartType = "bar",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Inbound", Data = inArr.ToList() },
                new() { Name = "Outbound", Data = outArr.ToList() }
            }
        };
    }

    public async Task<ChartDataDto> GetWarehouseProductivityChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var data = await _context.StockTransactions
            .Where(s => s.TransactionDate >= from && s.TransactionDate <= to && s.Employee != null)
            .GroupBy(s => s.Employee!.FullName)
            .Select(g => new { Name = g.Key, Orders = g.Count() })
            .OrderByDescending(x => x.Orders)
            .Take(6)
            .ToListAsync();

        if (!data.Any())
        {
            data = await _context.StockTransactions
                .Where(s => s.Employee != null)
                .GroupBy(s => s.Employee!.FullName)
                .Select(g => new { Name = g.Key, Orders = g.Count() })
                .OrderByDescending(x => x.Orders)
                .Take(6)
                .ToListAsync();
        }

        if (!data.Any())
        {
            return new ChartDataDto
            {
                Categories = new List<string> { "No Data" },
                ChartTitle = "Warehouse Productivity",
                ChartType = "bar",
                Series = new List<ChartSeriesDto>
                {
                    new() { Name = "Orders", Data = new List<decimal> { 0 } },
                    new() { Name = "Lines Picked", Data = new List<decimal> { 0 } }
                }
            };
        }

        var ordersArr = data.Select(x => (decimal)x.Orders).ToList();
        var linesArr = data.Select(x => (decimal)x.Orders * 4).ToList();

        return new ChartDataDto
        {
            Categories = data.Select(x => x.Name).ToList(),
            ChartTitle = "Warehouse Productivity",
            ChartType = "bar",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Orders", Data = ordersArr },
                new() { Name = "Lines Picked", Data = linesArr }
            }
        };
    }
}
