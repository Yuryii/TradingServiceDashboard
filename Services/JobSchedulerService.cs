using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        INotificationConfigCache configCache)
    {
        _scopeFactory = scopeFactory;
        _configCache = configCache;
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

    public void RegisterJob(string jobId, Type jobType, string cron)
    {
        NotificationJobsRegistrar.AddOrUpdateJob(jobId, jobType, cron);
    }

    public void RemoveJob(string jobId)
    {
        RecurringJob.RemoveIfExists(jobId);
    }
}
