using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Dashboard.Data;
using Dashboard.Models;
using Dashboard.Models.ViewModels;
using Dashboard.Services.Interfaces;

namespace Dashboard.Jobs;

public class NotificationJobsRegistrar
{
    private static bool _registered = false;
    private static readonly object _lock = new();

    public static void RegisterJobs()
    {
        lock (_lock)
        {
            if (_registered) return;
            _registered = true;

            RecurringJob.AddOrUpdate<FinanceNotificationJob>(
                "finance-notifications",
                job => job.ExecuteAsync(),
                "*/5 * * * *");

            RecurringJob.AddOrUpdate<InventoryNotificationJob>(
                "inventory-notifications",
                job => job.ExecuteAsync(),
                "*/5 * * * *");

            RecurringJob.AddOrUpdate<HumanResourcesNotificationJob>(
                "hr-notifications",
                job => job.ExecuteAsync(),
                "*/30 * * * *");

            RecurringJob.AddOrUpdate<SalesNotificationJob>(
                "sales-notifications",
                job => job.ExecuteAsync(),
                "*/10 * * * *");

            RecurringJob.AddOrUpdate<CustomerServiceNotificationJob>(
                "cs-notifications",
                job => job.ExecuteAsync(),
                "*/5 * * * *");

            RecurringJob.AddOrUpdate<MarketingNotificationJob>(
                "marketing-notifications",
                job => job.ExecuteAsync(),
                "*/30 * * * *");

            RecurringJob.AddOrUpdate<ExecutiveSummaryJob>(
                "executive-summary",
                job => job.ExecuteAsync(),
                "0 8 * * *");
        }
    }

    public static void AddOrUpdateJob(string jobId, Type jobType, string cron)
    {
        var method = typeof(RecurringJob).GetMethod("AddOrUpdate", new[] { typeof(string), typeof(string), typeof(string) });
        if (method != null)
        {
            method.MakeGenericMethod(jobType).Invoke(null, new object[] { jobId, "ExecuteAsync", cron });
        }
    }

    public static void EnqueueJob(IServiceProvider sp, Type jobType)
    {
        var job = sp.GetService(jobType);
        if (job == null) return;
        var method = jobType.GetMethod("ExecuteAsync");
        if (method == null) return;
        var task = (Task?)method.Invoke(job, null);
        if (task != null) task.GetAwaiter().GetResult();
    }
}

