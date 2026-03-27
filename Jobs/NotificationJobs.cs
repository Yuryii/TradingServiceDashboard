using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Dashboard.Data;
using Dashboard.Models;
using Dashboard.Models.ViewModels;
using Dashboard.Services.Interfaces;

namespace Dashboard.Jobs;

public static class NotificationJobs
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

        // 1. FIN_OVERDUE_30D — Cong no qua han 30 ngay
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
            dto.Title = "Cong no qua han";
            dto.Message = $"{overdue} don hang chua thanh toan qua {delayHours / 24} ngay";
            return true;
        });

        // 2. FIN_EXPENSE_PENDING — Chi phi cho phe duyet
        await CheckAndSendAsync(db, "FIN_EXPENSE_PENDING", async (cfg, dto) =>
        {
            var pending = await db.Expenses
                .Where(e => e.Status == "Pending")
                .AsNoTracking()
                .CountAsync();
            if (pending == 0) return false;
            dto.Title = "Chi phi cho phe duyet";
            dto.Message = $"{pending} chi phi dang cho xu ly phe duyet";
            return true;
        });

        // 3. FIN_OVER_BUDGET — Chi phi vuot ngan sach
        await CheckAndSendAsync(db, "FIN_OVER_BUDGET", async (cfg, dto) =>
        {
            var threshold = cfg.ThresholdValue ?? 0;
            var overBudget = await db.Expenses
                .Where(e => e.Status == "Approved" && e.Amount > threshold)
                .AsNoTracking()
                .CountAsync();
            if (overBudget == 0) return false;
            dto.Title = "Chi phi vuot ngan sach";
            dto.Message = $"{overBudget} chi phi vuot nguong {threshold:N0} VND";
            return true;
        });

        // 4. FIN_NEW_PAYMENT — Thanh toan moi tu KH
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
            dto.Title = "Thanh toan moi tu KH";
            dto.Message = $"{payments} thanh toan moi trong {delayHours} gio qua";
            return true;
        });

        // 5. FIN_CASHFLOW_LOW — Dong tien bat thuong
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
            dto.Title = "Dong tien bat thuong";
            dto.Message = $"Dong tien hom nay chi con {netCash:N0} VND";
            return true;
        });

        // 6. FIN_LARGE_INVOICE — Hoa don lon chua thanh toan
        await CheckAndSendAsync(db, "FIN_LARGE_INVOICE", async (cfg, dto) =>
        {
            var threshold = cfg.ThresholdValue ?? 50_000_000m;
            var largeInvoices = await db.SalesInvoices
                .Include(i => i.Customer)
                .Where(i => i.PaymentStatus != "Paid" && i.TotalAmount > threshold)
                .AsNoTracking()
                .CountAsync();
            if (largeInvoices == 0) return false;
            dto.Title = "Hoa don lon chua thanh toan";
            dto.Message = $"{largeInvoices} hoa don vuot {threshold:N0} VND chua thanh toan";
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

        // 7. INV_LOW_STOCK — Ton kho thap
        await CheckAndSendAsync(db, "INV_LOW_STOCK", async (cfg, dto) =>
        {
            var lowItems = await db.Inventories
                .Include(i => i.Product)
                .Where(i => i.QuantityAvailable <= i.ReorderPoint && i.QuantityAvailable > 0)
                .AsNoTracking()
                .CountAsync();
            if (lowItems == 0) return false;
            dto.Title = "Ton kho thap";
            dto.Message = $"{lowItems} san pham con thap hon diem dat hang";
            return true;
        });

        // 8. INV_OUT_OF_STOCK — Het hang
        await CheckAndSendAsync(db, "INV_OUT_OF_STOCK", async (cfg, dto) =>
        {
            var outItems = await db.Inventories
                .Where(i => i.QuantityAvailable == 0)
                .AsNoTracking()
                .CountAsync();
            if (outItems == 0) return false;
            dto.Title = "Het hang";
            dto.Message = $"{outItems} san pham da het hang";
            return true;
        });

        // 9. INV_PO_PENDING — Don mua hang cho phe duyet
        await CheckAndSendAsync(db, "INV_PO_PENDING", async (cfg, dto) =>
        {
            var pending = await db.PurchaseOrders
                .Where(po => po.Status == "Submitted")
                .AsNoTracking()
                .CountAsync();
            if (pending == 0) return false;
            dto.Title = "Don mua hang cho phe duyet";
            dto.Message = $"{pending} don mua hang cho xu ly phe duyet";
            return true;
        });

        // 10. INV_NEW_RECEIPT — Nhap kho moi
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
            dto.Title = "Nhap kho moi";
            dto.Message = $"{receipts} phieu nhap kho trong {delayHours} gio qua";
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

        // 11. HR_LEAVE_PENDING — Don nghi phep cho phe duyet
        await CheckAndSendAsync(db, "HR_LEAVE_PENDING", async (cfg, dto) =>
        {
            var pending = await db.LeaveRequests
                .Where(l => l.Status == "Pending")
                .AsNoTracking()
                .CountAsync();
            if (pending == 0) return false;
            dto.Title = "Don nghi phep cho phe duyet";
            dto.Message = $"{pending} don nghi phep dang cho xu ly";
            return true;
        });

        // 12. HR_PROBATION_ENDING — Nhan vien sap het thu viec
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
            dto.Title = "Nhan vien sap het thu viec";
            dto.Message = $"{probationEnding} nhan vien sap ket thuc thu viec trong 7 ngay toi";
            return true;
        });

        // 13. HR_HIGH_TURNOVER — Ty le nghi viec cao
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
            dto.Title = "Ty le nghi viec cao";
            dto.Message = $"{resignedThisMonth} nhan vien ({turnoverRate:N1}%) nghi viec thang nay, vuot nguong {threshold}%";
            return true;
        });

        // 14. HR_NEW_APPLICANT — Ung vien moi
        await CheckAndSendAsync(db, "HR_NEW_APPLICANT", async (cfg, dto) =>
        {
            var delayHours = cfg.DelayHours ?? 24;
            var cutoff = DateTime.Now.AddHours(-delayHours);
            var newApplicants = await db.Applicants
                .Where(a => a.AppliedDate >= cutoff)
                .AsNoTracking()
                .CountAsync();
            if (newApplicants == 0) return false;
            dto.Title = "Ung vien moi";
            dto.Message = $"{newApplicants} ung vien moi trong {delayHours} gio qua";
            return true;
        });

        // 15. HR_BIRTHDAY — Sinh nhat nhan vien
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
                dto.Title = "Sinh nhat nhan vien";
                dto.Message = $"Chuc mung sinh nhat {emp.FullName}!";
                var users = await db.Users.Select(u => u.Id).ToListAsync();
                foreach (var userId in users)
                    await _notifService.SendToUserAsync(userId, dto);
            }
            return false;
        });

        // 16. HR_NEW_EMPLOYEE — Nhan vien moi nhan viec
        await CheckAndSendAsync(db, "HR_NEW_EMPLOYEE", async (cfg, dto) =>
        {
            var delayHours = cfg.DelayHours ?? 168;
            var cutoff = DateTime.Now.AddHours(-delayHours);
            var newEmps = await db.Employees
                .Where(e => e.IsActive && e.HireDate >= cutoff)
                .AsNoTracking()
                .CountAsync();
            if (newEmps == 0) return false;
            dto.Title = "Nhan vien moi nhan viec";
            dto.Message = $"{newEmps} nhan vien moi trong 7 ngay qua";
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

        // 17. SAL_NEW_ORDER — Don hang moi
        await CheckAndSendAsync(db, "SAL_NEW_ORDER", async (cfg, dto) =>
        {
            var delayHours = cfg.DelayHours ?? 1;
            var cutoff = DateTime.Now.AddHours(-delayHours);
            var newOrders = await db.SalesOrders
                .Where(o => o.CreatedAt >= cutoff)
                .AsNoTracking()
                .CountAsync();
            if (newOrders == 0) return false;
            dto.Title = "Don hang moi";
            dto.Message = $"{newOrders} don hang duoc tao trong {delayHours} gio qua";
            return true;
        });

        // 18. SAL_LARGE_ORDER — Don hang lon
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
                dto.Title = "Don hang lon";
                dto.Message = $"Don #{order.OrderNumber} voi so tien {order.TotalAmount:N0} VND";
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

        // 19. SAL_DELIVERY_DELAYED — Don cho giao > 3 ngay
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
            dto.Title = "Don cho giao qua han";
            dto.Message = $"{delayed} don hang da thanh toan nhung cho giao qua {delayDays} ngay";
            return true;
        });

        // 20. SAL_OPP_STAGE_CHANGED — Co hoi chuyen stage
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
            dto.Title = "Co hoi chuyen stage";
            dto.Message = $"{changed} co hoi ban hang thay doi stage trong {delayHours} gio qua";
            return true;
        });

        // 21. SAL_TARGET_ACHIEVED — Dat muc tieu thang/quy
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
            dto.Title = "Dat muc tieu";
            dto.Message = $"Da dat: {string.Join(", ", achieved)}";
            return true;
        });

        // 22. SAL_NEW_CUSTOMER — Khach hang moi
        await CheckAndSendAsync(db, "SAL_NEW_CUSTOMER", async (cfg, dto) =>
        {
            var delayHours = cfg.DelayHours ?? 24;
            var cutoff = DateTime.Now.AddHours(-delayHours);
            var newCust = await db.Customers
                .Where(c => c.CreatedAt >= cutoff)
                .AsNoTracking()
                .CountAsync();
            if (newCust == 0) return false;
            dto.Title = "Khach hang moi";
            dto.Message = $"{newCust} khach hang moi trong {delayHours} gio qua";
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

        // 23. CS_NEW_TICKET — Ticket moi
        await CheckAndSendAsync(db, "CS_NEW_TICKET", async (cfg, dto) =>
        {
            var delayHours = cfg.DelayHours ?? 1;
            var cutoff = DateTime.Now.AddHours(-delayHours);
            var newTickets = await db.SupportTickets
                .Where(t => t.CreatedAt >= cutoff)
                .AsNoTracking()
                .CountAsync();
            if (newTickets == 0) return false;
            dto.Title = "Ticket moi";
            dto.Message = $"{newTickets} ticket duoc tao trong {delayHours} gio qua";
            return true;
        });

        // 24. CS_HIGH_PRIORITY — Ticket uu tien cao
        await CheckAndSendAsync(db, "CS_HIGH_PRIORITY", async (cfg, dto) =>
        {
            var highPriority = await db.SupportTickets
                .Where(t => (t.Priority == "Critical" || t.Priority == "High") && t.Status == "Open")
                .AsNoTracking()
                .CountAsync();
            if (highPriority == 0) return false;
            dto.Title = "Ticket uu tien cao";
            dto.Message = $"{highPriority} ticket uu tien cao chua duoc xu ly";
            return true;
        });

        // 25. CS_TICKET_SLA_BREACH — Ticket qua han SLA
        await CheckAndSendAsync(db, "CS_TICKET_SLA_BREACH", async (cfg, dto) =>
        {
            var delayHours = cfg.DelayHours ?? 24;
            var cutoff = DateTime.Now.AddHours(-delayHours);
            var overdue = await db.SupportTickets
                .Where(t => t.CreatedAt < cutoff && t.Status != "Resolved")
                .AsNoTracking()
                .CountAsync();
            if (overdue == 0) return false;
            dto.Title = "Ticket qua han SLA";
            dto.Message = $"{overdue} ticket vuot qua {delayHours} gio ma chua giai quyet";
            return true;
        });

        // 26. CS_TICKET_NO_RESPONSE — Ticket cho phan hoi
        await CheckAndSendAsync(db, "CS_TICKET_NO_RESPONSE", async (cfg, dto) =>
        {
            var delayHours = cfg.DelayHours ?? 4;
            var cutoff = DateTime.Now.AddHours(-delayHours);
            var waiting = await db.SupportTickets
                .Where(t => t.Status == "In Progress" && t.CreatedAt < cutoff)
                .AsNoTracking()
                .CountAsync();
            if (waiting == 0) return false;
            dto.Title = "Ticket cho phan hoi";
            dto.Message = $"{waiting} ticket cho phan hoi qua {delayHours} gio";
            return true;
        });

        // 27. CS_LOW_SATISFACTION — KH khong hai long
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
            dto.Title = "KH khong hai long";
            dto.Message = $"{lowSat} ticket duoc giai quyet trong 24 gio qua, vui long kiem tra phan hoi KH";
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

        // 28. MKT_BUDGET_80 — Ngan sach campaign sap het
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
            dto.Title = "Ngan sach campaign sap het";
            dto.Message = $"{campaigns.Count} campaign da su dung tren 80% ngan sach";
            return true;
        });

        // 29. MKT_BUDGET_EXCEEDED — Campaign vuot ngan sach
        await CheckAndSendAsync(db, "MKT_BUDGET_EXCEEDED", async (cfg, dto) =>
        {
            var exceeded = await db.MarketingCampaigns
                .Where(c => c.Budget > 0
                    && (decimal)c.ActualSpend > c.Budget
                    && c.IsActive)
                .AsNoTracking()
                .CountAsync();
            if (exceeded == 0) return false;
            dto.Title = "Campaign vuot ngan sach";
            dto.Message = $"{exceeded} campaign da vuot ngan sach duoc phep";
            return true;
        });

        // 30. MKT_NEW_LEAD — Lead moi
        await CheckAndSendAsync(db, "MKT_NEW_LEAD", async (cfg, dto) =>
        {
            var delayHours = cfg.DelayHours ?? 24;
            var cutoff = DateTime.Now.AddHours(-delayHours);
            var newLeads = await db.MarketingLeads
                .Where(l => l.CreatedDate >= cutoff)
                .AsNoTracking()
                .CountAsync();
            if (newLeads == 0) return false;
            dto.Title = "Lead moi";
            dto.Message = $"{newLeads} lead moi trong {delayHours} gio qua";
            return true;
        });

        // 31. MKT_LEAD_CONVERTED — Lead duoc chuyen doi
        await CheckAndSendAsync(db, "MKT_LEAD_CONVERTED", async (cfg, dto) =>
        {
            var delayHours = cfg.DelayHours ?? 24;
            var cutoff = DateTime.Now.AddHours(-delayHours);
            var converted = await db.MarketingLeads
                .Where(l => l.Status == "Converted" && l.ConvertedDate >= cutoff)
                .AsNoTracking()
                .CountAsync();
            if (converted == 0) return false;
            dto.Title = "Lead duoc chuyen doi";
            dto.Message = $"{converted} lead duoc chuyen doi trong {delayHours} gio qua";
            return true;
        });

        // 32. MKT_LOW_ROAS — ROAS thap
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
            dto.Title = "ROAS thap";
            dto.Message = $"{lowROAS} campaign co ROAS thap hon {threshold} hom qua";
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

        // 33. EXE_DAILY_DIGEST — Tom tat hoat dong ngay
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
            dto.Title = "Tom tat hoat dong ngay";
            dto.Message = $"Don hang: {orders} | Doanh thu: {revenue:N0} VND | Ticket: {tickets} | Chi phi cho duyet: {expenses}";
            return true;
        });

        // 34. EXE_DAILY_REPORT — Bao cao ngay san sang
        await CheckAndSendAsync(db, "EXE_DAILY_REPORT", async (cfg, dto) =>
        {
            dto.Title = "Bao cao ngay san sang";
            dto.Message = $"Bao cao ngay {today:dd/MM/yyyy} da san sang. Vui long kiem tra tai /Executive.";
            return true;
        });

        // 35. EXE_KPI_ANOMALY — Canh bao KPI bat thuong
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
                        anomalies.Add($"{target.KpiName} (lech {deviation:N0}%)");
                }
            }
            if (anomalies.Count == 0) return false;
            dto.Title = "Canh bao KPI bat thuong";
            dto.Message = $"KPI bat thuong: {string.Join(", ", anomalies)}";
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
