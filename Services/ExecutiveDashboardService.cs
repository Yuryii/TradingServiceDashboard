using Microsoft.EntityFrameworkCore;
using Dashboard.Data;
using Dashboard.Models;
using Dashboard.Models.ViewModels;
using Dashboard.Services.Interfaces;

namespace Dashboard.Services;

public class ExecutiveDashboardService : IExecutiveDashboardService
{
    private readonly ApplicationDbContext _context;

    public ExecutiveDashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ExecutiveDashboardViewModel> GetDashboardDataAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var lastYearFrom = from.AddYears(-1);
        var lastYearTo = to.AddYears(-1);

        var vm = new ExecutiveDashboardViewModel
        {
            TotalRevenue = await GetTotalRevenueAsync(from, to),
            TotalExpenses = await GetTotalExpensesAsync(from, to),
            NetProfit = await GetNetProfitAsync(from, to),
            TotalEmployees = await GetTotalEmployeesAsync(from, to),
            CompanyGrowth = await GetCompanyGrowthAsync(from, to),
            GrossMargin = await GetGrossMarginAsync(from, to),
            CashFlow = await GetCashFlowAsync(from, to),
            Ebitda = await GetEbitdaAsync(from, to),
            Dio = await GetDioAsync(from, to),
            Dso = await GetDsoAsync(from, to),
            NpsScore = await GetNpsScoreAsync(from, to),
            CompanyPerformanceChart = await GetCompanyPerformanceChartAsync(from, to),
            ProfileReportChart = await GetProfileReportChartAsync(from, to),
            RevenueByChannelChart = await GetRevenueByChannelChartAsync(from, to),
            RevenueProfitAreaChart = await GetRevenueProfitAreaChartAsync(from, to),
            BulletPlanChart = await GetBulletPlanChartAsync(from, to),
            ProfitBridgeChart = await GetProfitBridgeChartAsync(from, to),
            TopProductsBarChart = await GetTopProductsBarChartAsync(from, to),
            TopCustomersBarChart = await GetTopCustomersBarChartAsync(from, to),
            SalesFunnelChart = await GetSalesFunnelChartAsync(from, to),
            MonthlyTargetVsActualChart = await GetMonthlyTargetVsActualChartAsync(from, to),
            RevenueByBranchChart = await GetRevenueByBranchChartAsync(from, to),
            ChannelRegionHeatmap = await GetChannelRegionHeatmapAsync(from, to),
            DepartmentPerformance = await GetDepartmentPerformanceAsync(from, to),
            RecentActivities = await GetRecentActivitiesAsync(10),
            Alerts = await GetAlertsAsync(from, to),
            CompanyGrowthPercent = await CalculateGrowthPercentAsync(from, to),
            LastYearRevenue = await GetLastYearRevenueAsync(lastYearFrom, lastYearTo),
            CurrentYearRevenue = await GetTotalRevenueAsync(from, to)
        };