public class FinanceNotificationJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly INotificationService _notifService;
    private readonly INotificationConfigCache _configCache;

    public FinanceNotificationJob(
        IServiceScopeFactory scopeFactory,
        INotificationService notifService,
        INotificationConfigCache configCache)
    {
        _scopeFactory = scopeFactory;
        _notifService = notifService;
        _configCache = configCache;
    }

    public async Task ExecuteAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // 1. FIN_OVERDUE_30D — Overdue receivables 30 days
        await CheckAndSendAsync(db, "FIN_OVERDUE_30D", async (cfg, dto) =>
        {
            var delayHours = cfg.DelayHours ?? 720;
            var cutoff = DateTime.Now.AddHours(-delayHours);
            var overdue = await db.SalesOrders
                .Include(o => o.Customer)
                .Where(o => o.PaymentStatus != "Paid" && o.OrderDate < cutoff)
                .AsNoTracking()
                .CountAsync();
            if (overdue == 0) return false;
            dto.Title = "Overdue receivables";
            dto.Message = $"{overdue} unpaid orders overdue {delayHours / 24} days";
            return true;
        });

        // 2. FIN_EXPENSE_PENDING — Expenses pending approval
        await CheckAndSendAsync(db, "FIN_EXPENSE_PENDING", async (cfg, dto) =>
        {
            var pending = await db.Expenses
                .Where(e => e.Status == "Pending")
                .AsNoTracking()
                .CountAsync();
            if (pending == 0) return false;
            dto.Title = "Expenses pending approval";
            dto.Message = $"{pending} expenses awaiting approval";
            return true;
        });

        // 3. FIN_OVER_BUDGET — Expenses over budget
        await CheckAndSendAsync(db, "FIN_OVER_BUDGET", async (cfg, dto) =>
        {
            var threshold = cfg.ThresholdValue ?? 0;
            var overBudget = await db.Expenses
                .Where(e => e.Status == "Approved" && e.Amount > threshold)
                .AsNoTracking()
                .CountAsync();
            if (overBudget == 0) return false;
            dto.Title = "Expenses over budget";
            dto.Message = $"{overBudget} expenses exceeding {threshold:N0} VND threshold";
            return true;
        });

        // 4. FIN_NEW_PAYMENT — New payment from customer
        await CheckAndSendAsync(db, "FIN_NEW_PAYMENT", async (cfg, dto) =>
        {
            var delayHours = cfg.DelayHours ?? 24;
            var cutoff = DateTime.Now.AddHours(-delayHours);
            var payments = await db.CustomerPayments
                .Include(p => p.Customer)
                .Where(p => p.PaymentDate >= cutoff)
                .AsNoTracking()
                .CountAsync();
            if (payments == 0) return false;
            dto.Title = "New payment from customer";
            dto.Message = $"{payments} new payments in the past {delayHours} hours";
            return true;
        });

        // 5. FIN_CASHFLOW_LOW — Abnormal cash flow
        await CheckAndSendAsync(db, "FIN_CASHFLOW_LOW", async (cfg, dto) =>
        {
            var threshold = cfg.ThresholdValue ?? 0;
            var today = DateTime.Today;
            var cashIn = await db.CustomerPayments
                .Where(p => p.PaymentDate >= today && p.PaymentDate < today.AddDays(1))
                .SumAsync(p => p.Amount);
            var cashOut = await db.Expenses
                .Where(e => e.ExpenseDate >= today && e.ExpenseDate < today.AddDays(1) && e.Status == "Approved")
                .SumAsync(e => e.Amount);
            var netCash = cashIn - cashOut;
            if (netCash >= threshold) return false;
            dto.Title = "Abnormal cash flow";
            dto.Message = $"Today's net cash is only {netCash:N0} VND";
            return true;
        });

        // 6. FIN_LARGE_INVOICE — Large unpaid invoices
        await CheckAndSendAsync(db, "FIN_LARGE_INVOICE", async (cfg, dto) =>
        {
            var threshold = cfg.ThresholdValue ?? 50_000_000m;
            var largeInvoices = await db.SalesInvoices
                .Include(i => i.Customer)
                .Where(i => i.PaymentStatus != "Paid" && i.TotalAmount > threshold)
                .AsNoTracking()
                .CountAsync();
            if (largeInvoices == 0) return false;
            dto.Title = "Large unpaid invoices";
            dto.Message = $"{largeInvoices} invoices exceeding {threshold:N0} VND unpaid";
            return true;
        });
    }

    private async Task CheckAndSendAsync(
        ApplicationDbContext db,
        string code,
        Func<NotificationConfig, NotificationSignalDto, Task<bool>> condition)
    {
        var config = await _configCache.GetConfigAsync(code);
        if (config == null || !config.IsEnabled) return;

        var dto = new NotificationSignalDto
        {
            Category = config.Category,
            Severity = config.Severity,
            IconClass = config.IconClass,
            IconBgClass = config.IconBgClass,
            ActionUrl = config.ActionUrl ?? "/Finance"
        };

        if (!await condition(config, dto)) return;

        var users = await db.Users
            .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id
                && db.Roles.Any(r => r.Name == "Finance" || r.Name == "Executive")))
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var userId in users)
            await _notifService.SendToUserAsync(userId, dto);
    }
}

