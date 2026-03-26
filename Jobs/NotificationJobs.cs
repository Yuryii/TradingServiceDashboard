using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Dashboard.Data;
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
        var config = await _configCache.GetConfigAsync("FIN_OVERDUE_30D");
        if (config == null || !config.IsEnabled) return;

        var delayHours = config.DelayHours ?? 720;
        var cutoff = DateTime.Now.AddHours(-delayHours);

        var overdue = await db.SalesOrders
            .Include(o => o.Customer)
            .Where(o => o.PaymentStatus != "Paid" && o.OrderDate < cutoff)
            .AsNoTracking()
            .ToListAsync();

        if (overdue.Count == 0) return;

        var dto = new NotificationSignalDto
        {
            Title = $"Cong no qua han",
            Message = $"{overdue.Count} don hang chua thanh toan qua {delayHours / 24} ngay",
            Category = "Finance",
            Severity = "Critical",
            IconClass = "bx-error-circle",
            IconBgClass = "bg-label-danger",
            ActionUrl = "/Finance"
        };

        var users = await db.Users
            .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id
                && db.Roles.Any(r => r.Name == "Finance" || r.Name == "Executive")))
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var userId in users)
        {
            await _notifService.SendToUserAsync(userId, dto);
        }
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

        // INV_LOW_STOCK
        var lowStock = await _configCache.GetConfigAsync("INV_LOW_STOCK");
        if (lowStock?.IsEnabled == true)
        {
            var lowItems = await db.Inventories
                .Include(i => i.Product)
                .Where(i => i.QuantityAvailable <= i.ReorderPoint && i.QuantityAvailable > 0)
                .AsNoTracking()
                .CountAsync();

            if (lowItems > 0)
            {
                var dto = new NotificationSignalDto
                {
                    Title = "Ton kho thap",
                    Message = $"{lowItems} san pham con thap hon diem dat hang",
                    Category = "Inventory",
                    Severity = "Warning",
                    IconClass = "bx-warning",
                    IconBgClass = "bg-label-warning",
                    ActionUrl = "/Inventory"
                };

                var users = await db.Users
                    .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id
                        && db.Roles.Any(r => r.Name == "Inventory" || r.Name == "Executive")))
                    .Select(u => u.Id)
                    .ToListAsync();

                foreach (var userId in users)
                    await _notifService.SendToUserAsync(userId, dto);
            }
        }

        // INV_OUT_OF_STOCK
        var outStock = await _configCache.GetConfigAsync("INV_OUT_OF_STOCK");
        if (outStock?.IsEnabled == true)
        {
            var outItems = await db.Inventories
                .Include(i => i.Product)
                .Where(i => i.QuantityAvailable == 0)
                .AsNoTracking()
                .CountAsync();

            if (outItems > 0)
            {
                var dto = new NotificationSignalDto
                {
                    Title = "Het hang",
                    Message = $"{outItems} san pham da het hang",
                    Category = "Inventory",
                    Severity = "Critical",
                    IconClass = "bx-x-circle",
                    IconBgClass = "bg-label-danger",
                    ActionUrl = "/Inventory"
                };

                var users = await db.Users
                    .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id
                        && db.Roles.Any(r => r.Name == "Inventory" || r.Name == "Executive")))
                    .Select(u => u.Id)
                    .ToListAsync();

                foreach (var userId in users)
                    await _notifService.SendToUserAsync(userId, dto);
            }
        }
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

        // HR_LEAVE_PENDING
        var leaveConfig = await _configCache.GetConfigAsync("HR_LEAVE_PENDING");
        if (leaveConfig?.IsEnabled == true)
        {
            var pending = await db.LeaveRequests
                .Include(l => l.Employee)
                .Where(l => l.Status == "Pending")
                .AsNoTracking()
                .CountAsync();

            if (pending > 0)
            {
                var dto = new NotificationSignalDto
                {
                    Title = "Don nghi phep cho phe duyet",
                    Message = $"{pending} don nghi phep dang cho xu ly",
                    Category = "HumanResources",
                    Severity = "Warning",
                    IconClass = "bx-calendar",
                    IconBgClass = "bg-label-warning",
                    ActionUrl = "/HumanResources"
                };

                var users = await db.Users
                    .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id
                        && db.Roles.Any(r => r.Name == "HumanResources" || r.Name == "Executive")))
                    .Select(u => u.Id)
                    .ToListAsync();

                foreach (var userId in users)
                    await _notifService.SendToUserAsync(userId, dto);
            }
        }

        // HR_BIRTHDAY
        var birthdayConfig = await _configCache.GetConfigAsync("HR_BIRTHDAY");
        if (birthdayConfig?.IsEnabled == true)
        {
            var today = DateTime.Today;
            var birthdays = await db.Employees
                .Where(e => e.DateOfBirth.HasValue
                    && e.DateOfBirth.Value.Day == today.Day
                    && e.DateOfBirth.Value.Month == today.Month
                    && e.IsActive)
                .AsNoTracking()
                .ToListAsync();

            foreach (var emp in birthdays)
            {
                var dto = new NotificationSignalDto
                {
                    Title = "Sinh nhat nhan vien",
                    Message = $"Chuc mung sinh nhat {emp.FullName}!",
                    Category = "HumanResources",
                    Severity = "Info",
                    IconClass = "bx-gift",
                    IconBgClass = "bg-label-success",
                    ActionUrl = "/HumanResources"
                };

                var users = await db.Users
                    .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id))
                    .Select(u => u.Id)
                    .ToListAsync();

                foreach (var userId in users)
                    await _notifService.SendToUserAsync(userId, dto);
            }
        }
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

        // SAL_NEW_ORDER
        var newOrderConfig = await _configCache.GetConfigAsync("SAL_NEW_ORDER");
        if (newOrderConfig?.IsEnabled == true)
        {
            var delayHours = newOrderConfig.DelayHours ?? 1;
            var cutoff = DateTime.Now.AddHours(-delayHours);

            var newOrders = await db.SalesOrders
                .Include(o => o.Customer)
                .Where(o => o.CreatedAt >= cutoff)
                .AsNoTracking()
                .CountAsync();

            if (newOrders > 0)
            {
                var dto = new NotificationSignalDto
                {
                    Title = "Don hang moi",
                    Message = $"{newOrders} don hang duoc tao trong {delayHours} gio qua",
                    Category = "Sales",
                    Severity = "Info",
                    IconClass = "bx-shopping-bag",
                    IconBgClass = "bg-label-primary",
                    ActionUrl = "/Sales"
                };

                var users = await db.Users
                    .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id
                        && db.Roles.Any(r => r.Name == "Sales" || r.Name == "Executive")))
                    .Select(u => u.Id)
                    .ToListAsync();

                foreach (var userId in users)
                    await _notifService.SendToUserAsync(userId, dto);
            }
        }

        // SAL_LARGE_ORDER
        var largeOrderConfig = await _configCache.GetConfigAsync("SAL_LARGE_ORDER");
        if (largeOrderConfig?.IsEnabled == true)
        {
            var threshold = largeOrderConfig.ThresholdValue ?? 100_000_000m;

            var largeOrders = await db.SalesOrders
                .Include(o => o.Customer)
                .Where(o => o.TotalAmount > threshold && o.CreatedAt >= DateTime.Now.AddHours(-24))
                .AsNoTracking()
                .ToListAsync();

            foreach (var order in largeOrders)
            {
                var dto = new NotificationSignalDto
                {
                    Title = "Don hang lon",
                    Message = $"Don #{order.OrderNumber} voi so tien {order.TotalAmount:N0} VND",
                    Category = "Sales",
                    Severity = "Warning",
                    IconClass = "bx-error",
                    IconBgClass = "bg-label-warning",
                    ActionUrl = "/Sales"
                };

                var users = await db.Users
                    .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id
                        && db.Roles.Any(r => r.Name == "Sales" || r.Name == "Executive")))
                    .Select(u => u.Id)
                    .ToListAsync();

                foreach (var userId in users)
                    await _notifService.SendToUserAsync(userId, dto);
            }
        }
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

        // CS_HIGH_PRIORITY
        var config = await _configCache.GetConfigAsync("CS_HIGH_PRIORITY");
        if (config?.IsEnabled == true)
        {
            var highPriority = await db.SupportTickets
                .Include(t => t.Customer)
                .Where(t => (t.Priority == "Critical" || t.Priority == "High") && t.Status == "Open")
                .AsNoTracking()
                .CountAsync();

            if (highPriority > 0)
            {
                var dto = new NotificationSignalDto
                {
                    Title = "Ticket uu tien cao",
                    Message = $"{highPriority} ticket uu tien cao chua duoc xu ly",
                    Category = "CustomerService",
                    Severity = "Critical",
                    IconClass = "bx-error-circle",
                    IconBgClass = "bg-label-danger",
                    ActionUrl = "/CustomerService"
                };

                var users = await db.Users
                    .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id
                        && db.Roles.Any(r => r.Name == "CustomerService" || r.Name == "Executive")))
                    .Select(u => u.Id)
                    .ToListAsync();

                foreach (var userId in users)
                    await _notifService.SendToUserAsync(userId, dto);
            }
        }

        // CS_TICKET_SLA_BREACH
        var slaConfig = await _configCache.GetConfigAsync("CS_TICKET_SLA_BREACH");
        if (slaConfig?.IsEnabled == true)
        {
            var delayHours = slaConfig.DelayHours ?? 24;
            var cutoff = DateTime.Now.AddHours(-delayHours);

            var overdue = await db.SupportTickets
                .Where(t => t.CreatedAt < cutoff && t.Status != "Resolved")
                .AsNoTracking()
                .CountAsync();

            if (overdue > 0)
            {
                var dto = new NotificationSignalDto
                {
                    Title = "Ticket qua han SLA",
                    Message = $"{overdue} ticket vuot qua {delayHours} gio ma chua giai quyet",
                    Category = "CustomerService",
                    Severity = "Critical",
                    IconClass = "bx-time-five",
                    IconBgClass = "bg-label-danger",
                    ActionUrl = "/CustomerService"
                };

                var users = await db.Users
                    .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id
                        && db.Roles.Any(r => r.Name == "CustomerService" || r.Name == "Executive")))
                    .Select(u => u.Id)
                    .ToListAsync();

                foreach (var userId in users)
                    await _notifService.SendToUserAsync(userId, dto);
            }
        }
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

        // MKT_BUDGET_80
        var config = await _configCache.GetConfigAsync("MKT_BUDGET_80");
        if (config?.IsEnabled == true)
        {
            var campaigns = await db.MarketingCampaigns
                .Where(c => c.Budget > 0
                    && (decimal)c.ActualSpend >= (decimal)(c.Budget * 0.8m)
                    && (decimal)c.ActualSpend <= c.Budget
                    && c.IsActive)
                .AsNoTracking()
                .ToListAsync();

            if (campaigns.Count > 0)
            {
                var dto = new NotificationSignalDto
                {
                    Title = "Ngan sach campaign sap het",
                    Message = $"{campaigns.Count} campaign da su dung tren 80% ngan sach",
                    Category = "Marketing",
                    Severity = "Warning",
                    IconClass = "bx-trending-up",
                    IconBgClass = "bg-label-warning",
                    ActionUrl = "/Marketing"
                };

                var users = await db.Users
                    .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id
                        && db.Roles.Any(r => r.Name == "Marketing" || r.Name == "Executive")))
                    .Select(u => u.Id)
                    .ToListAsync();

                foreach (var userId in users)
                    await _notifService.SendToUserAsync(userId, dto);
            }
        }
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

        var config = await _configCache.GetConfigAsync("EXE_DAILY_DIGEST");
        if (config?.IsEnabled != true) return;

        var today = DateTime.Today;
        var yesterday = today.AddDays(-1);

        var orders = await db.SalesOrders.CountAsync(s => s.CreatedAt >= yesterday);
        var revenue = await db.SalesOrders
            .Where(s => s.CreatedAt >= yesterday && s.PaymentStatus == "Paid")
            .SumAsync(s => s.TotalAmount);
        var tickets = await db.SupportTickets.CountAsync(t => t.CreatedAt >= yesterday);
        var expenses = await db.Expenses.CountAsync(e => e.ExpenseDate >= yesterday && e.Status == "Pending");

        var dto = new NotificationSignalDto
        {
            Title = "Tom tat hoat dong ngay",
            Message = $"Don hang: {orders} | Doanh thu: {revenue:N0} VND | Ticket: {tickets} | Chi phi cho duyet: {expenses}",
            Category = "Executive",
            Severity = "Info",
            IconClass = "bx-news",
            IconBgClass = "bg-label-info",
            ActionUrl = "/Executive"
        };

        var users = await db.Users
            .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id
                && db.Roles.Any(r => r.Name == "Executive")))
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var userId in users)
            await _notifService.SendToUserAsync(userId, dto);
    }
}
