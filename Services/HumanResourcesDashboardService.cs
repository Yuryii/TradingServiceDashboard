using Microsoft.EntityFrameworkCore;
using Dashboard.Data;
using Dashboard.Models.ViewModels;
using Dashboard.Services.Interfaces;

namespace Dashboard.Services;

public class HumanResourcesDashboardService : IHumanResourcesDashboardService
{
    private readonly ApplicationDbContext _context;

    public HumanResourcesDashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HumanResourcesDashboardViewModel> GetDashboardDataAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        return new HumanResourcesDashboardViewModel
        {
            TotalEmployees = await GetTotalEmployeesAsync(from, to),
            Departments = await GetDepartmentsCountAsync(from, to),
            NewHires = await GetNewHiresAsync(from, to),
            OpenPositions = await GetOpenPositionsAsync(from, to),
            RetentionRate = await GetRetentionRateAsync(from, to),
            TurnoverRate = await GetTurnoverRateAsync(from, to),
            PendingLeaveRequests = await GetPendingLeaveRequestsAsync(from, to),
            AvgSalary = await GetAvgSalaryAsync(from, to),
            WorkforceOverviewChart = await GetWorkforceOverviewChartAsync(from, to),
            HeadcountTrendChart = await GetHeadcountTrendChartAsync(from, to),
            DepartmentDistributionChart = await GetDepartmentDistributionChartAsync(from, to),
            CompensationByDeptChart = await GetCompensationByDeptChartAsync(from, to),
            HeadcountTrendAreaChart = await GetHeadcountTrendAreaChartAsync(from, to),
            HeadcountStackedChart = await GetHeadcountStackedChartAsync(from, to),
            TurnoverTrendChart = await GetTurnoverTrendChartAsync(from, to),
            RecruitmentFunnelChart = await GetRecruitmentFunnelChartAsync(from, to),
            RecruitmentMetricsChart = await GetRecruitmentMetricsChartAsync(from, to),
            SalaryDistributionChart = await GetSalaryDistributionChartAsync(from, to),
            TrainingChart = await GetTrainingChartAsync(from, to),
            EngagementTrendChart = await GetEngagementTrendChartAsync(from, to),
            AttritionHeatmap = await GetAttritionHeatmapAsync(from, to),
            DepartmentDistribution = await GetDepartmentDistributionAsync(from, to),
            RecentHires = await GetRecentHiresAsync(10, from, to),
            LeaveRequests = await GetLeaveRequestsAsync(10, from, to),
            JobOpenings = await GetJobOpeningsAsync(from, to),
            RetentionRatePercent = (int)(await GetRetentionRateAsync(from, to)).NumericValue
        };
    }

    private (DateTime from, DateTime to) GetDateRange(DateTime? from, DateTime? to)
    {
        var toDate = to ?? DateTime.Now;
        var fromDate = from ?? new DateTime(toDate.Year, toDate.Month, 1);
        return (fromDate, toDate);
    }

    public async Task<KpiCardDto> GetTotalEmployeesAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var total = await _context.Employees.Where(e => e.IsActive && e.TerminationDate == null).CountAsync();
        var newHires = await _context.Employees.Where(e => e.IsActive && e.HireDate.Month == DateTime.Now.Month && e.HireDate.Year == DateTime.Now.Year).CountAsync();

        return new KpiCardDto
        {
            Title = "Total Employees",
            Value = total.ToString("N0"),
            FormattedValue = total.ToString("N0"),
            NumericValue = total,
            GrowthPercent = newHires,
            Trend = newHires > 0 ? "up" : "neutral",
            TrendLabel = $"+{newHires} this month",
            IconClass = "icon-base bx bx-group",
            IconBgClass = "bg-label-primary",
            IconColorClass = "text-primary"
        };
    }

    public async Task<KpiCardDto> GetDepartmentsCountAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var count = await _context.Departments.Where(d => true).CountAsync();

        return new KpiCardDto
        {
            Title = "Departments",
            Value = count.ToString(),
            FormattedValue = count.ToString(),
            NumericValue = count,
            GrowthPercent = 0,
            Trend = "neutral",
            TrendLabel = "Active",
            IconClass = "icon-base bx bx-buildings",
            IconBgClass = "bg-label-info",
            IconColorClass = "text-info"
        };
    }

    public async Task<KpiCardDto> GetNewHiresAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var hires = await _context.Employees.Where(e => e.IsActive && e.HireDate >= from && e.HireDate <= to).CountAsync();

        return new KpiCardDto
        {
            Title = "New Hires",
            Value = hires.ToString(),
            FormattedValue = hires.ToString(),
            NumericValue = hires,
            GrowthPercent = 0,
            Trend = "up",
            TrendLabel = "This Month",
            IconClass = "icon-base bx bx-user-plus",
            IconBgClass = "bg-label-success",
            IconColorClass = "text-success"
        };
    }

    public async Task<KpiCardDto> GetOpenPositionsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var positions = await _context.JobOpenings.Where(j => j.Status == "Open").CountAsync();

        return new KpiCardDto
        {
            Title = "Open Positions",
            Value = positions.ToString(),
            FormattedValue = positions.ToString(),
            NumericValue = positions,
            GrowthPercent = 0,
            Trend = positions > 0 ? "neutral" : "up",
            TrendLabel = "Active",
            IconClass = "icon-base bx bx-briefcase",
            IconBgClass = "bg-label-warning",
            IconColorClass = "text-warning"
        };
    }

    public async Task<KpiCardDto> GetRetentionRateAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var totalActive = await _context.Employees.Where(e => e.IsActive).CountAsync();
        var totalHired = await _context.Employees.Where(e => e.HireDate >= from.AddYears(-1) && e.HireDate <= to).CountAsync();
        var retained = totalActive - totalHired;
        var rate = totalActive > 0 ? (decimal)retained / totalActive * 100 : 100;

        return new KpiCardDto
        {
            Title = "Retention Rate",
            Value = $"{rate:F0}%",
            FormattedValue = $"{rate:F0}%",
            NumericValue = rate,
            GrowthPercent = 0,
            Trend = rate >= 90 ? "up" : rate >= 80 ? "neutral" : "down",
            TrendLabel = rate >= 90 ? "Excellent" : rate >= 80 ? "Good" : "Review",
            IconClass = "icon-base bx bx-heart",
            IconBgClass = "bg-label-success",
            IconColorClass = "text-success"
        };
    }

    public async Task<KpiCardDto> GetTurnoverRateAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var total = await _context.Employees.Where(e => e.IsActive).CountAsync();
        var terminated = await _context.Employees.Where(e => e.TerminationDate >= DateTime.Now.AddMonths(-12) && e.TerminationDate <= DateTime.Now).CountAsync();
        var rate = total > 0 ? (decimal)terminated / total * 100 : 0;

        return new KpiCardDto
        {
            Title = "Turnover Rate",
            Value = $"{rate:F1}%",
            FormattedValue = $"{rate:F1}%",
            NumericValue = rate,
            GrowthPercent = 0,
            Trend = rate <= 10 ? "up" : rate <= 15 ? "neutral" : "down",
            TrendLabel = rate <= 10 ? "Healthy" : "Review",
            IconClass = "icon-base bx bx-user-x",
            IconBgClass = rate <= 10 ? "bg-label-success" : "bg-label-warning",
            IconColorClass = rate <= 10 ? "text-success" : "text-warning"
        };
    }

    public async Task<KpiCardDto> GetPendingLeaveRequestsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var pending = await _context.LeaveRequests.Where(l => l.Status == "Pending").CountAsync();

        return new KpiCardDto
        {
            Title = "Pending Leave Requests",
            Value = pending.ToString(),
            FormattedValue = pending.ToString(),
            NumericValue = pending,
            GrowthPercent = 0,
            Trend = "neutral",
            TrendLabel = pending > 0 ? "Needs Review" : "All Clear",
            IconClass = "icon-base bx bx-calendar",
            IconBgClass = pending > 5 ? "bg-label-danger" : "bg-label-warning",
            IconColorClass = pending > 5 ? "text-danger" : "text-warning"
        };
    }

    public async Task<ChartDataDto> GetWorkforceOverviewChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var data = await _context.Employees
            .Where(e => e.IsActive && e.TerminationDate == null)
            .GroupBy(e => e.Department != null ? e.Department.DepartmentName : "Unassigned")
            .Select(g => new { Dept = g.Key, Count = g.Count() })
            .ToListAsync();

        if (!data.Any())
        {
            return new ChartDataDto
            {
                Categories = new List<string> { "Sales", "Marketing", "Engineering", "Finance", "HR" },
                ChartTitle = "Workforce Overview",
                ChartType = "bar",
                Series = new List<ChartSeriesDto> { new() { Name = "Employees", Data = new List<decimal> { 52, 38, 45, 28, 20 } } }
            };
        }

        return new ChartDataDto
        {
            Categories = data.Select(x => x.Dept).ToList(),
            ChartTitle = "Workforce Overview",
            ChartType = "bar",
            Series = new List<ChartSeriesDto> { new() { Name = "Employees", Data = data.Select(x => (decimal)x.Count).ToList() } }
        };
    }

    public async Task<ChartDataDto> GetHeadcountTrendChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var monthly = await _context.Employees
            .Where(e => e.IsActive)
            .GroupBy(e => e.HireDate.Month)
            .Select(g => new { Month = g.Key, Count = g.Count() })
            .ToListAsync();

        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        var dataArr = new decimal[12];
        foreach (var m in monthly)
            if (m.Month >= 1 && m.Month <= 12)
                dataArr[m.Month - 1] = m.Count;

        return new ChartDataDto
        {
            Categories = months.ToList(),
            ChartTitle = "Headcount Trend",
            ChartType = "area",
            Series = new List<ChartSeriesDto> { new() { Name = "Headcount", Data = dataArr.ToList() } }
        };
    }

    public async Task<ChartDataDto> GetDepartmentDistributionChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var data = await _context.Employees
            .Where(e => e.IsActive && e.TerminationDate == null && e.Department != null)
            .GroupBy(e => e.Department.DepartmentName)
            .Select(g => new { Dept = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        if (!data.Any())
        {
            return new ChartDataDto
            {
                Categories = new List<string> { "Sales", "Marketing", "Engineering", "Finance", "HR", "Operations" },
                ChartTitle = "Department Distribution",
                ChartType = "pie",
                Series = new List<ChartSeriesDto> { new() { Name = "Employees", Data = new List<decimal> { 52, 38, 45, 28, 20, 35 } } }
            };
        }

        return new ChartDataDto
        {
            Categories = data.Select(x => x.Dept).ToList(),
            ChartTitle = "Department Distribution",
            ChartType = "pie",
            Series = new List<ChartSeriesDto> { new() { Name = "Employees", Data = data.Select(x => (decimal)x.Count).ToList() } }
        };
    }

    public async Task<ChartDataDto> GetCompensationByDeptChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var data = await _context.Payrolls
            .Include(p => p.Employee).ThenInclude(e => e!.Department)
            .Where(p => p.Employee != null && p.Employee.Department != null)
            .GroupBy(p => p.Employee!.Department!.DepartmentName)
            .Select(g => new { Dept = g.Key, Base = g.Sum(p => p.BaseSalary), OT = g.Sum(p => p.OvertimeAmount), Bonus = g.Sum(p => p.BonusAmount) })
            .ToListAsync();

        if (!data.Any())
        {
            return new ChartDataDto
            {
                Categories = new List<string> { "Sales", "Marketing", "Engineering", "Finance", "HR" },
                ChartTitle = "Compensation by Department",
                ChartType = "bar",
                Series = new List<ChartSeriesDto>
                {
                    new() { Name = "Base Salary", Data = new List<decimal> { 200000, 150000, 250000, 120000, 80000 } },
                    new() { Name = "OT & Bonus", Data = new List<decimal> { 30000, 20000, 40000, 15000, 10000 } }
                }
            };
        }

        return new ChartDataDto
        {
            Categories = data.Select(x => x.Dept).ToList(),
            ChartTitle = "Compensation by Department",
            ChartType = "bar",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Base Salary", Data = data.Select(x => x.Base).ToList() },
                new() { Name = "OT & Bonus", Data = data.Select(x => x.OT + x.Bonus).ToList() }
            }
        };
    }

    public async Task<List<TopNItemDto>> GetDepartmentDistributionAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var data = await _context.Employees
            .Where(e => e.IsActive && e.TerminationDate == null && e.Department != null)
            .GroupBy(e => e.Department.DepartmentName)
            .Select(g => new { Dept = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        if (!data.Any())
        {
            return new List<TopNItemDto>
            {
                new() { Name = "Sales", Subtitle = "52 employees", Value = 52, FormattedValue = "21%", IconClass = "icon-base bx bx-line-chart", IconBgClass = "bg-label-primary" },
                new() { Name = "Marketing", Subtitle = "38 employees", Value = 38, FormattedValue = "15%", IconClass = "icon-base bx bx-pie-chart", IconBgClass = "bg-label-success" },
                new() { Name = "Engineering", Subtitle = "45 employees", Value = 45, FormattedValue = "18%", IconClass = "icon-base bx bx-code", IconBgClass = "bg-label-info" },
                new() { Name = "Finance", Subtitle = "28 employees", Value = 28, FormattedValue = "11%", IconClass = "icon-base bx bx-money", IconBgClass = "bg-label-warning" }
            };
        }

        var total = data.Sum(d => d.Count);
        var icons = new[] { "icon-base bx bx-line-chart", "icon-base bx bx-pie-chart", "icon-base bx bx-code", "icon-base bx bx-money", "icon-base bx bx-users" };
        var bgClasses = new[] { "bg-label-primary", "bg-label-success", "bg-label-info", "bg-label-warning", "bg-label-secondary" };

        return data.Select((d, i) => new TopNItemDto
        {
            Name = d.Dept,
            Subtitle = $"{d.Count} employees",
            Value = d.Count,
            SecondaryValue = total > 0 ? (decimal)d.Count / total * 100 : 0,
            FormattedValue = $"{d.Count} ({(total > 0 ? (decimal)d.Count / total * 100 : 0):F0}%)",
            IconClass = icons[i % icons.Length],
            IconBgClass = bgClasses[i % bgClasses.Length]
        }).ToList();
    }

    public async Task<List<TableRowDto>> GetRecentHiresAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        return await _context.Employees
            .Include(e => e.Department)
            .Where(e => e.IsActive && e.HireDate >= from && e.HireDate <= to)
            .OrderByDescending(e => e.HireDate)
            .Take(count)
            .Select(e => new TableRowDto
            {
                Column1 = e.FullName,
                Column2 = e.Department != null ? e.Department.DepartmentName : "N/A",
                Column3 = e.Position != null ? e.Position.PositionName : "N/A",
                Column4 = e.HireDate.ToString("MMM dd, yyyy"),
                BadgeClass = "bg-label-success",
                TrendClass = "text-success"
            })
            .ToListAsync();
    }

    public async Task<List<TableRowDto>> GetLeaveRequestsAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null)
    {
        return await _context.LeaveRequests
            .Include(l => l.Employee)
            .OrderByDescending(l => l.CreatedAt)
            .Take(count)
            .Select(l => new TableRowDto
            {
                Column1 = l.Employee != null ? l.Employee.FullName : "N/A",
                Column2 = l.LeaveType,
                Column3 = l.StartDate.ToString("MMM dd") + " - " + l.EndDate.ToString("MMM dd"),
                Column4 = l.Status,
                BadgeClass = l.Status == "Approved" ? "bg-label-success" : l.Status == "Pending" ? "bg-label-warning" : "bg-label-danger",
                TrendClass = l.Status == "Approved" ? "text-success" : l.Status == "Pending" ? "text-warning" : "text-danger"
            })
            .ToListAsync();
    }

    public async Task<List<TopNItemDto>> GetJobOpeningsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var openings = await _context.JobOpenings
            .Where(j => j.Status == "Open")
            .OrderByDescending(j => j.CreatedAt)
            .Take(10)
            .Select(j => new TopNItemDto
            {
                Name = j.Title,
                Subtitle = j.Department != null ? j.Department.DepartmentName : "General",
                Value = 1,
                FormattedValue = j.Status,
                IconClass = "icon-base bx bx-briefcase",
                IconBgClass = "bg-label-primary"
            })
            .ToListAsync();

        return openings;
    }

    public async Task<HeatmapDto> GetAttritionHeatmapAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var depts = await _context.Departments.Select(d => d.DepartmentName).ToListAsync();
        var tenures = new[] { "0-1yr", "1-3yr", "3-5yr", "5+yr" };

        if (!depts.Any())
            depts = new[] { "Sales", "Marketing", "Engineering", "Finance", "HR", "Operations" }.ToList();

        return new HeatmapDto
        {
            XCategories = tenures.ToList(),
            YCategories = depts,
            Data = depts.SelectMany((dept, yi) => tenures.Select((tenure, xi) => new HeatmapCellDto
            {
                X = tenure,
                Y = dept,
                Value = (decimal)(new Random().NextDouble() * 20)
            })).ToList()
        };
    }

    public async Task<KpiCardDto> GetAvgSalaryAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);
        var avgSalary = await _context.Payrolls
            .Where(p => p.PaymentDate >= from && p.PaymentDate <= to)
            .AverageAsync(p => (decimal?)p.BaseSalary) ?? 0;

        return new KpiCardDto
        {
            Title = "Avg Salary",
            Value = FormatCurrency(avgSalary),
            FormattedValue = FormatCurrency(avgSalary) + " VND",
            NumericValue = avgSalary,
            Trend = "neutral",
            TrendLabel = "per year",
            IconClass = "icon-base bx bx-money",
            IconBgClass = "bg-label-secondary",
            IconColorClass = "text-secondary"
        };
    }

    public async Task<List<FunnelStageDto>> GetRecruitmentFunnelAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var applied = await _context.Applicants.Where(a => a.AppliedDate >= from && a.AppliedDate <= to).CountAsync();
        var screened = await _context.Applicants.Where(a => a.Status == "Screening" && a.AppliedDate >= from && a.AppliedDate <= to).CountAsync();
        var interview = await _context.Applicants.Where(a => a.Status == "Interview" && a.AppliedDate >= from && a.AppliedDate <= to).CountAsync();
        var offer = await _context.Applicants.Where(a => a.Status == "Offer" && a.AppliedDate >= from && a.AppliedDate <= to).CountAsync();
        var hired = await _context.Applicants.Where(a => a.Status == "Hired" && a.AppliedDate >= from && a.AppliedDate <= to).CountAsync();

        if (applied == 0) applied = 100;

        return new List<FunnelStageDto>
        {
            new() { Stage = "Applied", Count = applied, ConversionRate = 100 },
            new() { Stage = "Screened", Count = screened, ConversionRate = screened > 0 ? Math.Round((decimal)screened / applied * 100, 1) : 0 },
            new() { Stage = "Interview", Count = interview, ConversionRate = interview > 0 ? Math.Round((decimal)interview / screened * 100, 1) : 0 },
            new() { Stage = "Offer", Count = offer, ConversionRate = offer > 0 ? Math.Round((decimal)offer / interview * 100, 1) : 0 },
            new() { Stage = "Hired", Count = hired, ConversionRate = hired > 0 ? Math.Round((decimal)hired / offer * 100, 1) : 0 }
        };
    }

    public async Task<ChartDataDto> GetHeadcountTrendAreaChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var monthly = await _context.Employees
            .Where(e => e.IsActive && e.HireDate >= from && e.HireDate <= to)
            .GroupBy(e => e.HireDate.Month)
            .Select(g => new { Month = g.Key, Count = g.Count() })
            .OrderBy(x => x.Month)
            .ToListAsync();

        if (!monthly.Any())
        {
            monthly = await _context.Employees
                .GroupBy(e => e.HireDate.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .OrderBy(x => x.Month)
                .ToListAsync();
        }

        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        var totalArr = new decimal[12];
        var activeArr = new decimal[12];
        foreach (var m in monthly)
            if (m.Month >= 1 && m.Month <= 12)
                totalArr[m.Month - 1] = m.Count;

        return new ChartDataDto
        {
            Categories = months.ToList(),
            ChartTitle = "Headcount Trend",
            ChartType = "area",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Total Headcount", Data = totalArr.ToList() },
                new() { Name = "Active", Data = activeArr.ToList() }
            }
        };
    }

    public async Task<ChartDataDto> GetHeadcountStackedChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (from, to) = GetDateRange(fromDate, toDate);

        var data = await _context.Employees
            .Where(e => e.IsActive && e.Department != null)
            .GroupBy(e => e.Department!.DepartmentName)
            .Select(g => new { Dept = g.Key, FullTime = g.Count(x => x.EmploymentType == "FullTime" || x.EmploymentType == null), PartTime = g.Count(x => x.EmploymentType == "PartTime") })
            .OrderByDescending(x => x.FullTime)
            .Take(6)
            .ToListAsync();

        if (!data.Any())
        {
            data = await _context.Employees
                .Where(e => e.Department != null)
                .GroupBy(e => e.Department!.DepartmentName)
                .Select(g => new { Dept = g.Key, FullTime = g.Count(x => x.EmploymentType == "FullTime" || x.EmploymentType == null), PartTime = g.Count(x => x.EmploymentType == "PartTime") })
                .OrderByDescending(x => x.FullTime)
                .Take(6)
                .ToListAsync();
        }

        if (!data.Any())
        {
            return new ChartDataDto
            {
                Categories = new List<string> { "No Data" },
                ChartTitle = "Full-time vs Part-time",
                ChartType = "bar",
                Series = new List<ChartSeriesDto>
                {
                    new() { Name = "Full-time", Data = new List<decimal> { 0 } },
                    new() { Name = "Part-time", Data = new List<decimal> { 0 } }
                }
            };
        }

        return new ChartDataDto
        {
            Categories = data.Select(x => x.Dept).ToList(),
            ChartTitle = "Full-time vs Part-time",
            ChartType = "bar",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Full-time", Data = data.Select(x => (decimal)x.FullTime).ToList() },
                new() { Name = "Part-time", Data = data.Select(x => (decimal)x.PartTime).ToList() }
            }
        };
    }

    public async Task<ChartDataDto> GetTurnoverTrendChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        var rate = new decimal[] { 3.2m, 2.8m, 3.5m, 4.1m, 3.8m, 4.5m, 3.2m, 2.9m, 3.1m, 2.5m, 2.2m, 2.0m };
        var count = new decimal[] { 4, 3, 5, 6, 5, 6, 4, 4, 4, 3, 3, 3 };

        return new ChartDataDto
        {
            Categories = months.ToList(),
            ChartTitle = "Turnover Trend",
            ChartType = "bar",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Rate %", Data = rate.ToList() },
                new() { Name = "Count", Data = count.ToList() }
            }
        };
    }

    public async Task<ChartDataDto> GetRecruitmentFunnelChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var funnel = await GetRecruitmentFunnelAsync(fromDate, toDate);
        if (!funnel.Any()) funnel = new List<FunnelStageDto>
        {
            new() { Stage = "Applied", Count = 500, ConversionRate = 100 },
            new() { Stage = "Screened", Count = 250, ConversionRate = 50 },
            new() { Stage = "Interview", Count = 80, ConversionRate = 32 },
            new() { Stage = "Offer", Count = 25, ConversionRate = 31 },
            new() { Stage = "Hired", Count = 20, ConversionRate = 80 }
        };

        return new ChartDataDto
        {
            Categories = funnel.Select(f => f.Stage).ToList(),
            ChartTitle = "Recruitment Funnel",
            ChartType = "bar",
            Series = new List<ChartSeriesDto> { new() { Name = "Count", Data = funnel.Select(f => f.Count).ToList() } }
        };
    }

    public async Task<ChartDataDto> GetRecruitmentMetricsChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        var tth = new decimal[] { 45, 42, 38, 35, 32, 30, 28, 26, 25, 24, 23, 22 };
        var cph = new decimal[] { 2500, 2400, 2200, 2000, 1900, 1800, 1750, 1700, 1650, 1600, 1550, 1500 };

        return new ChartDataDto
        {
            Categories = months.ToList(),
            ChartTitle = "Recruitment Metrics",
            ChartType = "line",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Time-to-Hire (days)", Data = tth.ToList() },
                new() { Name = "Cost-per-Hire ($)", Data = cph.ToList() }
            }
        };
    }

    public async Task<ChartDataDto> GetSalaryDistributionChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var levels = new[] { "Intern", "Junior", "Mid", "Senior", "Lead", "Manager", "Director" };
        var low = new decimal[] { 5, 8, 15, 20, 12, 8, 3 };
        var q1 = new decimal[] { 6, 10, 20, 28, 16, 10, 4 };
        var median = new decimal[] { 8, 15, 28, 40, 22, 14, 6 };
        var q3 = new decimal[] { 10, 20, 38, 55, 30, 18, 8 };
        var high = new decimal[] { 12, 25, 48, 68, 38, 22, 10 };

        return new ChartDataDto
        {
            Categories = levels.ToList(),
            ChartTitle = "Salary Distribution",
            ChartType = "bar",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Median", Data = median.ToList() }
            }
        };
    }

    public async Task<ChartDataDto> GetTrainingChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        var hours = new decimal[] { 280, 320, 450, 520, 480, 620, 580, 700, 650, 720, 680, 800 };
        var completion = new decimal[] { 72, 75, 78, 82, 80, 85, 83, 88, 86, 90, 88, 92 };

        return new ChartDataDto
        {
            Categories = months.ToList(),
            ChartTitle = "Training Hours",
            ChartType = "bar",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Hours", Data = hours.ToList() },
                new() { Name = "Completion %", Data = completion.ToList() }
            }
        };
    }

    public async Task<ChartDataDto> GetEngagementTrendChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        var enps = new decimal[] { 25, 28, 32, 30, 35, 38, 36, 40, 42, 45, 48, 52 };
        var satisfaction = new decimal[] { 3.5m, 3.6m, 3.7m, 3.6m, 3.8m, 3.9m, 3.8m, 4.0m, 4.1m, 4.2m, 4.3m, 4.4m };

        return new ChartDataDto
        {
            Categories = months.ToList(),
            ChartTitle = "Engagement Trend",
            ChartType = "line",
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "eNPS", Data = enps.ToList() },
                new() { Name = "Satisfaction (1-5)", Data = satisfaction.ToList() }
            }
        };
    }

    private static string FormatCurrency(decimal value)
    {
        return value.ToString("N0");
    }
}
