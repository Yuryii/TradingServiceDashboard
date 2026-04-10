using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Dashboard.Data;
using Dashboard.Models;
using Dashboard.Models.ViewModels;
using Dashboard.Services.Interfaces;
using Dashboard.Jobs;

namespace Dashboard.Services;

public class JobSchedulerService : IJobSchedulerService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly INotificationConfigCache _configCache;
    private readonly ILogger<JobSchedulerService> _logger;

    private static readonly Dictionary<string, (Type JobType, string DefaultCron)> JobRegistry = new()
    {
        ["finance-notifications"]     = (typeof(Jobs.FinanceNotificationJob),            "*/5 * * * *"),
        ["inventory-notifications"] = (typeof(Jobs.InventoryNotificationJob),          "*/5 * * * *"),
        ["hr-notifications"]         = (typeof(Jobs.HumanResourcesNotificationJob),       "*/30 * * * *"),
        ["sales-notifications"]     = (typeof(Jobs.SalesNotificationJob),             "*/10 * * * *"),
        ["cs-notifications"]        = (typeof(Jobs.CustomerServiceNotificationJob),     "*/5 * * * *"),
        ["marketing-notifications"] = (typeof(Jobs.MarketingNotificationJob),          "*/30 * * * *"),
        ["executive-summary"]        = (typeof(Jobs.ExecutiveSummaryJob),              "0 8 * * *"),
    };

    private static readonly Dictionary<string, string[]> JobNotificationCodes = new()
    {
        ["finance-notifications"]     = ["FIN_OVERDUE_30D", "FIN_EXPENSE_PENDING", "FIN_OVER_BUDGET", "FIN_NEW_PAYMENT", "FIN_CASHFLOW_LOW", "FIN_LARGE_INVOICE"],
        ["inventory-notifications"]   = ["INV_LOW_STOCK", "INV_OUT_OF_STOCK", "INV_PO_PENDING", "INV_NEW_RECEIPT"],
        ["hr-notifications"]           = ["HR_LEAVE_PENDING", "HR_PROBATION_ENDING", "HR_HIGH_TURNOVER", "HR_NEW_APPLICANT", "HR_BIRTHDAY", "HR_NEW_EMPLOYEE"],
        ["sales-notifications"]        = ["SAL_NEW_ORDER", "SAL_LARGE_ORDER", "SAL_DELIVERY_DELAYED", "SAL_OPP_STAGE_CHANGED", "SAL_TARGET_ACHIEVED", "SAL_NEW_CUSTOMER"],
        ["cs-notifications"]           = ["CS_NEW_TICKET", "CS_HIGH_PRIORITY", "CS_TICKET_SLA_BREACH", "CS_TICKET_NO_RESPONSE", "CS_LOW_SATISFACTION"],
        ["marketing-notifications"]    = ["MKT_BUDGET_80", "MKT_BUDGET_EXCEEDED", "MKT_NEW_LEAD", "MKT_LEAD_CONVERTED", "MKT_LOW_ROAS"],
        ["executive-summary"]           = ["EXE_DAILY_DIGEST", "EXE_DAILY_REPORT", "EXE_KPI_ANOMALY"],
    };

    public JobSchedulerService(
        IServiceScopeFactory scopeFactory,
        INotificationConfigCache configCache,
        ILogger<JobSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _configCache = configCache;
        _logger = logger;
    }

    public async Task RegisterAllJobsAsync()
    {
        var configs = await _configCache.GetAllConfigsAsync();
        var enabledCodes = configs.Where(c => c.IsEnabled).Select(c => c.NotificationCode).ToHashSet();

        foreach (var (jobId, (jobType, defaultCron)) in JobRegistry)
        {
            var jobCodes = JobNotificationCodes.GetValueOrDefault(jobId) ?? Array.Empty<string>();
            var anyEnabled = jobCodes.Any(code => enabledCodes.Contains(code));

            if (anyEnabled)
            {
                NotificationJobsRegistrar.AddOrUpdateJob(jobId, jobType, defaultCron);
            }
            else
            {
                RecurringJob.RemoveIfExists(jobId);
            }
        }
    }

    public async Task<List<JobConfigDto>> GetAllJobConfigsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var configs = await db.NotificationConfigs.AsNoTracking().ToListAsync();

        return configs.Select(c => new JobConfigDto
        {
            ConfigID = c.ConfigID,
            NotificationCode = c.NotificationCode,
            NotificationName = c.NotificationName,
            Category = c.Category,
            Severity = c.Severity,
            IsEnabled = c.IsEnabled,
            CronExpression = c.CronExpression,
            Description = c.Description,
            IconClass = c.IconClass,
            IconBgClass = c.IconBgClass,
            CheckIntervalMinutes = c.CheckIntervalMinutes,
            ThresholdValue = c.ThresholdValue,
            DelayHours = c.DelayHours,
        }).ToList();
    }

    public async Task UpdateJobStatusAsync(int configId, bool isEnabled)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var config = await db.NotificationConfigs.FindAsync(configId);
        if (config == null) return;

        config.IsEnabled = isEnabled;
        config.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await _configCache.InvalidateAsync();
        await RegisterAllJobsAsync();
    }

    public async Task UpdateCronExpressionAsync(int configId, string cronExpression)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var config = await db.NotificationConfigs.FindAsync(configId);
        if (config == null) return;

        config.CronExpression = cronExpression;
        config.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await _configCache.InvalidateAsync();
        await RegisterAllJobsAsync();
    }

    public void TriggerJob(string jobId)
    {
        var entry = JobRegistry.GetValueOrDefault(jobId);
        if (entry.JobType == null) return;

        using var scope = _scopeFactory.CreateScope();
        NotificationJobsRegistrar.EnqueueJob(scope.ServiceProvider, entry.JobType);
    }

    public async Task TriggerNotificationCodeAsync(string notificationCode, string? targetUserId = null)
    {
        var config = await _configCache.GetConfigAsync(notificationCode);
        if (config == null)
        {
            _logger.LogWarning("[TriggerNotificationCodeAsync] Config not found: {Code}", notificationCode);
            return;
        }
        if (!config.IsEnabled)
        {
            _logger.LogWarning("[TriggerNotificationCodeAsync] Config disabled: {Code}", notificationCode);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notifService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var dto = new NotificationSignalDto
        {
            Category = config.Category,
            Severity = config.Severity,
            IconClass = config.IconClass,
            IconBgClass = config.IconBgClass,
            ActionUrl = config.ActionUrl ?? $"/{config.Category}"
        };

        // Check condition based on notification code
        var hasCondition = await CheckNotificationCondition(db, config, dto);
        if (!hasCondition)
        {
            _logger.LogInformation("[TriggerNotificationCodeAsync] No data for code: {Code}, no notification sent", notificationCode);
            return;
        }

        _logger.LogInformation("[TriggerNotificationCodeAsync] Sending notification '{Title}' for {Code} to user {UserId}", dto.Title, notificationCode, targetUserId);

        // Neu co targetUserId -> gui truc tiep den user do (khong qua role check)
        if (!string.IsNullOrEmpty(targetUserId))
        {
            await notifService.SendToUserAsync(targetUserId, dto);
            _logger.LogInformation("[TriggerNotificationCodeAsync] Sent to user: {UserId}", targetUserId);
            return;
        }

        // Gui den tat ca user co role lien quan
        var users = await db.Users
            .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id
                && db.Roles.Any(r => r.Name == "Finance" || r.Name == "Executive"
                    || r.Name == "Sales" || r.Name == "Marketing"
                    || r.Name == "Inventory" || r.Name == "HumanResources"
                    || r.Name == "CustomerService" || r.Name == "Executive")))
            .Select(u => u.Id)
            .ToListAsync();

        _logger.LogInformation("[TriggerNotificationCodeAsync] Sending to {Count} role-based users", users.Count);
        foreach (var userId in users)
            await notifService.SendToUserAsync(userId, dto);
    }

    private async Task<bool> CheckNotificationCondition(ApplicationDbContext db, NotificationConfig config, NotificationSignalDto dto)
    {
        var code = config.NotificationCode;
        var now = DateTime.UtcNow;

        switch (code)
        {
            case "FIN_OVERDUE_30D":
            {
                var delayHours = config.DelayHours ?? 720;
                var cutoff = DateTime.Now.AddHours(-delayHours);
                var count = await db.SalesOrders.Where(o => o.PaymentStatus != "Paid" && o.OrderDate < cutoff).CountAsync();
                if (count == 0) return false;
                dto.Title = "Overdue receivables";
                dto.Message = $"{count} unpaid orders overdue {delayHours / 24} days";
                return true;
            }
            case "FIN_EXPENSE_PENDING":
            {
                var count = await db.Expenses.Where(e => e.Status == "Pending").CountAsync();
                if (count == 0) return false;
                dto.Title = "Expenses pending approval";
                dto.Message = $"{count} expenses awaiting approval";
                return true;
            }
            case "FIN_OVER_BUDGET":
            {
                var threshold = config.ThresholdValue ?? 0;
                var count = await db.Expenses.Where(e => e.Status == "Approved" && e.Amount > threshold).CountAsync();
                if (count == 0) return false;
                dto.Title = "Expenses over budget";
                dto.Message = $"{count} expenses exceeding {threshold:N0} VND threshold";
                return true;
            }
            case "FIN_NEW_PAYMENT":
            {
                var delayHours = config.DelayHours ?? 24;
                var cutoff = DateTime.Now.AddHours(-delayHours);
                var count = await db.CustomerPayments.Where(p => p.PaymentDate >= cutoff).CountAsync();
                if (count == 0) return false;
                dto.Title = "New payment from customer";
                dto.Message = $"{count} new payments in the past {delayHours} hours";
                return true;
            }
            case "FIN_LARGE_INVOICE":
            {
                var threshold = config.ThresholdValue ?? 50_000_000m;
                var count = await db.SalesInvoices.Where(i => i.PaymentStatus != "Paid" && i.TotalAmount > threshold).CountAsync();
                if (count == 0) return false;
                dto.Title = "Large unpaid invoices";
                dto.Message = $"{count} invoices exceeding {threshold:N0} VND unpaid";
                return true;
            }
            case "INV_LOW_STOCK":
            {
                var invs = await db.Inventories.Where(i => i.QuantityAvailable < i.ReorderPoint).CountAsync();
                if (invs == 0) return false;
                dto.Title = "Low inventory";
                dto.Message = $"{invs} products below reorder point";
                return true;
            }
            case "INV_OUT_OF_STOCK":
            {
                var count = await db.Inventories.Where(i => i.QuantityAvailable <= 0).CountAsync();
                if (count == 0) return false;
                dto.Title = "Out of stock";
                dto.Message = $"{count} products out of stock";
                return true;
            }
            case "INV_PO_PENDING":
            {
                var count = await db.PurchaseOrders.Where(p => p.Status == "Submitted").CountAsync();
                if (count == 0) return false;
                dto.Title = "Purchase order pending approval";
                dto.Message = $"{count} purchase orders awaiting approval";
                return true;
            }
            case "HR_LEAVE_PENDING":
            {
                var count = await db.LeaveRequests.Where(l => l.Status == "Pending").CountAsync();
                if (count == 0) return false;
                dto.Title = "Leave request pending approval";
                dto.Message = $"{count} leave requests awaiting approval";
                return true;
            }
            case "HR_NEW_APPLICANT":
            {
                var delayHours = config.DelayHours ?? 24;
                var cutoff = DateTime.Now.AddHours(-delayHours);
                var count = await db.Applicants.Where(a => a.AppliedDate >= cutoff).CountAsync();
                if (count == 0) return false;
                dto.Title = "New applicant";
                dto.Message = $"{count} new applicants in the past {delayHours} hours";
                return true;
            }
            case "SAL_NEW_ORDER":
            {
                var delayHours = config.DelayHours ?? 1;
                var cutoff = DateTime.Now.AddHours(-delayHours);
                var count = await db.SalesOrders.Where(o => o.CreatedAt >= cutoff).CountAsync();
                if (count == 0) return false;
                dto.Title = "New order";
                dto.Message = $"{count} new orders in the past {delayHours} hours";
                return true;
            }
            case "SAL_LARGE_ORDER":
            {
                var threshold = config.ThresholdValue ?? 100_000_000m;
                var count = await db.SalesOrders.Where(o => o.TotalAmount > threshold).CountAsync();
                if (count == 0) return false;
                dto.Title = "Large order";
                dto.Message = $"{count} orders exceeding {threshold:N0} VND";
                return true;
            }
            case "SAL_NEW_CUSTOMER":
            {
                var delayHours = config.DelayHours ?? 24;
                var cutoff = DateTime.Now.AddHours(-delayHours);
                var count = await db.Customers.Where(c => c.CreatedAt >= cutoff).CountAsync();
                if (count == 0) return false;
                dto.Title = "New customer";
                dto.Message = $"{count} new customers in the past {delayHours} hours";
                return true;
            }
            case "CS_NEW_TICKET":
            {
                var delayHours = config.DelayHours ?? 1;
                var cutoff = DateTime.Now.AddHours(-delayHours);
                var count = await db.SupportTickets.Where(t => t.CreatedAt >= cutoff).CountAsync();
                if (count == 0) return false;
                dto.Title = "New ticket";
                dto.Message = $"{count} new support tickets";
                return true;
            }
            case "CS_HIGH_PRIORITY":
            {
                var count = await db.SupportTickets.Where(t => t.Status == "Open" && (t.Priority == "High" || t.Priority == "Critical")).CountAsync();
                if (count == 0) return false;
                dto.Title = "High priority ticket";
                dto.Message = $"{count} high priority open tickets";
                return true;
            }
            case "MKT_NEW_LEAD":
            {
                var delayHours = config.DelayHours ?? 24;
                var cutoff = DateTime.Now.AddHours(-delayHours);
                var count = await db.MarketingLeads.Where(l => l.CreatedDate >= cutoff).CountAsync();
                if (count == 0) return false;
                dto.Title = "New lead";
                dto.Message = $"{count} new leads in the past {delayHours} hours";
                return true;
            }
            case "MKT_BUDGET_80":
            {
                var threshold = config.ThresholdValue ?? 80m;
                var camps = await db.MarketingCampaigns.Where(c => c.IsActive && c.ActualSpend >= c.Budget * threshold / 100).CountAsync();
                if (camps == 0) return false;
                dto.Title = "Campaign budget almost exhausted";
                dto.Message = $"{camps} campaigns reaching {threshold}% budget";
                return true;
            }
            default:
                dto.Title = config.NotificationName;
                dto.Message = $"Notification triggered for {config.NotificationName}";
                return true;
        }
    }

    public void RegisterJob(string jobId, Type jobType, string cron)
    {
        NotificationJobsRegistrar.AddOrUpdateJob(jobId, jobType, cron);
    }

    public void RemoveJob(string jobId)
    {
        RecurringJob.RemoveIfExists(jobId);
    }
}