public class InventoryNotificationJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly INotificationService _notifService;
    private readonly INotificationConfigCache _configCache;

    public InventoryNotificationJob(
        IServiceScopeFactory scopeFactory,
        INotificationService notifService,
        INotificationConfigCache configCache)
    {
        _scopeFactory = scopeFactory;
        _notifService = notifService;
        _configCache = configCache;
    }

    public async Task ExecuteAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // 7. INV_LOW_STOCK — Low inventory
        await CheckAndSendAsync(db, "INV_LOW_STOCK", async (cfg, dto) =>
        {
            var lowItems = await db.Inventories
                .Include(i => i.Product)
                .Where(i => i.QuantityAvailable <= i.ReorderPoint && i.QuantityAvailable > 0)
                .AsNoTracking()
                .CountAsync();
            if (lowItems == 0) return false;
            dto.Title = "Low inventory";
            dto.Message = $"{lowItems} products below reorder point";
            return true;
        });

        // 8. INV_OUT_OF_STOCK — Out of stock
        await CheckAndSendAsync(db, "INV_OUT_OF_STOCK", async (cfg, dto) =>
        {
            var outItems = await db.Inventories
                .Where(i => i.QuantityAvailable == 0)
                .AsNoTracking()
                .CountAsync();
            if (outItems == 0) return false;
            dto.Title = "Out of stock";
            dto.Message = $"{outItems} products out of stock";
            return true;
        });

        // 9. INV_PO_PENDING — Purchase order pending approval
        await CheckAndSendAsync(db, "INV_PO_PENDING", async (cfg, dto) =>
        {
            var pending = await db.PurchaseOrders
                .Where(po => po.Status == "Submitted")
                .AsNoTracking()
                .CountAsync();
            if (pending == 0) return false;
            dto.Title = "Purchase order pending approval";
            dto.Message = $"{pending} purchase orders awaiting approval";
            return true;
        });

        // 10. INV_NEW_RECEIPT — New stock receipt
        await CheckAndSendAsync(db, "INV_NEW_RECEIPT", async (cfg, dto) =>
        {
            var delayHours = cfg.DelayHours ?? 24;
            var cutoff = DateTime.Now.AddHours(-delayHours);
            var receipts = await db.PurchaseReceipts
                .Include(r => r.Supplier)
                .Where(r => r.ReceiptDate >= cutoff)
                .AsNoTracking()
                .CountAsync();
            if (receipts == 0) return false;
            dto.Title = "New stock receipt";
            dto.Message = $"{receipts} stock receipts in the past {delayHours} hours";
            return true;
        });
    }

    private async Task CheckAndSendAsync(
        ApplicationDbContext db,
        string code,
        Func<NotificationConfig, NotificationSignalDto, Task<bool>> condition)
    {
        var config = await _configCache.GetConfigAsync(code);
        if (config == null || !config.IsEnabled) return;

        var dto = new NotificationSignalDto
        {
            Category = config.Category,
            Severity = config.Severity,
            IconClass = config.IconClass,
            IconBgClass = config.IconBgClass,
            ActionUrl = config.ActionUrl ?? "/Inventory"
        };

        if (!await condition(config, dto)) return;

        var users = await db.Users
            .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id
                && db.Roles.Any(r => r.Name == "Inventory" || r.Name == "Executive")))
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var userId in users)
            await _notifService.SendToUserAsync(userId, dto);
    }
}

