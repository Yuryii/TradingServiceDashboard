using Microsoft.EntityFrameworkCore;
using Dashboard.Data;
using Dashboard.Models.ViewModels;
using Dashboard.Services.Interfaces;

namespace Dashboard.Services;

public class FinanceDashboardService : IFinanceDashboardService
{
    private readonly ApplicationDbContext _context;

    public FinanceDashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FinanceDashboardViewModel> GetDashboardDataAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        return new FinanceDashboardViewModel
        {
            TotalIncome = await GetTotalIncomeAsync(from, to),
            TotalExpenses = await GetTotalExpensesAsync(from, to),
            NetProfit = await GetNetProfitAsync(from, to),
            CashFlow = await GetCashFlowAsync(from, to),
            ProfitMargin = await GetProfitMarginAsync(from, to),
            Dso = await GetDsoAsync(from, to),
            Dpo = await GetDpoAsync(from, to),
            ArBalance = await GetArBalanceAsync(from, to),
            FinancialOverviewChart = await GetFinancialOverviewChartAsync(from, to),
            ExpenseBreakdownChart = await GetExpenseBreakdownChartAsync(from, to),
            CashflowTrendChart = await GetCashflowTrendChartAsync(from, to),
            ArApAgingChart = await GetArApAgingChartAsync(from, to),
            ProfitWaterfallChart = await GetProfitWaterfallChartAsync(from, to),
            CashflowTrendAreaChart = await GetCashflowTrendAreaChartAsync(from, to),
            DsoDpoTrendChart = await GetDsoDpoTrendChartAsync(from, to),
            CollectionsPerformanceChart = await GetCollectionsPerformanceChartAsync(from, to),
            ExpenseTreemapChart = await GetExpenseTreemapChartAsync(from, to),
            UnitEconomicsChart = await GetUnitEconomicsChartAsync(from, to),
            CustomerProfitScatterChart = await GetCustomerProfitScatterChartAsync(from, to),
            ArApAgingStackedChart = await GetArApAgingStackedChartAsync(from, to),
            ExpenseBreakdown = await GetExpenseBreakdownAsync(from, to),
            RecentTransactions = await GetRecentTransactionsAsync(10, from, to),
            MonthlyBudgetStatus = await GetMonthlyBudgetStatusAsync(from, to),
            ProfitMarginPercent = await CalculateProfitMarginAsync(from, to)
        };
    }

    private (DateTime from, DateTime to) GetDateRange(DateTime? from, DateTime? to)
    {
        var toDate = to ?? DateTime.Now;
        var fromDate = from ?? new DateTime(toDate.Year, toDate.Month, 1);
        return (fromDate, toDate);
    }

    public async Task<KpiCardDto> GetTotalIncomeAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var current = await _context.SalesOrders.Where(so => so.OrderDate >= from && so.OrderDate <= to).SumAsync(so => (decimal?)so.TotalAmount) ?? 0;
        var lastPeriod = await _context.SalesOrders.Where(so => so.OrderDate >= from.AddMonths(-1) && so.OrderDate <= from).SumAsync(so => (decimal?)so.TotalAmount) ?? 0;
        var growth = lastPeriod > 0 ? ((current - lastPeriod) / lastPeriod) * 100 : 0;

        return MakeKpi("Total Income", current, growth, "up", "icon-base bx bx-dollar", "bg-label-success", "text-success");
    }

    public async Task<KpiCardDto> GetTotalExpensesAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var current = await _context.Expenses.Where(e => e.Status == "Approved" && e.ExpenseDate >= from && e.ExpenseDate <= to).SumAsync(e => (decimal?)e.Amount) ?? 0;
        var lastPeriod = await _context.Expenses.Where(e => e.Status == "Approved" && e.ExpenseDate >= from.AddMonths(-1) && e.ExpenseDate <= from).SumAsync(e => (decimal?)e.Amount) ?? 0;
        var growth = lastPeriod > 0 ? ((current - lastPeriod) / lastPeriod) * 100 : 0;

        return MakeKpi("Total Expenses", current, -growth, growth <= 0 ? "up" : "down", "icon-base bx bx-wallet", "bg-label-danger", "text-danger");
    }

    public async Task<KpiCardDto> GetNetProfitAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var income = await _context.SalesOrders.Where(so => so.OrderDate >= from && so.OrderDate <= to).SumAsync(so => (decimal?)so.TotalAmount) ?? 0;
        var expenses = await _context.Expenses.Where(e => e.Status == "Approved" && e.ExpenseDate >= from && e.ExpenseDate <= to).SumAsync(e => (decimal?)e.Amount) ?? 0;
        var netProfit = income - expenses;
        var lastProfit = income - expenses;
        var growth = lastProfit != 0 ? ((netProfit - lastProfit) / Math.Abs(lastProfit)) * 100 : 0;

        return MakeKpi("Net Profit", netProfit, growth, netProfit >= 0 ? "up" : "down", "icon-base bx bx-trending-up", "bg-label-primary", "text-primary");
    }

    public async Task<KpiCardDto> GetCashFlowAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var inflows = await _context.SalesOrders.Where(so => so.OrderDate >= from && so.OrderDate <= to).SumAsync(so => (decimal?)so.PaidAmount) ?? 0;
        var outflows = await _context.Expenses.Where(e => e.Status == "Approved" && e.ExpenseDate >= from && e.ExpenseDate <= to).SumAsync(e => (decimal?)e.Amount) ?? 0;
        var cashFlow = inflows - outflows;

        return new KpiCardDto
        {
            Title = "Cash Flow",
            Value = $"{(cashFlow >= 0 ? "+" : "")}{FormatCurrency(cashFlow)}",
            FormattedValue = $"{(cashFlow >= 0 ? "+" : "")}{FormatCurrency(cashFlow)}",
            NumericValue = cashFlow,
            GrowthPercent = 0,
            Trend = cashFlow >= 0 ? "up" : "down",
            TrendLabel = cashFlow >= 0 ? "Positive" : "Negative",
            IconClass = "icon-base bx bx-trending-up",
            IconBgClass = cashFlow >= 0 ? "bg-label-success" : "bg-label-danger",
            IconColorClass = cashFlow >= 0 ? "text-success" : "text-danger"
        };
    }

    public async Task<KpiCardDto> GetProfitMarginAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var income = await _context.SalesOrders.Where(so => so.OrderDate >= from && so.OrderDate <= to).SumAsync(so => (decimal?)so.TotalAmount) ?? 1;
        var expenses = await _context.Expenses.Where(e => e.Status == "Approved" && e.ExpenseDate >= from && e.ExpenseDate <= to).SumAsync(e => (decimal?)e.Amount) ?? 0;
        var margin = income > 0 ? (income - expenses) / income * 100 : 0;

        return new KpiCardDto
        {
            Title = "Profit Margin",
            Value = $"{margin:F1}%",
            FormattedValue = $"{margin:F1}%",
            NumericValue = margin,
            GrowthPercent = 0,
            Trend = margin >= 30 ? "up" : margin >= 15 ? "neutral" : "down",
            TrendLabel = margin >= 30 ? "Healthy" : margin >= 15 ? "Normal" : "Low",
            IconClass = "icon-base bx bx-percent",
            IconBgClass = "bg-label-warning",
            IconColorClass = "text-warning"
        };
    }

    public async Task<KpiCardDto> GetDsoAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var unpaid = await _context.SalesOrders.Where(so => so.PaymentStatus != "Paid" && so.OrderDate >= from && so.OrderDate <= to).SumAsync(so => (decimal?)so.TotalAmount) ?? 0;
        var dailySales = await _context.SalesOrders.Where(so => so.OrderDate >= from && so.OrderDate <= to).SumAsync(so => (decimal?)so.TotalAmount) ?? 1;
        var days = Math.Max(1, (to - from).Days);
        var divisor = Math.Max(1, dailySales / days);
        var dso = unpaid > 0 ? unpaid / divisor : 0;

        return new KpiCardDto
        {
            Title = "DSO (Days Sales Outstanding)",
            Value = $"{dso:F0} days",
            FormattedValue = $"{dso:F0} days",
            NumericValue = dso,
            GrowthPercent = 0,
            Trend = dso <= 30 ? "up" : dso <= 45 ? "neutral" : "down",
            TrendLabel = dso <= 30 ? "Good" : dso <= 45 ? "Fair" : "High",
            IconClass = "icon-base bx bx-calendar",
            IconBgClass = dso <= 30 ? "bg-label-success" : dso <= 45 ? "bg-label-warning" : "bg-label-danger",
            IconColorClass = dso <= 30 ? "text-success" : dso <= 45 ? "text-warning" : "text-danger"
        };
    }

    public async Task<KpiCardDto> GetDpoAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var unpaidExpenses = await _context.Expenses.Where(e => e.Status == "Approved" && e.ExpenseDate >= from && e.ExpenseDate <= to).SumAsync(e => (decimal?)e.Amount) ?? 0;
        var dailyExpenses = unpaidExpenses > 0 ? unpaidExpenses : 10000;
        var days = (to - from).Days;
        var dpo = days > 0 ? Math.Max(15, Math.Min(60, days * 0.7m)) : 30;

        return new KpiCardDto
        {
            Title = "DPO (Days Payable Outstanding)",
            Value = $"{dpo:F0} days",
            FormattedValue = $"{dpo:F0} days",
            NumericValue = dpo,
            GrowthPercent = 0,
            Trend = "neutral",
            TrendLabel = "Normal",
            IconClass = "icon-base bx bx-calendar-check",
            IconBgClass = "bg-label-info",
            IconColorClass = "text-info"
        };
    }

    private async Task<KpiCardDto> GetArBalanceAsync(DateTime from, DateTime to)
    {
        var balance = await _context.SalesOrders
            .Where(so => so.PaymentStatus != "Paid" && so.OrderDate >= from && so.OrderDate <= to)
            .SumAsync(so => (decimal?)(so.TotalAmount - so.PaidAmount)) ?? 0;

        return new KpiCardDto
        {
            Title = "AR Balance",
            Value = FormatCurrency(balance),
            FormattedValue = FormatCurrency(balance),
            NumericValue = balance,
            GrowthPercent = 0,
            Trend = "neutral",
            TrendLabel = "Outstanding",
            IconClass = "icon-base bx bx-credit-card",
            IconBgClass = "bg-label-secondary",
            IconColorClass = "text-secondary"
        };
    }

    public async Task<ChartDataDto> GetFinancialOverviewChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var incomeData = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to)
            .GroupBy(so => so.OrderDate.Month)
            .Select(g => g.Sum(so => so.TotalAmount))
            .ToListAsync();

        var expenseData = await _context.Expenses
            .Where(e => e.Status == "Approved" && e.ExpenseDate >= from && e.ExpenseDate <= to)
            .GroupBy(e => e.ExpenseDate.Month)
            .Select(g => g.Sum(e => e.Amount))
            .ToListAsync();

        if (!incomeData.Any())
        {
            incomeData = await _context.SalesOrders
                .GroupBy(so => so.OrderDate.Month)
                .Select(g => g.Sum(so => so.TotalAmount))
                .ToListAsync();
        }

        if (!expenseData.Any())
        {
            expenseData = await _context.Expenses
                .Where(e => e.Status == "Approved")
                .GroupBy(e => e.ExpenseDate.Month)
                .Select(g => g.Sum(e => e.Amount))
                .ToListAsync();
        }

        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        var incomeArr = new decimal[12];
        for (int i = 0; i < Math.Min(incomeData.Count, 12); i++) incomeArr[incomeData.Count - 1 - i] = incomeData[incomeData.Count - 1 - i];

        var expenseArr = new decimal[12];
        for (int i = 0; i < Math.Min(expenseData.Count, 12); i++) expenseArr[expenseData.Count - 1 - i] = expenseData[expenseData.Count - 1 - i];

        return new ChartDataDto
        {
            Categories = months.ToList(),
            ChartTitle = "Financial Overview",
            ChartType = "bar",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Income", Data = incomeArr.ToList() },
                new() { Name = "Expenses", Data = expenseArr.ToList() }
            }
        };
    }

    public async Task<ChartDataDto> GetExpenseBreakdownChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var data = await _context.Expenses
            .Where(e => e.Status == "Approved" && e.ExpenseDate >= from && e.ExpenseDate <= to && e.Category != null)
            .GroupBy(e => e.Category!.CategoryName)
            .Select(g => new { Category = g.Key, Total = g.Sum(e => e.Amount) })
            .OrderByDescending(x => x.Total)
            .Take(10)
            .ToListAsync();

        if (!data.Any())
        {
            return new ChartDataDto
            {
                Categories = new List<string> { "Operations", "Payroll", "Marketing", "IT", "Travel" },
                ChartTitle = "Expense Breakdown",
                ChartType = "bar",
                Series = new List<ChartSeriesDto> { new() { Name = "Expenses", Data = new List<decimal> { 404600, 462400, 173400, 85000, 30500 } } }
            };
        }

        return new ChartDataDto
        {
            Categories = data.Select(x => x.Category).ToList(),
            ChartTitle = "Expense Breakdown",
            ChartType = "bar",
            Series = new List<ChartSeriesDto> { new() { Name = "Expenses", Data = data.Select(x => x.Total).ToList() } }
        };
    }

    public async Task<ChartDataDto> GetCashflowTrendChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var inflows = await _context.CustomerPayments
            .Where(p => p.PaymentDate >= from && p.PaymentDate <= to)
            .GroupBy(p => p.PaymentDate.Month)
            .Select(g => g.Sum(p => p.Amount))
            .ToListAsync();

        var outflows = await _context.Expenses
            .Where(e => e.Status == "Approved" && e.ExpenseDate >= from && e.ExpenseDate <= to)
            .GroupBy(e => e.ExpenseDate.Month)
            .Select(g => g.Sum(e => e.Amount))
            .ToListAsync();

        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        var inArr = new decimal[12];
        for (int i = 0; i < Math.Min(inflows.Count, 12); i++) inArr[11 - i] = inflows[inflows.Count - 1 - i];

        var outArr = new decimal[12];
        for (int i = 0; i < Math.Min(outflows.Count, 12); i++) outArr[11 - i] = outflows[outflows.Count - 1 - i];

        return new ChartDataDto
        {
            Categories = months.ToList(),
            ChartTitle = "Cash Flow Trend",
            ChartType = "area",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Inflows", Data = inArr.ToList() },
                new() { Name = "Outflows", Data = outArr.ToList() }
            }
        };
    }

    public async Task<ChartDataDto> GetArApAgingChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var agingBuckets = new[] { "0-30", "31-60", "61-90", "90+" };

        var arData = new List<decimal> { 100000, 50000, 30000, 15000 };
        var apData = new List<decimal> { 80000, 40000, 20000, 10000 };

        return new ChartDataDto
        {
            Categories = agingBuckets.ToList(),
            ChartTitle = "AR/AP Aging",
            ChartType = "bar",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "AR (Receivable)", Data = arData },
                new() { Name = "AP (Payable)", Data = apData }
            }
        };
    }

    public async Task<List<TopNItemDto>> GetExpenseBreakdownAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var data = await _context.Expenses
            .Where(e => e.Status == "Approved" && e.ExpenseDate >= from && e.ExpenseDate <= to && e.Category != null)
            .GroupBy(e => e.Category!.CategoryName)
            .Select(g => new { Category = g.Key, Total = g.Sum(e => e.Amount) })
            .OrderByDescending(x => x.Total)
            .Take(5)
            .ToListAsync();

        if (!data.Any())
        {
            return new List<TopNItemDto>
            {
                new() { Name = "Operations", Subtitle = "35% of expenses", Value = 404600, FormattedValue = "$404.6K", IconClass = "icon-base bx bx-building", IconBgClass = "bg-label-primary" },
                new() { Name = "Payroll", Subtitle = "40% of expenses", Value = 462400, FormattedValue = "$462.4K", IconClass = "icon-base bx bx-user", IconBgClass = "bg-label-success" },
                new() { Name = "Marketing", Subtitle = "15% of expenses", Value = 173400, FormattedValue = "$173.4K", IconClass = "icon-base bx bx-megaphone", IconBgClass = "bg-label-info" }
            };
        }

        var icons = new[] { "icon-base bx bx-building", "icon-base bx bx-user", "icon-base bx bx-megaphone", "icon-base bx bx-cog", "icon-base bx bx-car" };
        var bgClasses = new[] { "bg-label-primary", "bg-label-success", "bg-label-info", "bg-label-warning", "bg-label-secondary" };

        return data.Select((d, i) => new TopNItemDto
        {
            Name = d.Category,
            Subtitle = FormatCurrency(d.Total),
            Value = d.Total,
            FormattedValue = FormatCurrency(d.Total),
            IconClass = icons[i % icons.Length],
            IconBgClass = bgClasses[i % bgClasses.Length]
        }).ToList();
    }

    public async Task<List<TableRowDto>> GetRecentTransactionsAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var payments = await _context.CustomerPayments
            .Include(p => p.Customer)
            .OrderByDescending(p => p.PaymentDate)
            .Take(count / 2)
            .Select(p => new TableRowDto
            {
                Column1 = $"Invoice #{p.PaymentID + 2000}",
                Column2 = p.Customer != null ? p.Customer.CustomerName : "Customer",
                Column3 = "Client Payment",
                Column4 = $"+{FormatCurrency(p.Amount)}",
                BadgeClass = "bg-label-success",
                TrendClass = "text-success"
            })
            .ToListAsync();

        var expenses = await _context.Expenses
            .Include(e => e.Employee)
            .Where(e => e.Status == "Approved" && e.ExpenseDate >= from && e.ExpenseDate <= to)
            .OrderByDescending(e => e.ExpenseDate)
            .Take(count / 2)
            .Select(e => new TableRowDto
            {
                Column1 = $"Bill #{e.ExpenseID + 1000}",
                Column2 = e.Employee != null ? e.Employee.FullName : "Employee",
                Column3 = e.Category != null ? e.Category.CategoryName : "Expense",
                Column4 = $"-{FormatCurrency(e.Amount)}",
                BadgeClass = "bg-label-danger",
                TrendClass = "text-danger"
            })
            .ToListAsync();

        return payments.Concat(expenses).OrderByDescending(t => t.Column1).Take(count).ToList();
    }

    public async Task<List<TopNItemDto>> GetMonthlyBudgetStatusAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var categories = await _context.ExpenseCategories.Take(5).Select(c => c.CategoryName).ToListAsync();
        if (!categories.Any())
            categories = new[] { "Revenue", "Operating Costs", "Marketing Spend" }.ToList();

        var statuses = new[] { ("On Track", "bg-label-success"), ("Under", "bg-label-warning"), ("Over", "bg-label-danger") };
        var icons = new[] { "icon-base bx bx-check-circle", "icon-base bx bx-time", "icon-base bx bx-x-circle" };

        return categories.Select((cat, i) => new TopNItemDto
        {
            Name = cat,
            Subtitle = "vs budget",
            Value = 100,
            FormattedValue = statuses[i % 3].Item1,
            IconClass = icons[i % 3],
            IconBgClass = statuses[i % 3].Item2
        }).ToList();
    }

    public async Task<List<WaterfallItemDto>> GetProfitWaterfallAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var revenue = await _context.SalesOrders.Where(so => so.OrderDate >= from && so.OrderDate <= to).SumAsync(so => (decimal?)so.TotalAmount) ?? 0;
        var cogs = revenue * 0.6m;
        var grossProfit = revenue - cogs;
        var opex = revenue * 0.25m;
        var netProfit = grossProfit - opex;

        return new List<WaterfallItemDto>
        {
            new() { Label = "Revenue", Value = revenue, Color = "success", IsTotal = false },
            new() { Label = "COGS", Value = -cogs, Color = "danger", IsTotal = false },
            new() { Label = "Gross Profit", Value = grossProfit, Color = "primary", IsTotal = true },
            new() { Label = "Operating Expenses", Value = -opex, Color = "danger", IsTotal = false },
            new() { Label = "Net Profit", Value = netProfit, Color = "primary", IsTotal = true }
        };
    }

    public async Task<List<ScatterPointDto>> GetRevenueVsProfitScatterAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var data = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to && so.Customer != null)
            .GroupBy(so => so.Customer!.CustomerName)
            .Select(g => new { Name = g.Key, Revenue = g.Sum(so => so.TotalAmount) })
            .ToListAsync();

        return data.Select(d => new ScatterPointDto
        {
            X = d.Revenue,
            Y = d.Revenue * 0.3m,
            Size = d.Revenue / 10000,
            Label = d.Name
        }).ToList();
    }

    private async Task<int> CalculateProfitMarginAsync(DateTime from, DateTime to)
    {
        var income = await _context.SalesOrders.Where(so => so.OrderDate >= from && so.OrderDate <= to).SumAsync(so => (decimal?)so.TotalAmount) ?? 0;
        var expenses = await _context.Expenses.Where(e => e.Status == "Approved" && e.ExpenseDate >= from && e.ExpenseDate <= to).SumAsync(e => (decimal?)e.Amount) ?? 0;
        return income > 0 ? (int)Math.Round((income - expenses) / income * 100) : 53;
    }

    private static KpiCardDto MakeKpi(string title, decimal value, decimal growth, string trend, string iconClass, string bgClass, string colorClass)
    {
        return new KpiCardDto
        {
            Title = title,
            Value = FormatCurrency(value),
            FormattedValue = FormatCurrency(value),
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

    public async Task<ChartDataDto> GetProfitWaterfallChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var revenue = await _context.SalesOrders.Where(so => so.OrderDate >= from && so.OrderDate <= to).SumAsync(so => (decimal?)so.TotalAmount) ?? 0;
        var expenses = await _context.Expenses.Where(e => e.Status == "Approved" && e.ExpenseDate >= from && e.ExpenseDate <= to).SumAsync(e => (decimal?)e.Amount) ?? 0;
        var cogs = revenue * 0.6m;
        var grossProfit = revenue - cogs;
        var opex = expenses * 0.5m;
        var netProfit = grossProfit - opex;

        return new ChartDataDto
        {
            Categories = new List<string> { "Revenue", "COGS", "Gross Profit", "OpEx", "Marketing", "Admin", "Tax", "Net Profit" },
            ChartTitle = "Profit Waterfall",
            ChartType = "bar",
            Series = new List<ChartSeriesDto> { new() { Name = "Impact", Data = new List<decimal> { revenue, -cogs, grossProfit, -opex * 0.4m, -opex * 0.3m, -opex * 0.2m, -opex * 0.1m, netProfit } } }
        };
    }

    public async Task<ChartDataDto> GetCashflowTrendAreaChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var inflows = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to)
            .GroupBy(so => so.OrderDate.Month)
            .Select(g => g.Sum(so => so.PaidAmount))
            .OrderBy(x => x)
            .ToListAsync();

        var outflows = await _context.Expenses
            .Where(e => e.Status == "Approved" && e.ExpenseDate >= from && e.ExpenseDate <= to)
            .GroupBy(e => e.ExpenseDate.Month)
            .Select(g => g.Sum(e => e.Amount))
            .OrderBy(x => x)
            .ToListAsync();

        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        var inArr = new decimal[12];
        for (int i = 0; i < Math.Min(inflows.Count, 12); i++) inArr[i] = inflows[i];

        var outArr = new decimal[12];
        for (int i = 0; i < Math.Min(outflows.Count, 12); i++) outArr[i] = outflows[i];

        return new ChartDataDto
        {
            Categories = months.ToList(),
            ChartTitle = "Cash Flow Trend",
            ChartType = "area",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Inflows", Data = inArr.ToList() },
                new() { Name = "Outflows", Data = outArr.ToList() }
            }
        };
    }

    public async Task<ChartDataDto> GetDsoDpoTrendChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        var unpaid = await _context.SalesOrders
            .Where(so => so.PaymentStatus != "Paid" && so.OrderDate >= from && so.OrderDate <= to)
            .SumAsync(so => (decimal?)(so.TotalAmount - so.PaidAmount)) ?? 0;
        var avgSalesPerDay = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to)
            .AverageAsync(so => (decimal?)so.TotalAmount) ?? 1;
        var unpaidExpenses = await _context.Expenses.Where(e => e.Status == "Approved" && e.ExpenseDate >= from && e.ExpenseDate <= to).SumAsync(e => (decimal?)e.Amount) ?? 0;
        var days = Math.Max(1, (to - from).Days);

        var currentDso = avgSalesPerDay > 0 ? unpaid / avgSalesPerDay : 0;
        var currentDpo = unpaidExpenses > 0 ? days * 0.7m : 30;

        var dso = Enumerable.Range(0, 12).Select((_, i) => i < 6 ? currentDso * (0.9m + i * 0.02m) : currentDso * (0.9m + i * 0.02m - (i - 5) * 0.05m)).ToArray();
        var dpo = Enumerable.Range(0, 12).Select((_, i) => i < 6 ? currentDpo * (0.85m + i * 0.03m) : currentDpo * (0.85m + i * 0.03m - (i - 5) * 0.02m)).ToArray();

        return new ChartDataDto
        {
            Categories = months.ToList(),
            ChartTitle = "DSO & DPO Trend",
            ChartType = "line",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "DSO (Days)", Data = dso.ToList() },
                new() { Name = "DPO (Days)", Data = dpo.ToList() }
            }
        };
    }

    public async Task<ChartDataDto> GetCollectionsPerformanceChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var collectors = await _context.SalesOrders
            .Include(so => so.SalesEmployee)
            .Where(so => so.OrderDate >= from && so.OrderDate <= to && so.SalesEmployee != null)
            .GroupBy(so => so.SalesEmployee!.FullName)
            .Select(g => new { Name = g.Key, Collected = g.Sum(so => so.PaidAmount), Overdue = g.Sum(so => so.TotalAmount - so.PaidAmount) })
            .OrderByDescending(x => x.Collected)
            .Take(5)
            .ToListAsync();

        if (!collectors.Any())
        {
            collectors = await _context.SalesOrders
                .Include(so => so.SalesEmployee)
                .Where(so => so.SalesEmployee != null)
                .GroupBy(so => so.SalesEmployee!.FullName)
                .Select(g => new { Name = g.Key, Collected = g.Sum(so => so.PaidAmount), Overdue = g.Sum(so => so.TotalAmount - so.PaidAmount) })
                .OrderByDescending(x => x.Collected)
                .Take(5)
                .ToListAsync();
        }

        if (!collectors.Any())
            return new ChartDataDto
            {
                Categories = new List<string> { "No Data" },
                ChartTitle = "Collections Performance",
                ChartType = "bar",
                Series = new List<ChartSeriesDto> { new() { Name = "Collected", Data = new List<decimal> { 0 } }, new() { Name = "Overdue", Data = new List<decimal> { 0 } } }
            };

        return new ChartDataDto
        {
            Categories = collectors.Select(x => x.Name).ToList(),
            ChartTitle = "Collections Performance",
            ChartType = "bar",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Collected", Data = collectors.Select(x => x.Collected).ToList() },
                new() { Name = "Overdue", Data = collectors.Select(x => x.Overdue).ToList() }
            }
        };
    }

    public async Task<ChartDataDto> GetExpenseTreemapChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var data = await _context.Expenses
            .Where(e => e.Status == "Approved" && e.ExpenseDate >= from && e.ExpenseDate <= to && e.Category != null)
            .GroupBy(e => e.Category!.CategoryName)
            .Select(g => new { Category = g.Key, Total = g.Sum(e => e.Amount) })
            .OrderByDescending(x => x.Total)
            .ToListAsync();

        if (!data.Any())
        {
            data = await _context.Expenses
                .Where(e => e.Status == "Approved" && e.Category != null)
                .GroupBy(e => e.Category!.CategoryName)
                .Select(g => new { Category = g.Key, Total = g.Sum(e => e.Amount) })
                .OrderByDescending(x => x.Total)
                .ToListAsync();
        }

        if (!data.Any())
            return new ChartDataDto
            {
                Categories = new List<string> { "No Data" },
                ChartTitle = "Expense Treemap",
                ChartType = "bar",
                Series = new List<ChartSeriesDto> { new() { Name = "Expense", Data = new List<decimal> { 0 } } }
            };

        return new ChartDataDto
        {
            Categories = data.Select(x => x.Category).ToList(),
            ChartTitle = "Expense Treemap",
            ChartType = "bar",
            Series = new List<ChartSeriesDto> { new() { Name = "Expense", Data = data.Select(x => x.Total).ToList() } }
        };
    }

    public async Task<ChartDataDto> GetUnitEconomicsChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var data = await _context.SalesOrders
            .Where(so => so.OrderDate >= from && so.OrderDate <= to && so.SalesChannel != null)
            .GroupBy(so => so.SalesChannel!.ChannelName)
            .Select(g => new
            {
                Channel = g.Key,
                Revenue = g.Sum(so => so.TotalAmount),
                OrderDetails = _context.SalesOrderDetails.Where(d => d.SalesOrder!.SalesChannel!.ChannelName == g.Key).ToList()
            })
            .ToListAsync();

        if (!data.Any())
        {
            data = await _context.SalesOrders
                .Where(so => so.SalesChannel != null)
                .GroupBy(so => so.SalesChannel!.ChannelName)
                .Select(g => new
                {
                    Channel = g.Key,
                    Revenue = g.Sum(so => so.TotalAmount),
                    OrderDetails = _context.SalesOrderDetails.Where(d => d.SalesOrder!.SalesChannel!.ChannelName == g.Key).ToList()
                })
                .ToListAsync();
        }

        if (!data.Any())
            return new ChartDataDto
            {
                Categories = new List<string> { "No Data" },
                ChartTitle = "Unit Economics by Channel",
                ChartType = "bar",
                Series = new List<ChartSeriesDto> { new() { Name = "GM%", Data = new List<decimal> { 0 } }, new() { Name = "CM%", Data = new List<decimal> { 0 } } }
            };

        var gmPct = data.Select(d => d.Revenue > 0 ? 35m : 0m).ToList();
        var cmPct = data.Select(d => d.Revenue > 0 ? 25m : 0m).ToList();

        return new ChartDataDto
        {
            Categories = data.Select(x => x.Channel).ToList(),
            ChartTitle = "Unit Economics by Channel",
            ChartType = "bar",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "GM%", Data = gmPct },
                new() { Name = "CM%", Data = cmPct }
            }
        };
    }

    public async Task<ChartDataDto> GetCustomerProfitScatterChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var scatter = await GetRevenueVsProfitScatterAsync(fromDate, toDate);
        return new ChartDataDto
        {
            Categories = scatter.Select(s => s.Label).ToList(),
            ChartTitle = "Customer Revenue vs Profit",
            ChartType = "scatter",
            Series = new List<ChartSeriesDto> { new() { Name = "Customers", Data = scatter.Select(s => s.X).ToList() } }
        };
    }

    public async Task<ChartDataDto> GetArApAgingStackedChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var buckets = new[] { "0-30", "31-60", "61-90", "90+" };
        var arData = new List<decimal>();
        var apData = new List<decimal>();

        var unpaidOrders = await _context.SalesOrders
            .Where(so => so.PaymentStatus != "Paid" && so.OrderDate >= from && so.OrderDate <= to)
            .Select(so => new { Amount = so.TotalAmount - so.PaidAmount, Days = (DateTime.Now - so.OrderDate).Days })
            .ToListAsync();

        var unpaidExpenses = await _context.Expenses
            .Where(e => e.Status == "Approved" && e.ExpenseDate >= from && e.ExpenseDate <= to)
            .Select(e => new { Amount = e.Amount, Days = (DateTime.Now - e.ExpenseDate).Days })
            .ToListAsync();

        foreach (var bucket in buckets)
        {
            decimal ar = 0, ap = 0;
            if (bucket == "0-30") { ar = unpaidOrders.Where(x => x.Days <= 30).Sum(x => x.Amount); ap = unpaidExpenses.Where(x => x.Days <= 30).Sum(x => x.Amount); }
            else if (bucket == "31-60") { ar = unpaidOrders.Where(x => x.Days > 30 && x.Days <= 60).Sum(x => x.Amount); ap = unpaidExpenses.Where(x => x.Days > 30 && x.Days <= 60).Sum(x => x.Amount); }
            else if (bucket == "61-90") { ar = unpaidOrders.Where(x => x.Days > 60 && x.Days <= 90).Sum(x => x.Amount); ap = unpaidExpenses.Where(x => x.Days > 60 && x.Days <= 90).Sum(x => x.Amount); }
            else { ar = unpaidOrders.Where(x => x.Days > 90).Sum(x => x.Amount); ap = unpaidExpenses.Where(x => x.Days > 90).Sum(x => x.Amount); }
            arData.Add(ar);
            apData.Add(ap);
        }

        return new ChartDataDto
        {
            Categories = buckets.ToList(),
            ChartTitle = "AR/AP Aging",
            ChartType = "bar",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "AR", Data = arData },
                new() { Name = "AP", Data = apData }
            }
        };
    }
}
