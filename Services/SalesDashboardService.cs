using Microsoft.EntityFrameworkCore;
using Dashboard.Data;
using Dashboard.Models;
using Dashboard.Models.ViewModels;
using Dashboard.Services.Interfaces;

namespace Dashboard.Services;

public class SalesDashboardService : ISalesDashboardService
{
    private readonly ApplicationDbContext _context;

    public SalesDashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SalesDashboardViewModel> GetDashboardDataAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var salesFunnel = await GetSalesFunnelAsync(fromDate, toDate);

        var vm = new SalesDashboardViewModel
        {
            TotalSales = await GetTotalSalesAsync(from, to),
            TotalOrders = await GetTotalOrdersAsync(from, to),
            NewCustomers = await GetNewCustomersAsync(from, to),
            PendingDeals = await GetPendingDealsAsync(from, to),
            WinRate = await GetWinRateAsync(from, to),
            AverageOrderValue = await GetAverageOrderValueAsync(from, to),
            GrossMargin = await GetGrossMarginAsync(from, to),
            SalesTargetAchievement = await GetSalesTargetAchievementAsync(from, to),
            SalesOverviewChart = await GetSalesOverviewChartAsync(from, to),
            RevenueBySalespersonChart = await GetRevenueBySalespersonChartAsync(from, to),
            RevenueByChannelChart = await GetRevenueByChannelChartAsync(from, to),
            PipelineByStageChart = await GetPipelineByStageChartAsync(from, to),
            ForecastChart = await GetForecastChartAsync(from, to),
            SalespersonBarChart = await GetRevenueBySalespersonChartAsync(from, to),
            OrderMarginScatterChart = await GetOrderValueVsMarginScatterChartAsync(from, to),
            ParetoChart = await GetParetoChartAsync(from, to),
            SalesCycleBoxPlotChart = await GetSalesCycleBoxPlotChartAsync(from, to),
            RevenueByChannelBarChart = await GetRevenueByChannelChartAsync(from, to),
            TopProducts = await GetTopProductsAsync(10, from, to),
            TopSalespersons = await GetTopSalespersonsAsync(10, from, to),
            RecentOrders = await GetRecentOrdersAsync(10, from, to),
            SalesFunnel = salesFunnel,
            PipelineFunnelChart = BuildPipelineFunnelChart(salesFunnel),
            SalespersonProductHeatmap = await GetSalespersonProductHeatmapAsync(from, to),
            TargetAchievementPercent = await CalculateTargetAchievementAsync(from, to)
        };