public class HumanResourcesNotificationJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly INotificationService _notifService;
    private readonly INotificationConfigCache _configCache;

    public HumanResourcesNotificationJob(
        IServiceScopeFactory scopeFactory,
        INotificationService notifService,
        INotificationConfigCache configCache)
    {
        _scopeFactory = scopeFactory;
        _notifService = notifService;
        _configCache = configCache;
    }

    public async Task ExecuteAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // 11. HR_LEAVE_PENDING — Leave request pending approval
        await CheckAndSendAsync(db, "HR_LEAVE_PENDING", async (cfg, dto) =>
        {
            var pending = await db.LeaveRequests
                .Where(l => l.Status == "Pending")
                .AsNoTracking()
                .CountAsync();
            if (pending == 0) return false;
            dto.Title = "Leave request pending approval";
            dto.Message = $"{pending} leave requests awaiting processing";
            return true;
        });

        // 12. HR_PROBATION_ENDING — Employee ending probation
        await CheckAndSendAsync(db, "HR_PROBATION_ENDING", async (cfg, dto) =>
        {
            var delayHours = cfg.DelayHours ?? 168;
            var probationEnding = await db.Employees
                .Where(e => e.IsActive
                    && e.EmploymentType == "Probation"
                    && e.HireDate.AddHours(89) <= DateTime.Now
                    && e.HireDate.AddHours(delayHours) >= DateTime.Now)
                .AsNoTracking()
                .CountAsync();
            if (probationEnding == 0) return false;
            dto.Title = "Employee ending probation";
            dto.Message = $"{probationEnding} employees ending probation in next 7 days";
            return true;
        });

        // 13. HR_HIGH_TURNOVER — High turnover rate
        await CheckAndSendAsync(db, "HR_HIGH_TURNOVER", async (cfg, dto) =>
        {
            var threshold = cfg.ThresholdValue ?? 5m;
            var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var totalEmployees = await db.Employees.CountAsync(e => e.IsActive);
            if (totalEmployees == 0) return false;
            var resignedThisMonth = await db.Employees
                .Where(e => e.TerminationDate.HasValue && e.TerminationDate >= startOfMonth)
                .AsNoTracking()
                .CountAsync();
            var turnoverRate = (decimal)resignedThisMonth / totalEmployees * 100;
            if (turnoverRate < threshold) return false;
            dto.Title = "High turnover rate";
            dto.Message = $"{resignedThisMonth} employees ({turnoverRate:N1}%) left this month, exceeding {threshold}% threshold";
            return true;
        });

        // 14. HR_NEW_APPLICANT — New applicant
        await CheckAndSendAsync(db, "HR_NEW_APPLICANT", async (cfg, dto) =>
        {
            var delayHours = cfg.DelayHours ?? 24;
            var cutoff = DateTime.Now.AddHours(-delayHours);
            var newApplicants = await db.Applicants
                .Where(a => a.AppliedDate >= cutoff)
                .AsNoTracking()
                .CountAsync();
            if (newApplicants == 0) return false;
            dto.Title = "New applicant";
            dto.Message = $"{newApplicants} new applicants in the past {delayHours} hours";
            return true;
        });

        // 15. HR_BIRTHDAY — Employee birthday
        await CheckAndSendAsync(db, "HR_BIRTHDAY", async (cfg, dto) =>
        {
            var today = DateTime.Today;
            var birthdays = await db.Employees
                .Where(e => e.IsActive
                    && e.DateOfBirth.HasValue
                    && e.DateOfBirth.Value.Month == today.Month
                    && e.DateOfBirth.Value.Day == today.Day)
                .AsNoTracking()
                .ToListAsync();
            if (birthdays.Count == 0) return false;
            foreach (var emp in birthdays)
            {
                dto.Title = "Employee birthday";
                dto.Message = $"Happy birthday {emp.FullName}!";
                var users = await db.Users.Select(u => u.Id).ToListAsync();
                foreach (var userId in users)
                    await _notifService.SendToUserAsync(userId, dto);
            }
            return false;
        });

        // 16. HR_NEW_EMPLOYEE — New employee onboarded
        await CheckAndSendAsync(db, "HR_NEW_EMPLOYEE", async (cfg, dto) =>
        {
            var delayHours = cfg.DelayHours ?? 168;
            var cutoff = DateTime.Now.AddHours(-delayHours);
            var newEmps = await db.Employees
                .Where(e => e.IsActive && e.HireDate >= cutoff)
                .AsNoTracking()
                .CountAsync();
            if (newEmps == 0) return false;
            dto.Title = "New employee onboarded";
            dto.Message = $"{newEmps} new employees in the past 7 days";
            return true;
        });
    }

    private async Task CheckAndSendAsync(
        ApplicationDbContext db,
        string code,
        Func<NotificationConfig, NotificationSignalDto, Task<bool>> condition)
    {
        var config = await _configCache.GetConfigAsync(code);
        if (config == null || !config.IsEnabled) return;

        var dto = new NotificationSignalDto
        {
            Category = config.Category,
            Severity = config.Severity,
            IconClass = config.IconClass,
            IconBgClass = config.IconBgClass,
            ActionUrl = config.ActionUrl ?? "/HumanResources"
        };

        if (!await condition(config, dto)) return;

        var users = await db.Users
            .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id
                && db.Roles.Any(r => r.Name == "HumanResources" || r.Name == "Executive")))
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var userId in users)
            await _notifService.SendToUserAsync(userId, dto);
    }
}