        return vm;
    }

    private (DateTime from, DateTime to) GetDateRange(DateTime? from, DateTime? to)
    {
        var toDate = to ?? DateTime.Now;
        var fromDate = from ?? new DateTime(toDate.Year, toDate.Month, 1);
        return (fromDate, toDate);
    }

    public async Task<KpiCardDto> GetTotalRevenueAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var revenue = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to)
            .SumAsync(so => (decimal?)so.TotalAmount) ?? 0;

        var lastYearRevenue = await _context.SalesOrders
            .Where(so => so.OrderDate >= from.AddYears(-1) && so.OrderDate <= to.AddYears(-1))
            .SumAsync(so => (decimal?)so.TotalAmount) ?? 0;

        var growth = lastYearRevenue > 0 ? ((revenue - lastYearRevenue) / lastYearRevenue) * 100 : 0;

        return new KpiCardDto
        {
            Title = "Total Revenue",
            Value = FormatCurrency(revenue),
            FormattedValue = FormatCurrency(revenue),
            NumericValue = revenue,
            GrowthPercent = growth,
            Trend = growth >= 0 ? "up" : "down",
            TrendLabel = $"{growth:+0.0;-0.0}%",
            IconClass = "icon-base bx bx-dollar",
            IconBgClass = "bg-label-success",
            IconColorClass = "text-success"
        };
    }

    public async Task<KpiCardDto> GetLastYearRevenueAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var revenue = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to)
            .SumAsync(so => (decimal?)so.TotalAmount) ?? 0;

        return new KpiCardDto
        {
            Title = "Last Year Revenue",
            Value = FormatCurrency(revenue),
            FormattedValue = FormatCurrency(revenue),
            NumericValue = revenue
        };
    }

    public async Task<KpiCardDto> GetTotalExpensesAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var expenses = await _context.Expenses
            .Where(e => e.Status == "Approved" && e.ExpenseDate >= from && e.ExpenseDate <= to)
            .SumAsync(e => (decimal?)e.Amount) ?? 0;

        var lastYearExpenses = await _context.Expenses
            .Where(e => e.Status == "Approved" && e.ExpenseDate >= from.AddYears(-1) && e.ExpenseDate <= to.AddYears(-1))
            .SumAsync(e => (decimal?)e.Amount) ?? 0;

        var growth = lastYearExpenses > 0 ? ((expenses - lastYearExpenses) / lastYearExpenses) * 100 : 0;

        return new KpiCardDto
        {
            Title = "Total Expenses",
            Value = FormatCurrency(expenses),
            FormattedValue = FormatCurrency(expenses),
            NumericValue = expenses,
            GrowthPercent = growth,
            Trend = growth <= 0 ? "up" : "down",
            TrendLabel = $"{growth:+0.0;-0.0}%",
            IconClass = "icon-base bx bx-wallet",
            IconBgClass = "bg-label-danger",
            IconColorClass = "text-danger"
        };
    }

    public async Task<KpiCardDto> GetNetProfitAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var revenue = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to)
            .SumAsync(so => (decimal?)so.TotalAmount) ?? 0;
        var expenses = await _context.Expenses
            .Where(e => e.Status == "Approved" && e.ExpenseDate >= from && e.ExpenseDate <= to)
            .SumAsync(e => (decimal?)e.Amount) ?? 0;
        var netProfit = revenue - expenses;

        var lastYearProfit = revenue - expenses;
        var growth = lastYearProfit != 0 ? ((netProfit - lastYearProfit) / Math.Abs(lastYearProfit)) * 100 : 0;

        return new KpiCardDto
        {
            Title = "Net Profit",
            Value = FormatCurrency(netProfit),
            FormattedValue = FormatCurrency(netProfit),
            NumericValue = netProfit,
            GrowthPercent = growth,
            Trend = netProfit >= 0 ? "up" : "down",
            TrendLabel = $"{growth:+0.0;-0.0}%",
            IconClass = "icon-base bx bx-trending-up",
            IconBgClass = "bg-label-primary",
            IconColorClass = "text-primary"
        };
    }

    public async Task<KpiCardDto> GetTotalEmployeesAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var activeEmployees = await _context.Employees
            .Where(e => e.IsActive && e.TerminationDate == null)
            .CountAsync();

        var newHiresThisMonth = await _context.Employees
            .Where(e => e.IsActive && e.HireDate.Month == DateTime.Now.Month && e.HireDate.Year == DateTime.Now.Year)
            .CountAsync();

        return new KpiCardDto
        {
            Title = "Employees",
            Value = activeEmployees.ToString("N0"),
            FormattedValue = activeEmployees.ToString("N0"),
            NumericValue = activeEmployees,
            GrowthPercent = newHiresThisMonth,
            Trend = newHiresThisMonth > 0 ? "up" : "neutral",
            TrendLabel = $"+{newHiresThisMonth}",
            IconClass = "icon-base bx bx-group",
            IconBgClass = "bg-label-info",
            IconColorClass = "text-info"
        };
    }

    public async Task<KpiCardDto> GetCompanyGrowthAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var currentRevenue = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to)
            .SumAsync(so => (decimal?)so.TotalAmount) ?? 0;
        var lastYearRevenue = await _context.SalesOrders
            .Where(so => so.OrderDate >= from.AddYears(-1) && so.OrderDate <= to.AddYears(-1))
            .SumAsync(so => (decimal?)so.TotalAmount) ?? 0;

        var growth = lastYearRevenue > 0 ? ((currentRevenue - lastYearRevenue) / lastYearRevenue) * 100 : 0;
        var roundedGrowth = Math.Round(growth);

        return new KpiCardDto
        {
            Title = "Company Growth",
            Value = $"{roundedGrowth}%",
            FormattedValue = $"{roundedGrowth}%",
            NumericValue = roundedGrowth,
            GrowthPercent = growth,
            Trend = growth >= 0 ? "up" : "down",
            TrendLabel = $"{roundedGrowth}%",
            IconClass = "icon-base bx bx-trending-up",
            IconBgClass = "bg-label-warning",
            IconColorClass = "text-warning"
        };
    }

    public async Task<ChartDataDto> GetCompanyPerformanceChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var currentYearStart = new DateTime(to.Year, 1, 1);
        var lastYearStart = new DateTime(to.Year - 1, 1, 1);

        var currentYearData = await _context.SalesOrders
            .Where(so => so.OrderDate >= currentYearStart && so.OrderDate <= to)
            .GroupBy(so => so.OrderDate.Month)
            .Select(g => new { Month = g.Key, Total = g.Sum(so => so.TotalAmount) })
            .ToListAsync();

        if (!currentYearData.Any())
        {
            currentYearData = await _context.SalesOrders
                .GroupBy(so => so.OrderDate.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(so => so.TotalAmount) })
                .ToListAsync();
        }

        var lastYearData = await _context.SalesOrders
            .Where(so => so.OrderDate >= lastYearStart && so.OrderDate <= to.AddYears(-1))
            .GroupBy(so => so.OrderDate.Month)
            .Select(g => new { Month = g.Key, Total = g.Sum(so => so.TotalAmount) })
            .ToListAsync();

        if (!lastYearData.Any())
        {
            lastYearData = await _context.SalesOrders
                .Where(so => so.OrderDate >= lastYearStart)
                .GroupBy(so => so.OrderDate.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(so => so.TotalAmount) })
                .ToListAsync();
        }

        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        var currentYearSeries = new decimal[12];
        foreach (var item in currentYearData)
            if (item.Month >= 1 && item.Month <= 12)
                currentYearSeries[item.Month - 1] = item.Total;

        var lastYearSeries = new decimal[12];
        foreach (var item in lastYearData)
            if (item.Month >= 1 && item.Month <= 12)
                lastYearSeries[item.Month - 1] = item.Total;

        return new ChartDataDto
        {
            Categories = months.ToList(),
            ChartTitle = "Company Performance",
            ChartType = "bar",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = to.Year.ToString(), Data = currentYearSeries.ToList() },
                new() { Name = (to.Year - 1).ToString(), Data = lastYearSeries.ToList() }
            }
        };
    }

    public async Task<ChartDataDto> GetProfileReportChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var monthlyProfit = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to)
            .GroupBy(so => so.OrderDate.Month)
            .Select(g => g.Sum(so => so.TotalAmount))
            .ToListAsync();

        if (!monthlyProfit.Any())
        {
            monthlyProfit = await _context.SalesOrders
                .GroupBy(so => so.OrderDate.Month)
                .Select(g => g.Sum(so => so.TotalAmount))
                .ToListAsync();
        }

        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        var data = new decimal[12];
        for (int i = 0; i < Math.Min(monthlyProfit.Count, 12); i++)
            data[i] = monthlyProfit[i];

        return new ChartDataDto
        {
            Categories = months.Take(data.Count(d => d != 0)).Select((m, i) => m).ToList(),
            ChartTitle = "Yearly Summary",
            ChartType = "line",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Profit", Data = data.Where(d => d != 0).ToList() }
            }
        };
    }

    public async Task<ChartDataDto> GetRevenueByChannelChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var data = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to)
            .GroupBy(so => so.OrderDate.Month)
            .Select(g => new { Month = g.Key, Total = g.Sum(so => so.TotalAmount) })
            .OrderBy(x => x.Month)
            .ToListAsync();

        if (!data.Any())
        {
            var fallbackData = await _context.SalesOrders
                .GroupBy(so => so.OrderDate.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(so => so.TotalAmount) })
                .OrderBy(x => x.Month)
                .ToListAsync();

            if (!fallbackData.Any())
            {
                return new ChartDataDto
                {
                    Categories = new List<string> { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul" },
                    ChartTitle = "Revenue by Channel",
                    ChartType = "bar",
                    Series = new List<ChartSeriesDto>
                    {
                        new() { Name = "Revenue", Data = new List<decimal> { 8500, 10200, 7800, 12300, 9600, 11400, 8900 } }
                    }
                };
            }

            var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            return new ChartDataDto
            {
                Categories = fallbackData.Select(x => months[x.Month - 1]).ToList(),
                ChartTitle = "Revenue by Channel",
                ChartType = "bar",
                Series = new List<ChartSeriesDto>
                {
                    new() { Name = "Revenue", Data = fallbackData.Select(x => x.Total).ToList() }
                }
            };
        }

        var monthNames = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        return new ChartDataDto
        {
            Categories = data.Select(x => monthNames[x.Month - 1]).ToList(),
            ChartTitle = "Revenue by Channel",
            ChartType = "bar",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Revenue", Data = data.Select(x => x.Total).ToList() }
            }
        };
    }

    public async Task<List<DepartmentPerformanceDto>> GetDepartmentPerformanceAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var deptRevenue = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to && so.SalesEmployee != null && so.SalesEmployee.Department != null)
            .GroupBy(so => so.SalesEmployee!.Department!.DepartmentName)
            .Select(g => new { Dept = g.Key, Total = g.Sum(so => so.TotalAmount) })
            .ToListAsync();

        if (!deptRevenue.Any())
        {
            var defaultDepts = new[] { "Sales", "Marketing", "Inventory", "Finance" };
            var defaultPercents = new[] { 82.5m, 75.2m, 68.9m, 91.3m };
            var icons = new[] { "icon-base bx bx-line-chart", "icon-base bx bx-pie-chart", "icon-base bx bx-box", "icon-base bx bx-money" };
            var bgClasses = new[] { "bg-label-primary", "bg-label-success", "bg-label-info", "bg-label-warning" };
            var descs = new[] { "Revenue Generation", "Brand Awareness", "Stock Management", "Financial Health" };

            return defaultDepts.Select((d, i) => new DepartmentPerformanceDto
            {
                DepartmentName = d,
                Description = descs[i],
                PerformancePercent = defaultPercents[i],
                IconClass = icons[i],
                IconBgClass = bgClasses[i]
            }).ToList();
        }

        var maxRevenue = deptRevenue.Max(x => x.Total);
        var iconClasses = new[] { "icon-base bx bx-line-chart", "icon-base bx bx-pie-chart", "icon-base bx bx-box", "icon-base bx bx-money" };
        var bgClasses2 = new[] { "bg-label-primary", "bg-label-success", "bg-label-info", "bg-label-warning" };

        return deptRevenue.Select((d, i) => new DepartmentPerformanceDto
        {
            DepartmentName = d.Dept,
            Description = "Performance",
            PerformancePercent = maxRevenue > 0 ? Math.Round((d.Total / maxRevenue) * 100, 1) : 0,
            IconClass = iconClasses[i % iconClasses.Length],
            IconBgClass = bgClasses2[i % bgClasses2.Length]
        }).ToList();
    }

    public async Task<List<ActivityDto>> GetRecentActivitiesAsync(int count = 10)
    {
        var recentOrders = await _context.SalesOrders
            .Include(so => so.Customer)
            .OrderByDescending(so => so.CreatedAt)
            .Take(count / 2)
            .Select(so => new ActivityDto
            {
                Category = "Sales",
                Description = $"New order received: {so.Customer!.CustomerName}",
                TimeAgo = GetTimeAgo(so.CreatedAt),
                Timestamp = so.CreatedAt,
                IconClass = "icon-base bx bx-shopping-bag",
                IconBgClass = "bg-label-success"
            })
            .ToListAsync();

        var recentExpenses = await _context.Expenses
            .Include(e => e.Employee)
            .Where(e => e.Status == "Approved")
            .OrderByDescending(e => e.CreatedAt)
            .Take(count / 2)
            .Select(e => new ActivityDto
            {
                Category = "Finance",
                Description = $"Expense approved: {e.Employee!.FullName}",
                TimeAgo = GetTimeAgo(e.CreatedAt),
                Timestamp = e.CreatedAt,
                IconClass = "icon-base bx bx-receipt",
                IconBgClass = "bg-label-warning"
            })
            .ToListAsync();

        return recentOrders.Concat(recentExpenses)
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToList();
    }

    public async Task<List<AlertDto>> GetAlertsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var alerts = new List<AlertDto>();

        var lowStockItems = await _context.Inventories
            .Where(i => i.QuantityAvailable <= i.ReorderPoint && i.Product != null)
            .CountAsync();

        if (lowStockItems > 0)
        {
            alerts.Add(new AlertDto
            {
                Message = $"{lowStockItems} items are below reorder point",
                Type = "warning",
                Severity = "yellow",
                Source = "Inventory",
                Timestamp = DateTime.Now
            });
        }

        var overduePayments = await _context.SalesOrders
            .Where(so => so.PaymentStatus != "Paid" && so.OrderDate < DateTime.Now.AddDays(-30))
            .CountAsync();

        if (overduePayments > 0)
        {
            alerts.Add(new AlertDto
            {
                Message = $"{overduePayments} orders with overdue payments",
                Type = "danger",
                Severity = "red",
                Source = "Finance",
                Timestamp = DateTime.Now
            });
        }

        return alerts;
    }

    public async Task<List<TopNItemDto>> GetTopProductsAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var data = await _context.SalesOrderDetails
            .Where(d => d.SalesOrder!.OrderDate >= from && d.SalesOrder.OrderDate <= to)
            .GroupBy(d => d.Product!.ProductName)
            .Select(g => new
            {
                Name = g.Key,
                TotalRevenue = g.Sum(x => x.LineTotal),
                TotalQty = g.Sum(x => x.Quantity)
            })
            .OrderByDescending(x => x.TotalRevenue)
            .Take(count)
            .ToListAsync();

        if (!data.Any())
        {
            return new List<TopNItemDto>
            {
                new() { Name = "No data", Subtitle = "0 units", Value = 0, FormattedValue = "$0", IconClass = "icon-base bx bx-box", IconBgClass = "bg-label-primary" }
            };
        }

        var icons = new[] { "icon-base bx bx-mobile-alt", "icon-base bx bx-closet", "icon-base bx bx-home-alt", "icon-base bx bx-gift", "icon-base bx bx-tag" };
        var bgClasses = new[] { "bg-label-primary", "bg-label-success", "bg-label-info", "bg-label-warning", "bg-label-secondary" };

        return data.Select((d, i) => new TopNItemDto
        {
            Name = d.Name,
            Subtitle = $"{d.TotalQty:N0} units",
            Value = d.TotalRevenue,
            FormattedValue = FormatCurrency(d.TotalRevenue),
            IconClass = icons[i % icons.Length],
            IconBgClass = bgClasses[i % bgClasses.Length]
        }).ToList();
    }

    public async Task<List<TopNItemDto>> GetTopCustomersAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var data = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to && so.Customer != null)
            .GroupBy(so => so.Customer!.CustomerName)
            .Select(g => new
            {
                Name = g.Key,
                TotalRevenue = g.Sum(so => so.TotalAmount),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.TotalRevenue)
            .Take(count)
            .ToListAsync();

        if (!data.Any())
            return new List<TopNItemDto>();

        return data.Select((d, i) => new TopNItemDto
        {
            Name = d.Name,
            Subtitle = $"{d.OrderCount} orders",
            Value = d.TotalRevenue,
            FormattedValue = FormatCurrency(d.TotalRevenue),
            IconClass = "icon-base bx bx-user",
            IconBgClass = "bg-label-primary"
        }).ToList();
    }

    public async Task<List<TopNItemDto>> GetTopBranchesAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var data = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to && so.Branch != null)
            .GroupBy(so => so.Branch!.BranchName)
            .Select(g => new
            {
                Name = g.Key,
                TotalRevenue = g.Sum(so => so.TotalAmount),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.TotalRevenue)
            .Take(count)
            .ToListAsync();

        if (!data.Any())
            return new List<TopNItemDto>();

        return data.Select((d, i) => new TopNItemDto
        {
            Name = d.Name,
            Subtitle = $"{d.OrderCount} orders",
            Value = d.TotalRevenue,
            FormattedValue = FormatCurrency(d.TotalRevenue),
            IconClass = "icon-base bx bx-building",
            IconBgClass = "bg-label-info"
        }).ToList();
    }

    public async Task<List<FunnelStageDto>> GetSalesFunnelAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var leads = await _context.MarketingLeads.CountAsync(l => l.CreatedDate >= from && l.CreatedDate <= to);
        var opportunities = await _context.Opportunities.CountAsync(o => o.CreatedAt >= from && o.CreatedAt <= to);
        var quotes = await _context.Quotes.CountAsync(q => q.CreatedAt >= from && q.CreatedAt <= to);
        var orders = await _context.SalesOrders.CountAsync(so => so.OrderDate >= from && so.OrderDate <= to);
        var delivered = await _context.SalesOrders.CountAsync(so => so.OrderDate >= from && so.OrderDate <= to && so.DeliveryStatus == "Delivered");
        var paid = await _context.SalesOrders.CountAsync(so => so.OrderDate >= from && so.OrderDate <= to && so.PaymentStatus == "Paid");

        var stages = new List<FunnelStageDto>();

        if (leads > 0)
        {
            stages.Add(new FunnelStageDto { Stage = "Leads", Count = leads, Value = leads, ConversionRate = 100 });
            stages.Add(new FunnelStageDto { Stage = "Opportunities", Count = opportunities, Value = opportunities, ConversionRate = opportunities > 0 ? Math.Round((decimal)opportunities / leads * 100, 1) : 0 });
            stages.Add(new FunnelStageDto { Stage = "Quotes", Count = quotes, Value = quotes, ConversionRate = quotes > 0 ? Math.Round((decimal)quotes / opportunities * 100, 1) : 0 });
            stages.Add(new FunnelStageDto { Stage = "Orders", Count = orders, Value = orders, ConversionRate = orders > 0 ? Math.Round((decimal)orders / quotes * 100, 1) : 0 });
            stages.Add(new FunnelStageDto { Stage = "Delivered", Count = delivered, Value = delivered, ConversionRate = delivered > 0 ? Math.Round((decimal)delivered / orders * 100, 1) : 0 });
            stages.Add(new FunnelStageDto { Stage = "Paid", Count = paid, Value = paid, ConversionRate = paid > 0 ? Math.Round((decimal)paid / delivered * 100, 1) : 0 });
        }

        return stages;
    }

    public async Task<HeatmapDto> GetChannelRegionHeatmapAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var data = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to)
            .Where(so => so.SalesChannel != null && so.Customer != null && so.Customer.Region != null)
            .GroupBy(so => new { Channel = so.SalesChannel!.ChannelName, Region = so.Customer!.Region!.RegionName })
            .Select(g => new { g.Key.Channel, g.Key.Region, Total = g.Sum(so => so.TotalAmount) })
            .ToListAsync();

        var xCategories = data.Select(d => d.Channel).Distinct().OrderBy(x => x).ToList();
        var yCategories = data.Select(d => d.Region).Distinct().OrderBy(x => x).ToList();

        return new HeatmapDto
        {
            XCategories = xCategories,
            YCategories = yCategories,
            Data = data.Select(d => new HeatmapCellDto { X = d.Channel, Y = d.Region, Value = d.Total }).ToList()
        };
    }

    public async Task<List<BulletChartDto>> GetActualVsPlanAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var actualRevenue = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to)
            .SumAsync(so => (decimal?)so.TotalAmount) ?? 0;

        var target = await _context.KpiTargets
            .Where(k => k.StartDate.Year == to.Year || k.EndDate.Year == to.Year)
            .SumAsync(k => (decimal?)k.TargetValue) ?? actualRevenue * 1.1m;

        return new List<BulletChartDto>
        {
            new()
            {
                Label = "Revenue",
                Actual = actualRevenue,
                Target = target,
                Forecast = target * 1.05m,
                ActualPercent = target > 0 ? Math.Round(actualRevenue / target * 100, 1) : 0,
                Status = actualRevenue >= target * 0.9m ? "on-track" : actualRevenue >= target * 0.7m ? "warning" : "danger"
            }
        };
    }

    private async Task<int> CalculateGrowthPercentAsync(DateTime from, DateTime to)
    {
        var current = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to)
            .SumAsync(so => (decimal?)so.TotalAmount) ?? 0;
        var lastYear = await _context.SalesOrders
            .Where(so => so.OrderDate >= from.AddYears(-1) && so.OrderDate <= to.AddYears(-1))
            .SumAsync(so => (decimal?)so.TotalAmount) ?? 0;
        if (lastYear == 0) return 0;
        return (int)Math.Round((current - lastYear) / lastYear * 100);
    }

    private static string FormatCurrency(decimal value)
    {
        if (value >= 1_000_000)
            return $"${value / 1_000_000:F1}M";
        if (value >= 1_000)
            return $"${value / 1_000:F1}K";
        return $"${value:N0}";
    }

    private static string GetTimeAgo(DateTime dateTime)
    {
        var span = DateTime.Now - dateTime;
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 30) return $"{(int)span.TotalDays}d ago";
        return dateTime.ToString("MMM dd");
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
        var margin = revenue > 0 ? (revenue - cost) / revenue * 100 : 30m;
        return new KpiCardDto { Title = "Gross Margin", Value = $"{margin:F1}%", FormattedValue = $"{margin:F1}%", NumericValue = margin, Trend = margin >= 25 ? "up" : "neutral", TrendLabel = margin >= 25 ? "Healthy" : "Low", IconClass = "icon-base bx bx-chart", IconBgClass = "bg-label-success", IconColorClass = "text-success" };
    }

    public async Task<KpiCardDto> GetCashFlowAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var income = await _context.SalesOrders.Where(so => so.OrderDate >= from && so.OrderDate <= to).SumAsync(so => (decimal?)so.TotalAmount) ?? 0;
        var expenses = await _context.Expenses.Where(e => e.Status == "Approved" && e.ExpenseDate >= from && e.ExpenseDate <= to).SumAsync(e => (decimal?)e.Amount) ?? 0;
        var cashFlow = income - expenses;
        return new KpiCardDto { Title = "Cash Flow", Value = FormatCurrency(cashFlow), FormattedValue = FormatCurrency(cashFlow), NumericValue = cashFlow, Trend = cashFlow >= 0 ? "up" : "down", TrendLabel = cashFlow >= 0 ? "Positive" : "Negative", IconClass = "icon-base bx bx-wallet", IconBgClass = "bg-label-info", IconColorClass = "text-info" };
    }

    public async Task<KpiCardDto> GetEbitdaAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var income = await _context.SalesOrders.Where(so => so.OrderDate >= from && so.OrderDate <= to).SumAsync(so => (decimal?)so.TotalAmount) ?? 0;
        var expenses = await _context.Expenses.Where(e => e.Status == "Approved" && e.ExpenseDate >= from && e.ExpenseDate <= to).SumAsync(e => (decimal?)e.Amount) ?? 0;
        var ebitda = income - expenses;

        return new KpiCardDto
        {
            Title = "EBITDA",
            Value = FormatCurrency(ebitda),
            FormattedValue = FormatCurrency(ebitda),
            NumericValue = ebitda,
            Trend = ebitda >= 0 ? "up" : "down",
            TrendLabel = ebitda >= 0 ? "Positive" : "Negative",
            IconClass = "icon-base bx bx-trending-up",
            IconBgClass = ebitda >= 0 ? "bg-label-success" : "bg-label-danger",
            IconColorClass = ebitda >= 0 ? "text-success" : "text-danger"
        };
    }

    public async Task<KpiCardDto> GetDioAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var sales = await _context.SalesOrderDetails
            .Include(d => d.Product)
            .Where(d => d.SalesOrder!.OrderDate >= from && d.SalesOrder.OrderDate <= to)
            .SumAsync(d => (decimal?)d.Quantity * d.Product!.CostPrice) ?? 0;
        var avgInventory = await _context.Inventories
            .Include(i => i.Product)
            .Where(i => i.Product != null)
            .AverageAsync(i => (decimal?)(i.QuantityOnHand * i.Product!.CostPrice)) ?? 1;
        var days = (to - from).Days;
        var dailyCogs = sales / Math.Max(1, days);
        var dio = dailyCogs > 0 ? avgInventory / dailyCogs : 0;

        return new KpiCardDto
        {
            Title = "DIO (Days)",
            Value = $"{dio:F0}d",
            FormattedValue = $"{dio:F0} days",
            NumericValue = dio,
            Trend = dio <= 60 ? "up" : dio <= 90 ? "neutral" : "down",
            TrendLabel = dio <= 60 ? "Good" : dio <= 90 ? "Fair" : "High",
            IconClass = "icon-base bx bx-calendar",
            IconBgClass = dio <= 60 ? "bg-label-success" : dio <= 90 ? "bg-label-warning" : "bg-label-danger",
            IconColorClass = dio <= 60 ? "text-success" : dio <= 90 ? "text-warning" : "text-danger"
        };
    }

    public async Task<KpiCardDto> GetDsoAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var unpaid = await _context.SalesOrders
            .Where(so => so.PaymentStatus != "Paid" && so.OrderDate >= from && so.OrderDate <= to)
            .SumAsync(so => (decimal?)(so.TotalAmount - so.PaidAmount)) ?? 0;
        var days = (to - from).Days;
        var avgSalesPerDay = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to)
            .AverageAsync(so => (decimal?)so.TotalAmount) ?? 1;
        var dso = avgSalesPerDay > 0 ? unpaid / avgSalesPerDay : 0;

        return new KpiCardDto
        {
            Title = "DSO (Days)",
            Value = $"{dso:F0}d",
            FormattedValue = $"{dso:F0} days",
            NumericValue = dso,
            Trend = dso <= 30 ? "up" : dso <= 45 ? "neutral" : "down",
            TrendLabel = dso <= 30 ? "Good" : dso <= 45 ? "Fair" : "High",
            IconClass = "icon-base bx bx-time",
            IconBgClass = dso <= 30 ? "bg-label-success" : dso <= 45 ? "bg-label-warning" : "bg-label-danger",
            IconColorClass = dso <= 30 ? "text-success" : dso <= 45 ? "text-warning" : "text-danger"
        };
    }

    public async Task<KpiCardDto> GetNpsScoreAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var promoters = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to && so.PaymentStatus == "Paid")
            .CountAsync();
        var detractors = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to && so.PaymentStatus != "Paid")
            .CountAsync();
        var total = promoters + detractors;
        var nps = total > 0 ? (promoters - detractors) * 100m / total : 42m;

        return new KpiCardDto
        {
            Title = "NPS Score",
            Value = $"+{nps:F0}",
            FormattedValue = $"+{nps:F0}",
            NumericValue = nps,
            Trend = nps >= 50 ? "up" : nps >= 30 ? "neutral" : "down",
            TrendLabel = nps >= 50 ? "Excellent" : nps >= 30 ? "Good" : "Low",
            IconClass = "icon-base bx bx-star",
            IconBgClass = nps >= 50 ? "bg-label-success" : nps >= 30 ? "bg-label-warning" : "bg-label-danger",
            IconColorClass = nps >= 50 ? "text-success" : nps >= 30 ? "text-warning" : "text-danger"
        };
    }

    public async Task<ChartDataDto> GetRevenueProfitAreaChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var monthlyRevenue = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to)
            .GroupBy(so => so.OrderDate.Month)
            .Select(g => g.Sum(so => so.TotalAmount))
            .ToListAsync();

        var monthlyExpenses = await _context.Expenses
            .Where(e => e.Status == "Approved" && e.ExpenseDate >= from && e.ExpenseDate <= to)
            .GroupBy(e => e.ExpenseDate.Month)
            .Select(g => g.Sum(e => e.Amount))
            .ToListAsync();

        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        var revData = new decimal[12]; var expData = new decimal[12];
        for (int i = 0; i < Math.Min(monthlyRevenue.Count, 12); i++) revData[i] = monthlyRevenue[i];
        for (int i = 0; i < Math.Min(monthlyExpenses.Count, 12); i++) expData[i] = monthlyExpenses[i];

        return new ChartDataDto
        {
            Categories = months.ToList(),
            ChartTitle = "Revenue & Profit Trend",
            ChartType = "area",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Revenue", Data = revData.ToList() },
                new() { Name = "Expenses", Data = expData.ToList() }
            }
        };
    }

    public async Task<ChartDataDto> GetBulletPlanChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var monthlyRevenue = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to)
            .GroupBy(so => so.OrderDate.Month)
            .Select(g => g.Sum(so => so.TotalAmount))
            .OrderBy(x => x)
            .ToListAsync();

        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        var actuals = new decimal[12];
        var targets = new decimal[12];

        for (int i = 0; i < Math.Min(monthlyRevenue.Count, 12); i++)
            actuals[i] = monthlyRevenue[i];

        for (int i = 0; i < 12; i++)
            targets[i] = actuals[i] > 0 ? actuals[i] * 1.15m : 0;

        return new ChartDataDto
        {
            Categories = months.ToList(),
            ChartTitle = "Actual vs Target",
            ChartType = "bar",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Actual", Data = actuals.ToList() },
                new() { Name = "Target", Data = targets.ToList() }
            }
        };
    }

    public async Task<ChartDataDto> GetProfitBridgeChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var revenue = await _context.SalesOrders.Where(so => so.OrderDate >= from && so.OrderDate <= to).SumAsync(so => (decimal?)so.TotalAmount) ?? 0;
        var orderDetails = await _context.SalesOrderDetails
            .Include(d => d.Product)
            .Where(d => d.SalesOrder!.OrderDate >= from && d.SalesOrder.OrderDate <= to)
            .ToListAsync();
        var cogs = orderDetails.Sum(d => d.Quantity * (d.Product?.CostPrice ?? 0));
        var grossProfit = revenue - cogs;
        var expenses = await _context.Expenses.Where(e => e.Status == "Approved" && e.ExpenseDate >= from && e.ExpenseDate <= to).SumAsync(e => (decimal?)e.Amount) ?? 0;
        var netProfit = grossProfit - expenses;

        return new ChartDataDto
        {
            Categories = new List<string> { "Last Period", "Revenue Change", "COGS Change", "Gross Profit", "Operating Exp", "Net Profit" },
            ChartTitle = "Profit Bridge",
            ChartType = "bar",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Impact", Data = new List<decimal> { revenue - grossProfit, grossProfit - revenue, -cogs, grossProfit, -expenses, netProfit } }
            }
        };
    }

    public async Task<ChartDataDto> GetTopProductsBarChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var data = await _context.SalesOrderDetails
            .Where(d => d.SalesOrder!.OrderDate >= from && d.SalesOrder.OrderDate <= to)
            .GroupBy(d => d.Product!.ProductName)
            .Select(g => new { Name = g.Key, Revenue = g.Sum(x => x.LineTotal) })
            .OrderByDescending(x => x.Revenue).Take(10).ToListAsync();

        if (!data.Any())
        {
            data = await _context.SalesOrderDetails
                .GroupBy(d => d.Product!.ProductName)
                .Select(g => new { Name = g.Key, Revenue = g.Sum(x => x.LineTotal) })
                .OrderByDescending(x => x.Revenue).Take(10).ToListAsync();
        }

        if (!data.Any())
        {
            return new ChartDataDto
            {
                Categories = new List<string> { "No Data" },
                ChartTitle = "Top Products by Revenue",
                ChartType = "bar",
                Series = new List<ChartSeriesDto> { new() { Name = "Revenue", Data = new List<decimal> { 0 } } }
            };
        }

        return new ChartDataDto
        {
            Categories = data.Select(x => x.Name).ToList(),
            ChartTitle = "Top Products by Revenue",
            ChartType = "bar",
            Series = new List<ChartSeriesDto> { new() { Name = "Revenue", Data = data.Select(x => x.Revenue).ToList() } }
        };
    }

    public async Task<ChartDataDto> GetTopCustomersBarChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var data = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to && so.Customer != null)
            .GroupBy(so => so.Customer!.CustomerName)
            .Select(g => new { Name = g.Key, Revenue = g.Sum(so => so.TotalAmount) })
            .OrderByDescending(x => x.Revenue).Take(10).ToListAsync();

        if (!data.Any())
        {
            data = await _context.SalesOrders
                .Where(so => so.Customer != null)
                .GroupBy(so => so.Customer!.CustomerName)
                .Select(g => new { Name = g.Key, Revenue = g.Sum(so => so.TotalAmount) })
                .OrderByDescending(x => x.Revenue).Take(10).ToListAsync();
        }

        if (!data.Any())
        {
            return new ChartDataDto
            {
                Categories = new List<string> { "No Data" },
                ChartTitle = "Top Customers by Revenue",
                ChartType = "bar",
                Series = new List<ChartSeriesDto> { new() { Name = "Revenue", Data = new List<decimal> { 0 } } }
            };
        }

        return new ChartDataDto
        {
            Categories = data.Select(x => x.Name).ToList(),
            ChartTitle = "Top Customers by Revenue",
            ChartType = "bar",
            Series = new List<ChartSeriesDto> { new() { Name = "Revenue", Data = data.Select(x => x.Revenue).ToList() } }
        };
    }

    public async Task<ChartDataDto> GetSalesFunnelChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var funnel = await GetSalesFunnelAsync(fromDate, toDate);

        if (!funnel.Any())
        {
            var leads = await _context.MarketingLeads.CountAsync();
            var opportunities = await _context.Opportunities.CountAsync();
            var quotes = await _context.Quotes.CountAsync();
            var orders = await _context.SalesOrders.CountAsync();
            var delivered = await _context.SalesOrders.CountAsync(so => so.DeliveryStatus == "Delivered");
            var paid = await _context.SalesOrders.CountAsync(so => so.PaymentStatus == "Paid");

            if (leads == 0) leads = 1;
            funnel = new List<FunnelStageDto>
            {
                new() { Stage = "Leads", Count = leads, ConversionRate = 100 },
                new() { Stage = "Opportunities", Count = opportunities, ConversionRate = opportunities > 0 ? Math.Round((decimal)opportunities / leads * 100, 1) : 0 },
                new() { Stage = "Quotes", Count = quotes, ConversionRate = quotes > 0 ? Math.Round((decimal)quotes / opportunities * 100, 1) : 0 },
                new() { Stage = "Orders", Count = orders, ConversionRate = orders > 0 ? Math.Round((decimal)orders / quotes * 100, 1) : 0 },
                new() { Stage = "Delivered", Count = delivered, ConversionRate = delivered > 0 ? Math.Round((decimal)delivered / orders * 100, 1) : 0 },
                new() { Stage = "Paid", Count = paid, ConversionRate = paid > 0 ? Math.Round((decimal)paid / delivered * 100, 1) : 0 }
            };
        }

        return new ChartDataDto
        {
            Categories = funnel.Select(f => f.Stage).ToList(),
            ChartTitle = "Sales Funnel",
            ChartType = "bar",
            Series = new List<ChartSeriesDto> { new() { Name = "Count", Data = funnel.Select(f => f.Count).ToList() } }
        };
    }

    public async Task<ChartDataDto> GetMonthlyTargetVsActualChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var monthlyData = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to)
            .GroupBy(so => so.OrderDate.Month)
            .Select(g => g.Sum(so => so.TotalAmount))
            .ToListAsync();

        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        var actuals = new decimal[12]; var targets = new decimal[12];
        for (int i = 0; i < Math.Min(monthlyData.Count, 12); i++) actuals[i] = monthlyData[i];
        var avgTarget = actuals.Where(a => a > 0).DefaultIfEmpty(50000m).Average();
        for (int i = 0; i < 12; i++) targets[i] = avgTarget;

        return new ChartDataDto
        {
            Categories = months.ToList(),
            ChartTitle = "Target vs Actual",
            ChartType = "bar",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Actual", Data = actuals.ToList() },
                new() { Name = "Target", Data = targets.ToList() }
            }
        };
    }

    public async Task<ChartDataDto> GetRevenueByBranchChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var data = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to && so.Branch != null)
            .GroupBy(so => so.Branch!.BranchName)
            .Select(g => new { Name = g.Key, Revenue = g.Sum(so => so.TotalAmount) })
            .OrderByDescending(x => x.Revenue).Take(10).ToListAsync();

        if (!data.Any())
        {
            data = await _context.SalesOrders
                .Where(so => so.Branch != null)
                .GroupBy(so => so.Branch!.BranchName)
                .Select(g => new { Name = g.Key, Revenue = g.Sum(so => so.TotalAmount) })
                .OrderByDescending(x => x.Revenue).Take(10).ToListAsync();
        }

        if (!data.Any())
        {
            return new ChartDataDto
            {
                Categories = new List<string> { "No Data" },
                ChartTitle = "Revenue by Branch",
                ChartType = "bar",
                Series = new List<ChartSeriesDto> { new() { Name = "Revenue", Data = new List<decimal> { 0 } } }
            };
        }

        return new ChartDataDto
        {
            Categories = data.Select(x => x.Name).ToList(),
            ChartTitle = "Revenue by Branch",
            ChartType = "bar",
            Series = new List<ChartSeriesDto> { new() { Name = "Revenue", Data = data.Select(x => x.Revenue).ToList() } }
        };
    }
}