        return vm;
    }

    private static ChartDataDto BuildPipelineFunnelChart(IReadOnlyList<FunnelStageDto> stages)
    {
        if (stages.Count == 0)
        {
            return new ChartDataDto
            {
                Categories = new List<string> { "No data" },
                ChartTitle = "Pipeline Funnel",
                ChartType = "bar",
                Series = new List<ChartSeriesDto> { new() { Name = "Count", Data = new List<decimal> { 0 } } }
            };
        }

        return new ChartDataDto
        {
            Categories = stages.Select(s => s.Stage).ToList(),
            ChartTitle = "Pipeline Funnel",
            ChartType = "bar",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Count", Data = stages.Select(s => s.Count).ToList() }
            }
        };
    }

    private (DateTime from, DateTime to) GetDateRange(DateTime? from, DateTime? to)
    {
        var toDate = to ?? DateTime.Now;
        var fromDate = from ?? new DateTime(toDate.Year, toDate.Month, 1);
        return (fromDate, toDate);
    }

    public async Task<KpiCardDto> GetTotalSalesAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var current = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to)
            .SumAsync(so => (decimal?)so.TotalAmount) ?? 0;
        var lastPeriod = await _context.SalesOrders
            .Where(so => so.OrderDate >= from.AddMonths(-1) && so.OrderDate <= from)
            .SumAsync(so => (decimal?)so.TotalAmount) ?? 0;
        var growth = lastPeriod > 0 ? ((current - lastPeriod) / lastPeriod) * 100 : 0;

        return MakeKpi("Total Sales", current, growth, "up", "icon-base bx bx-dollar", "bg-label-success", "text-success");
    }

    public async Task<KpiCardDto> GetTotalOrdersAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var current = await _context.SalesOrders.Where(so => so.OrderDate >= from && so.OrderDate <= to).CountAsync();
        var lastPeriod = await _context.SalesOrders.Where(so => so.OrderDate >= from.AddMonths(-1) && so.OrderDate <= from).CountAsync();
        var growth = lastPeriod > 0 ? ((current - lastPeriod) / (decimal)lastPeriod) * 100 : 0;

        return MakeKpi("Total Orders", current, growth, "up", "icon-base bx bx-cart", "bg-label-primary", "text-primary", suffix: "");
    }

    public async Task<KpiCardDto> GetNewCustomersAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var current = await _context.Customers.Where(c => c.JoinDate >= from && c.JoinDate <= to).CountAsync();
        var lastPeriod = await _context.Customers.Where(c => c.JoinDate >= from.AddMonths(-1) && c.JoinDate <= from).CountAsync();
        var growth = lastPeriod > 0 ? ((current - lastPeriod) / (decimal)lastPeriod) * 100 : 0;

        return MakeKpi("New Customers", current, growth, "up", "icon-base bx bx-user-plus", "bg-label-info", "text-info", suffix: "");
    }

    public async Task<KpiCardDto> GetPendingDealsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var pending = await _context.Opportunities
            .Where(o => (o.Status == "Open" || o.Status == "In Progress")
                && o.CreatedAt >= from && o.CreatedAt <= to)
            .CountAsync();

        return new KpiCardDto
        {
            Title = "Pending Deals",
            Value = pending.ToString("N0"),
            FormattedValue = pending.ToString("N0"),
            NumericValue = pending,
            GrowthPercent = 0,
            Trend = "neutral",
            TrendLabel = "In Progress",
            IconClass = "icon-base bx bx-time",
            IconBgClass = "bg-label-warning",
            IconColorClass = "text-warning",
            Subtitle = "Open pipeline"
        };
    }

    public async Task<KpiCardDto> GetWinRateAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var won = await _context.Opportunities.Where(o => o.Status == "Won" && o.ActualCloseDate >= from && o.ActualCloseDate <= to).CountAsync();
        var total = await _context.Opportunities.Where(o => (o.Status == "Won" || o.Status == "Lost") && o.ActualCloseDate >= from && o.ActualCloseDate <= to).CountAsync();
        var rate = total > 0 ? (decimal)won / total * 100 : 0;

        return new KpiCardDto
        {
            Title = "Win Rate",
            Value = $"{rate:F1}%",
            FormattedValue = $"{rate:F1}%",
            NumericValue = rate,
            GrowthPercent = 0,
            Trend = "neutral",
            TrendLabel = "This month",
            IconClass = "icon-base bx bx-trophy",
            IconBgClass = "bg-label-success",
            IconColorClass = "text-success"
        };
    }

    public async Task<KpiCardDto> GetAverageOrderValueAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var avg = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to)
            .AverageAsync(so => (decimal?)so.TotalAmount) ?? 0;

        return MakeKpi("Avg Order Value", avg, 0, "neutral", "icon-base bx bx-receipt", "bg-label-secondary", "text-secondary");
    }

    public async Task<KpiCardDto> GetGrossMarginAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var orderDetails = await _context.SalesOrderDetails
            .Include(d => d.SalesOrder)
            .Where(d => d.SalesOrder!.OrderDate >= from && d.SalesOrder.OrderDate <= to)
            .ToListAsync();

        var revenue = orderDetails.Sum(d => d.LineTotal);
        var cost = orderDetails.Sum(d => d.Quantity * (d.Product?.CostPrice ?? 0));
        var margin = revenue > 0 ? (revenue - cost) / revenue * 100 : 0;

        return new KpiCardDto
        {
            Title = "Gross Margin",
            Value = $"{margin:F1}%",
            FormattedValue = $"{margin:F1}%",
            NumericValue = margin,
            GrowthPercent = 0,
            Trend = margin >= 20 ? "up" : margin >= 10 ? "neutral" : "down",
            TrendLabel = margin >= 20 ? "Healthy" : margin >= 10 ? "Normal" : "Low",
            IconClass = "icon-base bx bx-chart",
            IconBgClass = "bg-label-primary",
            IconColorClass = "text-primary"
        };
    }

    private async Task<KpiCardDto> GetSalesTargetAchievementAsync(DateTime from, DateTime to)
    {
        var actual = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to)
            .SumAsync(so => (decimal?)so.TotalAmount) ?? 0;

        var target = await _context.KpiTargets
            .Where(k => k.StartDate.Year == to.Year || k.EndDate.Year == to.Year)
            .SumAsync(k => (decimal?)k.TargetValue) ?? actual * 1.1m;

        var pct = target > 0 ? actual / target * 100 : 0;

        return new KpiCardDto
        {
            Title = "Target Achievement",
            Value = $"{pct:F0}%",
            FormattedValue = $"{pct:F0}%",
            NumericValue = pct,
            GrowthPercent = pct,
            Trend = pct >= 100 ? "up" : pct >= 70 ? "neutral" : "down",
            TrendLabel = pct >= 100 ? "On Track" : $"{pct:F0}%",
            IconClass = "icon-base bx bx-target-lock",
            IconBgClass = "bg-label-warning",
            IconColorClass = "text-warning"
        };
    }

    public async Task<ChartDataDto> GetSalesOverviewChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var monthlyData = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to)
            .GroupBy(so => so.OrderDate.Month)
            .Select(g => new { Month = g.Key, Total = g.Sum(so => so.TotalAmount) })
            .OrderBy(x => x.Month)
            .ToListAsync();

        if (!monthlyData.Any())
        {
            monthlyData = await _context.SalesOrders
                .GroupBy(so => so.OrderDate.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(so => so.TotalAmount) })
                .OrderBy(x => x.Month)
                .ToListAsync();
        }

        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        var data = new decimal[12];
        foreach (var m in monthlyData)
            if (m.Month >= 1 && m.Month <= 12)
                data[m.Month - 1] = m.Total;

        return new ChartDataDto
        {
            Categories = months.ToList(),
            ChartTitle = "Sales Overview",
            ChartType = "bar",
            Series = new List<ChartSeriesDto> { new() { Name = "Sales", Data = data.ToList() } }
        };
    }

    public async Task<ChartDataDto> GetRevenueBySalespersonChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var data = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to && so.SalesEmployee != null)
            .GroupBy(so => so.SalesEmployee!.FullName)
            .Select(g => new { Name = g.Key, Total = g.Sum(so => so.TotalAmount) })
            .OrderByDescending(x => x.Total)
            .Take(10)
            .ToListAsync();

        if (!data.Any())
        {
            var allData = await _context.SalesOrders
                .Where(so => so.SalesEmployee != null)
                .GroupBy(so => so.SalesEmployee!.FullName)
                .Select(g => new { Name = g.Key, Total = g.Sum(so => so.TotalAmount) })
                .OrderByDescending(x => x.Total)
                .Take(10)
                .ToListAsync();

            return new ChartDataDto
            {
                Categories = allData.Select(x => x.Name).ToList(),
                ChartTitle = "Revenue by Salesperson",
                ChartType = "bar",
                Series = new List<ChartSeriesDto> { new() { Name = "Revenue", Data = allData.Select(x => x.Total).ToList() } }
            };
        }

        return new ChartDataDto
        {
            Categories = data.Select(x => x.Name).ToList(),
            ChartTitle = "Revenue by Salesperson",
            ChartType = "bar",
            Series = new List<ChartSeriesDto> { new() { Name = "Revenue", Data = data.Select(x => x.Total).ToList() } }
        };
    }

    public async Task<ChartDataDto> GetRevenueByChannelChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var data = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to && so.SalesChannel != null)
            .GroupBy(so => so.SalesChannel!.ChannelName)
            .Select(g => new { Channel = g.Key, Revenue = g.Sum(so => so.TotalAmount), Count = g.Count() })
            .OrderByDescending(x => x.Revenue)
            .ToListAsync();

        if (!data.Any())
        {
            var fallbackData = await _context.SalesOrders
                .Where(so => so.SalesChannel != null)
                .GroupBy(so => so.SalesChannel!.ChannelName)
                .Select(g => new { Channel = g.Key, Revenue = g.Sum(so => so.TotalAmount), Count = g.Count() })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();

            if (!fallbackData.Any())
            {
                return new ChartDataDto
                {
                    Categories = new List<string> { "No Data" },
                    ChartTitle = "Revenue by Channel",
                    ChartType = "bar",
                    Series = new List<ChartSeriesDto>
                    {
                        new() { Name = "Revenue", Data = new List<decimal> { 0 } },
                        new() { Name = "Orders", Data = new List<decimal> { 0 } }
                    }
                };
            }

            return new ChartDataDto
            {
                Categories = fallbackData.Select(x => x.Channel).ToList(),
                ChartTitle = "Revenue by Channel",
                ChartType = "bar",
                Series = new List<ChartSeriesDto>
                {
                    new() { Name = "Revenue", Data = fallbackData.Select(x => x.Revenue).ToList() },
                    new() { Name = "Orders", Data = fallbackData.Select(x => (decimal)x.Count).ToList() }
                }
            };
        }

        return new ChartDataDto
        {
            Categories = data.Select(x => x.Channel).ToList(),
            ChartTitle = "Revenue by Channel",
            ChartType = "bar",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Revenue", Data = data.Select(x => x.Revenue).ToList() },
                new() { Name = "Orders", Data = data.Select(x => (decimal)x.Count).ToList() }
            }
        };
    }

    public async Task<ChartDataDto> GetPipelineByStageChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var data = await _context.Opportunities
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to && o.Status != null)
            .GroupBy(o => o.Status)
            .Select(g => new { Name = g.Key, Value = g.Sum(o => o.EstimatedValue), Count = g.Count() })
            .OrderByDescending(x => x.Value)
            .ToListAsync();

        if (!data.Any())
        {
            var allData = await _context.Opportunities
                .Where(o => o.Status != null)
                .GroupBy(o => o.Status)
                .Select(g => new { Name = g.Key, Value = g.Sum(o => o.EstimatedValue), Count = g.Count() })
                .OrderByDescending(x => x.Value)
                .ToListAsync();

            if (!allData.Any())
            {
                return new ChartDataDto
                {
                    Categories = new List<string> { "No Data" },
                    ChartTitle = "Pipeline by Stage",
                    ChartType = "bar",
                    Series = new List<ChartSeriesDto> { new() { Name = "Value", Data = new List<decimal> { 0 } } }
                };
            }

            return new ChartDataDto
            {
                Categories = allData.Select(x => x.Name).ToList(),
                ChartTitle = "Pipeline by Stage",
                ChartType = "bar",
                Series = new List<ChartSeriesDto> { new() { Name = "Value", Data = allData.Select(x => x.Value).ToList() } }
            };
        }

        return new ChartDataDto
        {
            Categories = data.Select(x => x.Name).ToList(),
            ChartTitle = "Pipeline by Stage",
            ChartType = "bar",
            Series = new List<ChartSeriesDto> { new() { Name = "Value", Data = data.Select(x => x.Value).ToList() } }
        };
    }

    public async Task<List<TopNItemDto>> GetTopProductsAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var data = await _context.SalesOrderDetails
            .Where(d => d.SalesOrder!.OrderDate >= from && d.SalesOrder.OrderDate <= to)
            .GroupBy(d => d.Product!.ProductName)
            .Select(g => new { Name = g.Key, Revenue = g.Sum(x => x.LineTotal), Qty = g.Sum(x => x.Quantity) })
            .OrderByDescending(x => x.Revenue)
            .Take(count)
            .ToListAsync();

        if (!data.Any())
            return new List<TopNItemDto> { new() { Name = "No data", Subtitle = "0 units", Value = 0, FormattedValue = "$0", IconClass = "icon-base bx bx-box", IconBgClass = "bg-label-primary" } };

        var icons = new[] { "icon-base bx bx-mobile-alt", "icon-base bx bx-closet", "icon-base bx bx-home-alt", "icon-base bx bx-gift", "icon-base bx bx-tag" };
        var bgClasses = new[] { "bg-label-primary", "bg-label-success", "bg-label-info", "bg-label-warning", "bg-label-secondary" };

        return data.Select((d, i) => new TopNItemDto
        {
            Name = d.Name,
            Subtitle = $"{d.Qty:N0} units",
            Value = d.Revenue,
            FormattedValue = FormatCurrency(d.Revenue),
            IconClass = icons[i % icons.Length],
            IconBgClass = bgClasses[i % bgClasses.Length]
        }).ToList();
    }

    public async Task<List<TopNItemDto>> GetTopSalespersonsAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var data = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to && so.SalesEmployee != null)
            .GroupBy(so => so.SalesEmployee!.FullName)
            .Select(g => new { Name = g.Key, Revenue = g.Sum(so => so.TotalAmount), Orders = g.Count() })
            .OrderByDescending(x => x.Revenue)
            .Take(count)
            .ToListAsync();

        if (!data.Any())
            return new List<TopNItemDto> { new() { Name = "No data", Subtitle = "0 orders", Value = 0, FormattedValue = "$0", IconClass = "icon-base bx bx-user", IconBgClass = "bg-label-primary" } };

        return data.Select((d, i) => new TopNItemDto
        {
            Name = d.Name,
            Subtitle = $"{d.Orders} orders",
            Value = d.Revenue,
            FormattedValue = FormatCurrency(d.Revenue),
            IconClass = "icon-base bx bx-user",
            IconBgClass = "bg-label-primary"
        }).ToList();
    }

    public async Task<List<TableRowDto>> GetRecentOrdersAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var orders = await _context.SalesOrders
            .Include(so => so.Customer)
            .Include(so => so.OrderDetails).ThenInclude(d => d.Product)
            .Where(so => so.OrderDate >= from && so.OrderDate <= to)
            .OrderByDescending(so => so.OrderDate)
            .Take(count)
            .Select(so => new TableRowDto
            {
                Column1 = so.OrderNumber,
                Column2 = so.Customer != null ? so.Customer.CustomerName : "N/A",
                Column3 = so.OrderDetails.FirstOrDefault() != null ? so.OrderDetails.First().Product!.ProductName : so.OrderDetails.FirstOrDefault()!.Description ?? "Mixed",
                Column4 = FormatCurrency(so.TotalAmount),
                BadgeClass = so.PaymentStatus == "Paid" ? "bg-label-success" : so.PaymentStatus == "Pending" ? "bg-label-warning" : "bg-label-danger",
                TrendClass = "text-success"
            })
            .ToListAsync();

        return orders;
    }

    public async Task<List<FunnelStageDto>> GetSalesFunnelAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var leads = await _context.MarketingLeads.CountAsync(l => l.CreatedDate >= from && l.CreatedDate <= to);
        var qualified = await _context.Opportunities.Where(o => o.CreatedAt >= from && o.CreatedAt <= to && o.Status != null).CountAsync();
        var proposals = await _context.Quotes.CountAsync(q => q.CreatedAt >= from && q.CreatedAt <= to);
        var won = await _context.Opportunities.CountAsync(o => o.Status == "Won" && o.ActualCloseDate >= from && o.ActualCloseDate <= to);
        var lost = await _context.Opportunities.CountAsync(o => o.Status == "Lost" && o.ActualCloseDate >= from && o.ActualCloseDate <= to);

        if (leads == 0) leads = 100;
        var stages = new List<FunnelStageDto>
        {
            new() { Stage = "Leads", Count = leads, ConversionRate = 100 },
            new() { Stage = "Qualified", Count = qualified, ConversionRate = qualified > 0 ? Math.Round((decimal)qualified / leads * 100, 1) : 0 },
            new() { Stage = "Proposals", Count = proposals, ConversionRate = proposals > 0 && qualified > 0 ? Math.Round((decimal)proposals / qualified * 100, 1) : 0 },
            new() { Stage = "Won", Count = won, ConversionRate = won > 0 && proposals > 0 ? Math.Round((decimal)won / proposals * 100, 1) : 0 },
            new() { Stage = "Lost", Count = lost, ConversionRate = lost > 0 && proposals > 0 ? Math.Round((decimal)lost / proposals * 100, 1) : 0 }
        };

        return stages;
    }

    public async Task<List<ScatterPointDto>> GetOrderValueVsMarginScatterAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var data = await _context.SalesOrderDetails
            .Include(d => d.Product)
            .Where(d => d.SalesOrder!.OrderDate >= from && d.SalesOrder.OrderDate <= to)
            .GroupBy(d => new { d.ProductID, ProductName = d.Product!.ProductName })
            .Select(g => new
            {
                g.Key.ProductName,
                AvgValue = g.Average(d => d.LineTotal),
                AvgCost = g.Average(d => d.Quantity * d.Product!.CostPrice),
                TotalQty = g.Sum(d => d.Quantity)
            })
            .ToListAsync();

        return data.Select(d => new ScatterPointDto
        {
            X = d.AvgValue,
            Y = d.AvgCost > 0 ? (d.AvgValue - d.AvgCost) / d.AvgCost * 100 : 0,
            Size = Math.Max(1, (decimal)d.TotalQty / 100),
            Label = d.ProductName
        }).ToList();
    }

    public async Task<List<TopNItemDto>> GetParetoByCustomerAsync(int count = 20, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var data = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to && so.Customer != null)
            .GroupBy(so => so.Customer!.CustomerName)
            .Select(g => new { Name = g.Key, Revenue = g.Sum(so => so.TotalAmount) })
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
            return new TopNItemDto
            {
                Name = d.Name,
                Subtitle = $"{(total > 0 ? cumulative / total * 100 : 0):F0}% cumulative",
                Value = d.Revenue,
                SecondaryValue = total > 0 ? cumulative / total * 100 : 0,
                FormattedValue = FormatCurrency(d.Revenue),
                IconClass = "icon-base bx bx-user",
                IconBgClass = "bg-label-primary"
            };
        }).ToList();
    }

    public async Task<HeatmapDto> GetSalespersonProductHeatmapAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var data = await _context.SalesOrderDetails
            .Include(d => d.SalesOrder).ThenInclude(so => so!.SalesEmployee)
            .Where(d => d.SalesOrder!.OrderDate >= from && d.SalesOrder.OrderDate <= to)
            .GroupBy(d => new
            {
                Salesperson = d.SalesOrder!.SalesEmployee != null ? d.SalesOrder.SalesEmployee.FullName : "Unassigned",
                Product = d.Product != null ? d.Product.ProductName : "Unknown"
            })
            .Select(g => new { g.Key.Salesperson, g.Key.Product, Revenue = g.Sum(x => x.LineTotal) })
            .ToListAsync();

        if (data.Count == 0)
        {
            data = await _context.SalesOrderDetails
                .Include(d => d.SalesOrder).ThenInclude(so => so!.SalesEmployee)
                .Where(d => d.Product != null && d.SalesOrder != null)
                .GroupBy(d => new
                {
                    Salesperson = d.SalesOrder!.SalesEmployee != null ? d.SalesOrder.SalesEmployee.FullName : "Unassigned",
                    Product = d.Product != null ? d.Product.ProductName : "Unknown"
                })
                .Select(g => new { g.Key.Salesperson, g.Key.Product, Revenue = g.Sum(x => x.LineTotal) })
                .ToListAsync();
        }

        var xCategories = data.Select(d => d.Product).Distinct().OrderBy(x => x).ToList();
        var yCategories = data.Select(d => d.Salesperson).Distinct().OrderBy(x => x).ToList();

        return new HeatmapDto
        {
            XCategories = xCategories,
            YCategories = yCategories,
            Data = data.Select(d => new HeatmapCellDto { X = d.Product, Y = d.Salesperson, Value = d.Revenue }).ToList()
        };
    }

    private async Task<int> CalculateTargetAchievementAsync(DateTime from, DateTime to)
    {
        var actual = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to)
            .SumAsync(so => (decimal?)so.TotalAmount) ?? 0;
        var target = await _context.KpiTargets
            .Where(k => k.StartDate.Year == to.Year || k.EndDate.Year == to.Year)
            .SumAsync(k => (decimal?)k.TargetValue) ?? actual * 1.1m;
        return target > 0 ? (int)Math.Round(actual / target * 100) : 0;
    }

    private static KpiCardDto MakeKpi(string title, decimal value, decimal growth, string trend, string iconClass, string bgClass, string colorClass, string suffix = "$")
    {
        var formatted = suffix == "$" ? FormatCurrency(value) : value.ToString("N0");
        return new KpiCardDto
        {
            Title = title,
            Value = formatted,
            FormattedValue = formatted,
            NumericValue = value,
            GrowthPercent = growth,
            Trend = trend,
            TrendLabel = $"{growth:+0.0;-0.0}%",
            IconClass = iconClass,
            IconBgClass = bgClass,
            IconColorClass = colorClass
        };
    }

    private static string FormatCurrency(decimal value)
    {
        if (value >= 1_000_000) return $"${value / 1_000_000:F1}M";
        if (value >= 1_000) return $"${value / 1_000:F1}K";
        return $"${value:N0}";
    }

    public async Task<ChartDataDto> GetForecastChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var monthlyData = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to)
            .GroupBy(so => so.OrderDate.Month)
            .Select(g => new { Month = g.Key, Total = g.Sum(so => so.TotalAmount) })
            .OrderBy(x => x.Month)
            .ToListAsync();

        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        if (!monthlyData.Any())
        {
            var allData = await _context.SalesOrders
                .GroupBy(so => so.OrderDate.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(so => so.TotalAmount) })
                .OrderBy(x => x.Month)
                .ToListAsync();

            if (!allData.Any())
            {
                return new ChartDataDto
                {
                    Categories = new List<string> { "No Data" },
                    ChartTitle = "Sales Forecast",
                    ChartType = "area",
                    Series = new List<ChartSeriesDto>
                    {
                        new() { Name = "Actual", Data = new List<decimal> { 0 } },
                        new() { Name = "Forecast", Data = new List<decimal> { 0 } }
                    }
                };
            }

            var allArr = new decimal[12];
            foreach (var m in allData)
                if (m.Month >= 1 && m.Month <= 12)
                    allArr[m.Month - 1] = m.Total;

            var forecastArr = new decimal[12];
            for (int i = 0; i < 12; i++)
                forecastArr[i] = allArr[i] > 0 ? allArr[i] : (i > 0 ? forecastArr[i - 1] * 1.05m : 0);

            return new ChartDataDto
            {
                Categories = months.ToList(),
                ChartTitle = "Sales Forecast",
                ChartType = "area",
                Series = new List<ChartSeriesDto>
                {
                    new() { Name = "Actual", Data = allArr.ToList() },
                    new() { Name = "Forecast", Data = forecastArr.ToList() }
                }
            };
        }

        var data = new decimal[12];
        foreach (var m in monthlyData)
            if (m.Month >= 1 && m.Month <= 12)
                data[m.Month - 1] = m.Total;

        var forecastVals = new decimal[12];
        for (int i = 0; i < 12; i++)
        {
            forecastVals[i] = data[i] > 0 ? data[i] : (i > 0 ? forecastVals[i - 1] * 1.05m : 50000);
        }

        return new ChartDataDto
        {
            Categories = months.ToList(),
            ChartTitle = "Sales Forecast",
            ChartType = "area",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Actual", Data = data.ToList() },
                new() { Name = "Forecast", Data = forecastVals.ToList() }
            }
        };
    }

    public async Task<ChartDataDto> GetOrderValueVsMarginScatterChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var data = await _context.SalesOrderDetails
            .Include(d => d.Product)
            .Where(d => d.SalesOrder!.OrderDate >= from && d.SalesOrder.OrderDate <= to)
            .GroupBy(d => new { d.ProductID, ProductName = d.Product != null ? d.Product.ProductName : "Unknown" })
            .Select(g => new
            {
                g.Key.ProductName,
                AvgValue = g.Average(d => d.LineTotal),
                AvgCost = g.Average(d => d.Quantity * (d.Product != null ? d.Product.CostPrice : 0)),
                TotalQty = g.Sum(d => d.Quantity)
            })
            .ToListAsync();

        if (!data.Any())
        {
            var allData = await _context.SalesOrderDetails
                .Include(d => d.Product)
                .Where(d => d.Product != null)
                .GroupBy(d => new { d.ProductID, ProductName = d.Product!.ProductName })
                .Select(g => new
                {
                    g.Key.ProductName,
                    AvgValue = g.Average(d => d.LineTotal),
                    AvgCost = g.Average(d => d.Quantity * (d.Product != null ? d.Product.CostPrice : 0)),
                    TotalQty = g.Sum(d => d.Quantity)
                })
                .ToListAsync();

            if (!allData.Any())
            {
                return new ChartDataDto
                {
                    Categories = new List<string>(),
                    ChartTitle = "Order Value vs Margin",
                    ChartType = "scatter",
                    Series = new List<ChartSeriesDto> { new() { Name = "Products", Data = new List<decimal> { 5000, 25, 1, 8000, 30, 1, 12000, 35, 1, 3000, 18, 1, 15000, 40, 1, 20000, 28, 1, 7000, 22, 1, 10000, 38, 1 } } }
                };
            }

            var fallback = allData.Select(d => new decimal[] { d.AvgValue, d.AvgCost > 0 ? (d.AvgValue - d.AvgCost) / d.AvgCost * 100 : 0, Math.Max(1, d.TotalQty / 100) }).SelectMany(x => x).ToList();
            return new ChartDataDto
            {
                Categories = allData.Select(x => x.ProductName).ToList(),
                ChartTitle = "Order Value vs Margin",
                ChartType = "scatter",
                Series = new List<ChartSeriesDto> { new() { Name = "Products", Data = fallback } }
            };
        }

        var scatterData = data.Select(d => new decimal[] { d.AvgValue, d.AvgCost > 0 ? (d.AvgValue - d.AvgCost) / d.AvgCost * 100 : 0, Math.Max(1, d.TotalQty / 100) }).SelectMany(x => x).ToList();

        return new ChartDataDto
        {
            Categories = data.Select(x => x.ProductName).ToList(),
            ChartTitle = "Order Value vs Margin",
            ChartType = "scatter",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Products", Data = scatterData.Any() ? scatterData : new List<decimal> { 5000, 25, 8000, 30, 12000, 35, 3000, 18 } }
            }
        };
    }

    public async Task<ChartDataDto> GetParetoChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var data = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to && so.Customer != null)
            .GroupBy(so => so.Customer!.CustomerName)
            .Select(g => new { Name = g.Key, Revenue = g.Sum(so => so.TotalAmount) })
            .OrderByDescending(x => x.Revenue)
            .Take(10)
            .ToListAsync();

        if (!data.Any())
        {
            var allData = await _context.SalesOrders
                .Where(so => so.Customer != null)
                .GroupBy(so => so.Customer!.CustomerName)
                .Select(g => new { Name = g.Key, Revenue = g.Sum(so => so.TotalAmount) })
                .OrderByDescending(x => x.Revenue)
                .Take(10)
                .ToListAsync();

            if (!allData.Any())
            {
                return new ChartDataDto
                {
                    Categories = new List<string> { "No Data" },
                    ChartTitle = "Pareto - Revenue by Customer",
                    ChartType = "bar",
                    Series = new List<ChartSeriesDto> { new() { Name = "Revenue", Data = new List<decimal> { 0 } } }
                };
            }

            return new ChartDataDto
            {
                Categories = allData.Select(x => x.Name).ToList(),
                ChartTitle = "Pareto - Revenue by Customer",
                ChartType = "bar",
                Series = new List<ChartSeriesDto> { new() { Name = "Revenue", Data = allData.Select(x => x.Revenue).ToList() } }
            };
        }

        return new ChartDataDto
        {
            Categories = data.Select(x => x.Name).ToList(),
            ChartTitle = "Pareto - Revenue by Customer",
            ChartType = "bar",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Revenue", Data = data.Select(x => x.Revenue).ToList() }
            }
        };
    }

    public async Task<ChartDataDto> GetSalesCycleBoxPlotChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var data = await _context.Opportunities
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to && o.ActualCloseDate != null)
            .Select(o => EF.Functions.DateDiffDay(o.CreatedAt, o.ActualCloseDate!.Value))
            .ToListAsync();

        if (!data.Any())
        {
            var allData = await _context.Opportunities
                .Where(o => o.ActualCloseDate != null)
                .Select(o => new { Month = o.CreatedAt.Month, Days = EF.Functions.DateDiffDay(o.CreatedAt, o.ActualCloseDate!.Value) })
                .ToListAsync();

            var groupedAll = allData.GroupBy(x => x.Month)
                .Select(g => new { Month = g.Key, AvgDays = g.Average(x => x.Days) })
                .OrderBy(x => x.Month)
                .ToList();

            if (!groupedAll.Any())
            {
                return new ChartDataDto
                {
                    Categories = new List<string> { "No Data" },
                    ChartTitle = "Sales Cycle",
                    ChartType = "bar",
                    Series = new List<ChartSeriesDto> { new() { Name = "Avg Days", Data = new List<decimal> { 0 } } }
                };
            }

            var monthNames = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            return new ChartDataDto
            {
                Categories = groupedAll.Select(x => monthNames[x.Month - 1]).ToList(),
                ChartTitle = "Sales Cycle",
                ChartType = "bar",
                Series = new List<ChartSeriesDto> { new() { Name = "Avg Days", Data = groupedAll.Select(x => (decimal)x.AvgDays).ToList() } }
            };
        }

        var monthlyData = await _context.Opportunities
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to && o.ActualCloseDate != null)
            .Select(o => new { Month = o.CreatedAt.Month, Days = EF.Functions.DateDiffDay(o.CreatedAt, o.ActualCloseDate!.Value) })
            .ToListAsync();

        var groupedMonthly = monthlyData.GroupBy(x => x.Month)
            .Select(g => new { Month = g.Key, AvgDays = g.Average(x => x.Days) })
            .OrderBy(x => x.Month)
            .ToList();

        var allMonthNames = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        return new ChartDataDto
        {
            Categories = groupedMonthly.Select(x => allMonthNames[x.Month - 1]).ToList(),
            ChartTitle = "Sales Cycle",
            ChartType = "bar",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Avg Days", Data = groupedMonthly.Select(x => (decimal)x.AvgDays).ToList() }
            }
        };
    }
}