public class SalesNotificationJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly INotificationService _notifService;
    private readonly INotificationConfigCache _configCache;

    public SalesNotificationJob(
        IServiceScopeFactory scopeFactory,
        INotificationService notifService,
        INotificationConfigCache configCache)
    {
        _scopeFactory = scopeFactory;
        _notifService = notifService;
        _configCache = configCache;
    }

    public async Task ExecuteAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // 17. SAL_NEW_ORDER — New order
        await CheckAndSendAsync(db, "SAL_NEW_ORDER", async (cfg, dto) =>
        {
            var delayHours = cfg.DelayHours ?? 1;
            var cutoff = DateTime.Now.AddHours(-delayHours);
            var newOrders = await db.SalesOrders
                .Where(o => o.CreatedAt >= cutoff)
                .AsNoTracking()
                .CountAsync();
            if (newOrders == 0) return false;
            dto.Title = "New order";
            dto.Message = $"{newOrders} orders created in the past {delayHours} hours";
            return true;
        });

        // 18. SAL_LARGE_ORDER — Large order
        await CheckAndSendAsync(db, "SAL_LARGE_ORDER", async (cfg, dto) =>
        {
            var threshold = cfg.ThresholdValue ?? 100_000_000m;
            var cutoff = DateTime.Now.AddHours(-24);
            var largeOrders = await db.SalesOrders
                .Include(o => o.Customer)
                .Where(o => o.TotalAmount > threshold && o.CreatedAt >= cutoff)
                .AsNoTracking()
                .ToListAsync();
            if (largeOrders.Count == 0) return false;
            foreach (var order in largeOrders)
            {
                dto.Title = "Large order";
                dto.Message = $"Order #{order.OrderNumber} with amount {order.TotalAmount:N0} VND";
                var users = await db.Users
                    .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id
                        && db.Roles.Any(r => r.Name == "Sales" || r.Name == "Executive")))
                    .Select(u => u.Id)
                    .ToListAsync();
                foreach (var userId in users)
                    await _notifService.SendToUserAsync(userId, dto);
            }
            return false;
        });

        // 19. SAL_DELIVERY_DELAYED — Order delivery delayed > 3 days
        await CheckAndSendAsync(db, "SAL_DELIVERY_DELAYED", async (cfg, dto) =>
        {
            var delayDays = (cfg.DelayHours ?? 72) / 24;
            var cutoff = DateTime.Today.AddDays(-delayDays);
            var delayed = await db.SalesOrders
                .Where(o => o.DeliveryStatus == "Pending"
                    && o.OrderDate < cutoff
                    && o.PaymentStatus == "Paid")
                .AsNoTracking()
                .CountAsync();
            if (delayed == 0) return false;
            dto.Title = "Order delivery delayed";
            dto.Message = $"{delayed} paid orders pending delivery for over {delayDays} days";
            return true;
        });

        // 20. SAL_OPP_STAGE_CHANGED — Opportunity stage changed
        await CheckAndSendAsync(db, "SAL_OPP_STAGE_CHANGED", async (cfg, dto) =>
        {
            var delayHours = cfg.DelayHours ?? 1;
            var cutoff = DateTime.Now.AddHours(-delayHours);
            var changed = await db.OpportunityStageHistories
                .Include(h => h.Opportunity)
                .Where(h => h.ChangedAt >= cutoff)
                .AsNoTracking()
                .CountAsync();
            if (changed == 0) return false;
            dto.Title = "Opportunity stage changed";
            dto.Message = $"{changed} sales opportunities changed stage in the past {delayHours} hours";
            return true;
        });

        // 21. SAL_TARGET_ACHIEVED — Target achieved monthly/quarterly
        await CheckAndSendAsync(db, "SAL_TARGET_ACHIEVED", async (cfg, dto) =>
        {
            var today = DateTime.Today;
            var targets = await db.KpiTargets
                .Where(t => t.Status == "Active")
                .AsNoTracking()
                .ToListAsync();
            var achieved = new List<string>();
            foreach (var target in targets)
            {
                decimal actualRevenue = 0;
                if (target.KpiType == "Monthly")
                {
                    var startOfMonth = new DateTime(today.Year, today.Month, 1);
                    actualRevenue = await db.SalesOrders
                        .Where(s => s.CreatedAt >= startOfMonth && s.PaymentStatus == "Paid")
                        .SumAsync(s => s.TotalAmount);
                }
                else if (target.KpiType == "Quarterly")
                {
                    var quarter = (today.Month - 1) / 3;
                    var startOfQuarter = new DateTime(today.Year, quarter * 3 + 1, 1);
                    actualRevenue = await db.SalesOrders
                        .Where(s => s.CreatedAt >= startOfQuarter && s.PaymentStatus == "Paid")
                        .SumAsync(s => s.TotalAmount);
                }
                if (actualRevenue >= target.TargetValue)
                    achieved.Add($"{target.KpiName} ({actualRevenue:N0}/{target.TargetValue:N0})");
            }
            if (achieved.Count == 0) return false;
            dto.Title = "Target achieved";
            dto.Message = $"Achieved: {string.Join(", ", achieved)}";
            return true;
        });

        // 22. SAL_NEW_CUSTOMER — New customer
        await CheckAndSendAsync(db, "SAL_NEW_CUSTOMER", async (cfg, dto) =>
        {
            var delayHours = cfg.DelayHours ?? 24;
            var cutoff = DateTime.Now.AddHours(-delayHours);
            var newCust = await db.Customers
                .Where(c => c.CreatedAt >= cutoff)
                .AsNoTracking()
                .CountAsync();
            if (newCust == 0) return false;
            dto.Title = "New customer";
            dto.Message = $"{newCust} new customers in the past {delayHours} hours";
            return true;
        });
    }

    private async Task CheckAndSendAsync(
        ApplicationDbContext db,
        string code,
        Func<NotificationConfig, NotificationSignalDto, Task<bool>> condition)
    {
        var config = await _configCache.GetConfigAsync(code);
        if (config == null || !config.IsEnabled) return;

        var dto = new NotificationSignalDto
        {
            Category = config.Category,
            Severity = config.Severity,
            IconClass = config.IconClass,
            IconBgClass = config.IconBgClass,
            ActionUrl = config.ActionUrl ?? "/Sales"
        };

        if (!await condition(config, dto)) return;

        var users = await db.Users
            .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id
                && db.Roles.Any(r => r.Name == "Sales" || r.Name == "Executive")))
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var userId in users)
            await _notifService.SendToUserAsync(userId, dto);
    }
}

public class CustomerServiceNotificationJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly INotificationService _notifService;
    private readonly INotificationConfigCache _configCache;

    public CustomerServiceNotificationJob(
        IServiceScopeFactory scopeFactory,
        INotificationService notifService,
        INotificationConfigCache configCache)
    {
        _scopeFactory = scopeFactory;
        _notifService = notifService;
        _configCache = configCache;
    }

    public async Task ExecuteAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // 23. CS_NEW_TICKET — New ticket
        await CheckAndSendAsync(db, "CS_NEW_TICKET", async (cfg, dto) =>
        {
            var delayHours = cfg.DelayHours ?? 1;
            var cutoff = DateTime.Now.AddHours(-delayHours);
            var newTickets = await db.SupportTickets
                .Where(t => t.CreatedAt >= cutoff)
                .AsNoTracking()
                .CountAsync();
            if (newTickets == 0) return false;
            dto.Title = "New ticket";
            dto.Message = $"{newTickets} tickets created in the past {delayHours} hours";
            return true;
        });

        // 24. CS_HIGH_PRIORITY — High priority ticket
        await CheckAndSendAsync(db, "CS_HIGH_PRIORITY", async (cfg, dto) =>
        {
            var highPriority = await db.SupportTickets
                .Where(t => (t.Priority == "Critical" || t.Priority == "High") && t.Status == "Open")
                .AsNoTracking()
                .CountAsync();
            if (highPriority == 0) return false;
            dto.Title = "High priority ticket";
            dto.Message = $"{highPriority} high priority tickets not yet processed";
            return true;
        });

        // 25. CS_TICKET_SLA_BREACH — Ticket SLA breach
        await CheckAndSendAsync(db, "CS_TICKET_SLA_BREACH", async (cfg, dto) =>
        {
            var delayHours = cfg.DelayHours ?? 24;
            var cutoff = DateTime.Now.AddHours(-delayHours);
            var overdue = await db.SupportTickets
                .Where(t => t.CreatedAt < cutoff && t.Status != "Resolved")
                .AsNoTracking()
                .CountAsync();
            if (overdue == 0) return false;
            dto.Title = "Ticket SLA breach";
            dto.Message = $"{overdue} tickets overdue {delayHours} hours unresolved";
            return true;
        });

        // 26. CS_TICKET_NO_RESPONSE — Ticket awaiting response
        await CheckAndSendAsync(db, "CS_TICKET_NO_RESPONSE", async (cfg, dto) =>
        {
            var delayHours = cfg.DelayHours ?? 4;
            var cutoff = DateTime.Now.AddHours(-delayHours);
            var waiting = await db.SupportTickets
                .Where(t => t.Status == "In Progress" && t.CreatedAt < cutoff)
                .AsNoTracking()
                .CountAsync();
            if (waiting == 0) return false;
            dto.Title = "Ticket awaiting response";
            dto.Message = $"{waiting} tickets awaiting response for over {delayHours} hours";
            return true;
        });

        // 27. CS_LOW_SATISFACTION — Customer dissatisfaction
        await CheckAndSendAsync(db, "CS_LOW_SATISFACTION", async (cfg, dto) =>
        {
            var cutoff = DateTime.Now.AddHours(-24);
            var lowSat = await db.SupportTickets
                .Where(t => t.Status == "Resolved"
                    && t.ResolvedDate.HasValue
                    && t.ResolvedDate >= cutoff)
                .AsNoTracking()
                .CountAsync();
            if (lowSat == 0) return false;
            dto.Title = "Customer dissatisfaction";
            dto.Message = $"{lowSat} tickets resolved in the past 24 hours, please check customer feedback";
            return true;
        });
    }

    private async Task CheckAndSendAsync(
        ApplicationDbContext db,
        string code,
        Func<NotificationConfig, NotificationSignalDto, Task<bool>> condition)
    {
        var config = await _configCache.GetConfigAsync(code);
        if (config == null || !config.IsEnabled) return;

        var dto = new NotificationSignalDto
        {
            Category = config.Category,
            Severity = config.Severity,
            IconClass = config.IconClass,
            IconBgClass = config.IconBgClass,
            ActionUrl = config.ActionUrl ?? "/CustomerService"
        };

        if (!await condition(config, dto)) return;

        var users = await db.Users
            .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id
                && db.Roles.Any(r => r.Name == "CustomerService" || r.Name == "Executive")))
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var userId in users)
            await _notifService.SendToUserAsync(userId, dto);
    }
}

public class MarketingNotificationJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly INotificationService _notifService;
    private readonly INotificationConfigCache _configCache;

    public MarketingNotificationJob(
        IServiceScopeFactory scopeFactory,
        INotificationService notifService,
        INotificationConfigCache configCache)
    {
        _scopeFactory = scopeFactory;
        _notifService = notifService;
        _configCache = configCache;
    }

    public async Task ExecuteAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // 28. MKT_BUDGET_80 — Campaign budget almost exhausted
        await CheckAndSendAsync(db, "MKT_BUDGET_80", async (cfg, dto) =>
        {
            var campaigns = await db.MarketingCampaigns
                .Where(c => c.Budget > 0
                    && (decimal)c.ActualSpend >= (decimal)(c.Budget * 0.8m)
                    && (decimal)c.ActualSpend <= c.Budget
                    && c.IsActive)
                .AsNoTracking()
                .ToListAsync();
            if (campaigns.Count == 0) return false;
            dto.Title = "Campaign budget almost exhausted";
            dto.Message = $"{campaigns.Count} campaigns using over 80% of budget";
            return true;
        });

        // 29. MKT_BUDGET_EXCEEDED — Campaign exceeded budget
        await CheckAndSendAsync(db, "MKT_BUDGET_EXCEEDED", async (cfg, dto) =>
        {
            var exceeded = await db.MarketingCampaigns
                .Where(c => c.Budget > 0
                    && (decimal)c.ActualSpend > c.Budget
                    && c.IsActive)
                .AsNoTracking()
                .CountAsync();
            if (exceeded == 0) return false;
            dto.Title = "Campaign exceeded budget";
            dto.Message = $"{exceeded} campaigns exceeded allocated budget";
            return true;
        });

        // 30. MKT_NEW_LEAD — New lead
        await CheckAndSendAsync(db, "MKT_NEW_LEAD", async (cfg, dto) =>
        {
            var delayHours = cfg.DelayHours ?? 24;
            var cutoff = DateTime.Now.AddHours(-delayHours);
            var newLeads = await db.MarketingLeads
                .Where(l => l.CreatedDate >= cutoff)
                .AsNoTracking()
                .CountAsync();
            if (newLeads == 0) return false;
            dto.Title = "New lead";
            dto.Message = $"{newLeads} new leads in the past {delayHours} hours";
            return true;
        });

        // 31. MKT_LEAD_CONVERTED — Lead converted
        await CheckAndSendAsync(db, "MKT_LEAD_CONVERTED", async (cfg, dto) =>
        {
            var delayHours = cfg.DelayHours ?? 24;
            var cutoff = DateTime.Now.AddHours(-delayHours);
            var converted = await db.MarketingLeads
                .Where(l => l.Status == "Converted" && l.ConvertedDate >= cutoff)
                .AsNoTracking()
                .CountAsync();
            if (converted == 0) return false;
            dto.Title = "Lead converted";
            dto.Message = $"{converted} leads converted in the past {delayHours} hours";
            return true;
        });

        // 32. MKT_LOW_ROAS — Low ROAS
        await CheckAndSendAsync(db, "MKT_LOW_ROAS", async (cfg, dto) =>
        {
            var threshold = cfg.ThresholdValue ?? 1m;
            var yesterday = DateTime.Today.AddDays(-1);
            var recentSpends = await db.MarketingSpendDailies
                .Where(s => s.SpendDate == yesterday)
                .AsNoTracking()
                .ToListAsync();
            var lowROAS = recentSpends.Count(s => s.CPA.HasValue && s.CPA > threshold);
            if (lowROAS == 0) return false;
            dto.Title = "Low ROAS";
            dto.Message = $"{lowROAS} campaigns with ROAS below {threshold} yesterday";
            return true;
        });
    }

    private async Task CheckAndSendAsync(
        ApplicationDbContext db,
        string code,
        Func<NotificationConfig, NotificationSignalDto, Task<bool>> condition)
    {
        var config = await _configCache.GetConfigAsync(code);
        if (config == null || !config.IsEnabled) return;

        var dto = new NotificationSignalDto
        {
            Category = config.Category,
            Severity = config.Severity,
            IconClass = config.IconClass,
            IconBgClass = config.IconBgClass,
            ActionUrl = config.ActionUrl ?? "/Marketing"
        };

        if (!await condition(config, dto)) return;

        var users = await db.Users
            .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id
                && db.Roles.Any(r => r.Name == "Marketing" || r.Name == "Executive")))
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var userId in users)
            await _notifService.SendToUserAsync(userId, dto);
    }
}

public class ExecutiveSummaryJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly INotificationService _notifService;
    private readonly INotificationConfigCache _configCache;

    public ExecutiveSummaryJob(
        IServiceScopeFactory scopeFactory,
        INotificationService notifService,
        INotificationConfigCache configCache)
    {
        _scopeFactory = scopeFactory;
        _notifService = notifService;
        _configCache = configCache;
    }

    public async Task ExecuteAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var today = DateTime.Today;
        var yesterday = today.AddDays(-1);

        // 33. EXE_DAILY_DIGEST — Daily activity summary
        await CheckAndSendAsync(db, "EXE_DAILY_DIGEST", async (cfg, dto) =>
        {
            var orders = await db.SalesOrders
                .Where(s => s.CreatedAt >= yesterday)
                .CountAsync();
            var revenue = await db.SalesOrders
                .Where(s => s.CreatedAt >= yesterday && s.PaymentStatus == "Paid")
                .SumAsync(s => s.TotalAmount);
            var tickets = await db.SupportTickets
                .Where(t => t.CreatedAt >= yesterday)
                .CountAsync();
            var expenses = await db.Expenses
                .Where(e => e.ExpenseDate >= yesterday && e.Status == "Pending")
                .CountAsync();
            dto.Title = "Daily activity summary";
            dto.Message = $"Orders: {orders} | Revenue: {revenue:N0} VND | Tickets: {tickets} | Expenses pending: {expenses}";
            return true;
        });

        // 34. EXE_DAILY_REPORT — Daily report ready
        await CheckAndSendAsync(db, "EXE_DAILY_REPORT", async (cfg, dto) =>
        {
            dto.Title = "Daily report ready";
            dto.Message = $"Daily report for {today:dd/MM/yyyy} is ready. Please check at /Executive.";
            return true;
        });

        // 35. EXE_KPI_ANOMALY — KPI anomaly warning
        await CheckAndSendAsync(db, "EXE_KPI_ANOMALY", async (cfg, dto) =>
        {
            var threshold = cfg.ThresholdValue ?? 20m;
            var anomalies = new List<string>();
            var targets = await db.KpiTargets
                .Where(t => t.Status == "Active")
                .AsNoTracking()
                .ToListAsync();
            foreach (var target in targets)
            {
                var startOfMonth = new DateTime(today.Year, today.Month, 1);
                var actual = await db.SalesOrders
                    .Where(s => s.CreatedAt >= startOfMonth && s.PaymentStatus == "Paid")
                    .SumAsync(s => s.TotalAmount);
                if (target.TargetValue > 0)
                {
                    var deviation = Math.Abs((actual - target.TargetValue) / target.TargetValue * 100);
                    if (deviation >= threshold)
                        anomalies.Add($"{target.KpiName} (deviation {deviation:N0}%)");
                }
            }
            if (anomalies.Count == 0) return false;
            dto.Title = "KPI anomaly warning";
            dto.Message = $"Abnormal KPIs: {string.Join(", ", anomalies)}";
            return true;
        });
    }

    private async Task CheckAndSendAsync(
        ApplicationDbContext db,
        string code,
        Func<NotificationConfig, NotificationSignalDto, Task<bool>> condition)
    {
        var config = await _configCache.GetConfigAsync(code);
        if (config == null || !config.IsEnabled) return;

        var dto = new NotificationSignalDto
        {
            Category = config.Category,
            Severity = config.Severity,
            IconClass = config.IconClass,
            IconBgClass = config.IconBgClass,
            ActionUrl = config.ActionUrl ?? "/Executive"
        };

        if (!await condition(config, dto)) return;

        var users = await db.Users
            .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id
                && db.Roles.Any(r => r.Name == "Executive")))
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var userId in users)
            await _notifService.SendToUserAsync(userId, dto);
    }
}
