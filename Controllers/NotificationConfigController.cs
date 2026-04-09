using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dashboard.Data;
using Dashboard.Models;
using Dashboard.Models.ViewModels;
using Dashboard.Services.Interfaces;

namespace Dashboard.Controllers;

[Authorize(Policy = "SystemPolicy")]
public class NotificationConfigController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationConfigCache _configCache;
    private readonly IJobSchedulerService _jobScheduler;

    public NotificationConfigController(
        ApplicationDbContext context,
        INotificationConfigCache configCache,
        IJobSchedulerService jobScheduler)
    {
        _context = context;
        _configCache = configCache;
        _jobScheduler = jobScheduler;
    }

    public async Task<IActionResult> Index()
    {
        var configs = await _context.NotificationConfigs.AsNoTracking().ToListAsync();

        var total = configs.Count;
        var enabled = configs.Count(c => c.IsEnabled);
        var disabled = total - enabled;

        var byCategory = configs
            .GroupBy(c => c.Category)
            .Select(g => new
            {
                Category = g.Key,
                Total = g.Count(),
                Enabled = g.Count(c => c.IsEnabled),
                Disabled = g.Count(c => !c.IsEnabled)
            })
            .ToList();

        ViewBag.Total = total;
        ViewBag.Enabled = enabled;
        ViewBag.Disabled = disabled;
        ViewBag.ByCategory = byCategory;

        return View();
    }

    public async Task<IActionResult> List([FromQuery] string? category, [FromQuery] string? severity)
    {
        var query = _context.NotificationConfigs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(category))
            query = query.Where(c => c.Category == category);

        if (!string.IsNullOrEmpty(severity))
            query = query.Where(c => c.Severity == severity);

        var configs = await query
            .OrderBy(c => c.Category)
            .ThenBy(c => c.NotificationName)
            .ToListAsync();

        var vms = configs.Select(c => new NotificationConfigListVM
        {
            ConfigID = c.ConfigID,
            NotificationCode = c.NotificationCode,
            NotificationName = c.NotificationName,
            Category = c.Category,
            Severity = c.Severity,
            IsEnabled = c.IsEnabled,
            CheckIntervalMinutes = c.CheckIntervalMinutes,
            ThresholdValue = c.ThresholdValue,
            ThresholdValue2 = c.ThresholdValue2,
            DelayHours = c.DelayHours,
            IconClass = c.IconClass,
            IconBgClass = c.IconBgClass,
            ActionUrl = c.ActionUrl,
            Description = c.Description,
            AllowedRoles = c.AllowedRoles
        }).ToList();

        return View(vms);
    }

    public async Task<IActionResult> GetAll()
    {
        var configs = await _context.NotificationConfigs
            .AsNoTracking()
            .Select(c => new NotificationConfigListVM
            {
                ConfigID = c.ConfigID,
                NotificationCode = c.NotificationCode,
                NotificationName = c.NotificationName,
                Category = c.Category,
                Severity = c.Severity,
                IsEnabled = c.IsEnabled,
                CheckIntervalMinutes = c.CheckIntervalMinutes,
                ThresholdValue = c.ThresholdValue,
                ThresholdValue2 = c.ThresholdValue2,
                DelayHours = c.DelayHours,
                IconClass = c.IconClass,
                IconBgClass = c.IconBgClass,
                ActionUrl = c.ActionUrl,
                Description = c.Description,
                AllowedRoles = c.AllowedRoles,
                CronExpression = c.CronExpression
            })
            .ToListAsync();

        return Json(configs);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var config = await _context.NotificationConfigs.FindAsync(id);
        if (config == null) return NotFound();

        var vm = new NotificationConfigEditVM
        {
            ConfigID = config.ConfigID,
            NotificationCode = config.NotificationCode,
            NotificationName = config.NotificationName,
            IsEnabled = config.IsEnabled,
            CheckIntervalMinutes = config.CheckIntervalMinutes,
            ThresholdValue = config.ThresholdValue,
            ThresholdValue2 = config.ThresholdValue2,
            DelayHours = config.DelayHours,
            AllowedRoles = config.AllowedRoles,
            IconClass = config.IconClass,
            IconBgClass = config.IconBgClass,
            ActionUrl = config.ActionUrl,
            Description = config.Description,
            CronExpression = config.CronExpression
        };

        ViewBag.Roles = new[] { "Executive", "Finance", "Sales", "Marketing", "Inventory", "HumanResources", "CustomerService" };
        ViewBag.Severities = new[] { "Info", "Warning", "Critical" };
        ViewBag.Intervals = new[] { 1, 5, 10, 15, 30, 60, 1440 };
        ViewBag.Category = config.Category;

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(NotificationConfigEditVM model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = new[] { "Executive", "Finance", "Sales", "Marketing", "Inventory", "HumanResources", "CustomerService" };
            ViewBag.Severities = new[] { "Info", "Warning", "Critical" };
            ViewBag.Intervals = new[] { 1, 5, 10, 15, 30, 60, 1440 };
            ViewBag.Category = _context.NotificationConfigs.AsNoTracking().FirstOrDefault(c => c.ConfigID == model.ConfigID)?.Category;
            return View(model);
        }

        var config = await _context.NotificationConfigs.FindAsync(model.ConfigID);
        if (config == null) return NotFound();

        config.IsEnabled = model.IsEnabled;
        config.CheckIntervalMinutes = model.CheckIntervalMinutes;
        config.ThresholdValue = model.ThresholdValue;
        config.ThresholdValue2 = model.ThresholdValue2;
        config.DelayHours = model.DelayHours;
        config.AllowedRoles = model.AllowedRoles;
        config.IconClass = model.IconClass;
        config.IconBgClass = model.IconBgClass;
        config.ActionUrl = model.ActionUrl;
        config.Description = model.Description;
        config.CronExpression = model.CronExpression;
        config.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Invalidate cache so jobs pick up new config
        await _configCache.InvalidateAsync();
        await _jobScheduler.RegisterAllJobsAsync();

        TempData["SuccessMessage"] = $"Cau hinh '{config.NotificationName}' da duoc cap nhat.";
        return RedirectToAction(nameof(List));
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var config = await _context.NotificationConfigs.FindAsync(id);
        if (config == null) return NotFound();

        config.IsEnabled = !config.IsEnabled;
        config.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _configCache.InvalidateAsync();
        await _jobScheduler.RegisterAllJobsAsync();

        return Json(new
        {
            success = true,
            isEnabled = config.IsEnabled,
            message = config.IsEnabled
                ? $"'{config.NotificationName}' has been enabled"
                : $"'{config.NotificationName}' has been disabled"
        });
    }

    [HttpPost]
    public async Task<IActionResult> BulkToggle([FromBody] BulkToggleRequest request)
    {
        if (request?.Ids == null || request.Ids.Length == 0)
            return Json(new { success = false, message = "No items selected." });

        var configs = await _context.NotificationConfigs
            .Where(c => request.Ids.Contains(c.ConfigID))
            .ToListAsync();

        foreach (var config in configs)
        {
            config.IsEnabled = request.Enable;
            config.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        await _configCache.InvalidateAsync();
        await _jobScheduler.RegisterAllJobsAsync();

        var action = request.Enable ? "enabled" : "disabled";
        return Json(new { success = true, message = $"{configs.Count} items have been {action}." });
    }

    [HttpPost]
    public async Task<IActionResult> Reset()
    {
        var existing = await _context.NotificationConfigs.AnyAsync();
        if (existing)
        {
            var all = await _context.NotificationConfigs.ToListAsync();
            _context.NotificationConfigs.RemoveRange(all);
        }

        var defaultConfigs = GetDefaultConfigs();
        _context.NotificationConfigs.AddRange(defaultConfigs);
        await _context.SaveChangesAsync();
        await _configCache.InvalidateAsync();
        await _jobScheduler.RegisterAllJobsAsync();

        TempData["SuccessMessage"] = $"Reset {defaultConfigs.Count} configurations to defaults.";
        return RedirectToAction(nameof(List));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateJobStatus([FromBody] JobStatusUpdateRequest request)
    {
        if (request == null || request.ConfigId <= 0)
            return Json(new { success = false, message = "Invalid config ID." });

        var config = await _context.NotificationConfigs.FindAsync(request.ConfigId);
        if (config == null)
            return Json(new { success = false, message = "Configuration not found." });

        config.IsEnabled = request.IsEnabled;
        config.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _configCache.InvalidateAsync();
        await _jobScheduler.RegisterAllJobsAsync();

        return Json(new
        {
            success = true,
            isEnabled = config.IsEnabled,
            message = $"'{config.NotificationName}' has been {(config.IsEnabled ? "enabled" : "disabled")}"
        });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateCron([FromBody] CronUpdateRequest request)
    {
        if (request == null || request.ConfigId <= 0)
            return Json(new { success = false, message = "Invalid config ID." });

        if (string.IsNullOrWhiteSpace(request.CronExpression))
            return Json(new { success = false, message = "Cron expression cannot be empty." });

        var config = await _context.NotificationConfigs.FindAsync(request.ConfigId);
        if (config == null)
            return Json(new { success = false, message = "Configuration not found." });

        config.CronExpression = request.CronExpression;
        config.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _configCache.InvalidateAsync();
        await _jobScheduler.RegisterAllJobsAsync();

        return Json(new
        {
            success = true,
            cronExpression = config.CronExpression,
            message = $"Cron schedule for '{config.NotificationName}' updated to '{config.CronExpression}'"
        });
    }

    [HttpPost]
    public async Task<IActionResult> TriggerJobNow([FromBody] TriggerJobRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.JobId))
            return Json(new { success = false, message = "Invalid job ID." });

        try
        {
            _jobScheduler.TriggerJob(request.JobId);
            return Json(new { success = true, message = $"Job '{request.JobId}' triggered successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Failed to trigger job: {ex.Message}" });
        }
    }

    private static List<NotificationConfig> GetDefaultConfigs() => new()
    {
        new() { NotificationCode = "FIN_OVERDUE_30D",      NotificationName = "Overdue receivables 30 days",      Category = "Finance",           Severity = "Critical", IsEnabled = true,  CheckIntervalMinutes = 5,   DelayHours = 720,     IconClass = "bx-error-circle",    IconBgClass = "bg-label-danger",  ActionUrl = "/Finance",        AllowedRoles = "Finance,Executive", CronExpression = "*/5 * * * *" },
        new() { NotificationCode = "FIN_EXPENSE_PENDING",  NotificationName = "Expenses pending approval",       Category = "Finance",           Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 5,   IconClass = "bx-receipt",         IconBgClass = "bg-label-warning", ActionUrl = "/Finance",        AllowedRoles = "Finance,Executive", CronExpression = "*/5 * * * *" },
        new() { NotificationCode = "FIN_OVER_BUDGET",     NotificationName = "Expenses over budget",              Category = "Finance",           Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 5,   ThresholdValue = 0,      IconClass = "bx-wallet",          IconBgClass = "bg-label-warning", ActionUrl = "/Finance",        AllowedRoles = "Finance,Executive", CronExpression = "*/5 * * * *" },
        new() { NotificationCode = "FIN_NEW_PAYMENT",      NotificationName = "New payment from customer",       Category = "Finance",           Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 5,   DelayHours = 24,        IconClass = "bx-money",           IconBgClass = "bg-label-success",  ActionUrl = "/Finance",        AllowedRoles = "Finance,Sales", CronExpression = "*/5 * * * *" },
        new() { NotificationCode = "FIN_CASHFLOW_LOW",     NotificationName = "Abnormal cash flow",              Category = "Finance",           Severity = "Critical", IsEnabled = true,  CheckIntervalMinutes = 5,   IconClass = "bx-trending-down",   IconBgClass = "bg-label-danger",  ActionUrl = "/Finance",        AllowedRoles = "Finance,Executive", CronExpression = "*/5 * * * *" },
        new() { NotificationCode = "FIN_LARGE_INVOICE",   NotificationName = "Large unpaid invoices",           Category = "Finance",           Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 5,   ThresholdValue = 50000000m, IconClass = "bx-file",            IconBgClass = "bg-label-warning", ActionUrl = "/Finance",        AllowedRoles = "Finance,Executive", CronExpression = "*/5 * * * *" },
        new() { NotificationCode = "INV_LOW_STOCK",        NotificationName = "Low inventory",                  Category = "Inventory",         Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 5,   IconClass = "bx-box",             IconBgClass = "bg-label-warning", ActionUrl = "/Inventory",      AllowedRoles = "Inventory,Executive", CronExpression = "*/5 * * * *" },
        new() { NotificationCode = "INV_OUT_OF_STOCK",     NotificationName = "Out of stock",                   Category = "Inventory",         Severity = "Critical", IsEnabled = true,  CheckIntervalMinutes = 5,   IconClass = "bx-x-circle",        IconBgClass = "bg-label-danger",  ActionUrl = "/Inventory",      AllowedRoles = "Inventory,Executive", CronExpression = "*/5 * * * *" },
        new() { NotificationCode = "INV_PO_PENDING",       NotificationName = "Purchase order pending approval",Category = "Inventory",         Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 5,   IconClass = "bx-cart",            IconBgClass = "bg-label-warning", ActionUrl = "/Inventory",      AllowedRoles = "Inventory,Executive", CronExpression = "*/5 * * * *" },
        new() { NotificationCode = "INV_NEW_RECEIPT",      NotificationName = "New stock receipt",              Category = "Inventory",         Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 5,   DelayHours = 24,        IconClass = "bx-package",         IconBgClass = "bg-label-primary", ActionUrl = "/Inventory",      AllowedRoles = "Inventory", CronExpression = "*/5 * * * *" },
        new() { NotificationCode = "HR_LEAVE_PENDING",     NotificationName = "Leave request pending approval",Category = "HumanResources",    Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 30,  IconClass = "bx-calendar",        IconBgClass = "bg-label-warning", ActionUrl = "/HumanResources",  AllowedRoles = "HumanResources,Executive", CronExpression = "*/30 * * * *" },
        new() { NotificationCode = "HR_PROBATION_ENDING",  NotificationName = "Employee ending probation",       Category = "HumanResources",    Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 30,  DelayHours = 168,        IconClass = "bx-user-check",       IconBgClass = "bg-label-warning", ActionUrl = "/HumanResources",  AllowedRoles = "HumanResources,Executive", CronExpression = "*/30 * * * *" },
        new() { NotificationCode = "HR_HIGH_TURNOVER",     NotificationName = "High turnover rate",             Category = "HumanResources",    Severity = "Critical", IsEnabled = true,  CheckIntervalMinutes = 30,  ThresholdValue = 5,         IconClass = "bx-user-minus",     IconBgClass = "bg-label-danger",  ActionUrl = "/HumanResources",  AllowedRoles = "HumanResources,Executive", CronExpression = "*/30 * * * *" },
        new() { NotificationCode = "HR_NEW_APPLICANT",     NotificationName = "New applicant",                 Category = "HumanResources",    Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 30,  DelayHours = 24,        IconClass = "bx-user-plus",       IconBgClass = "bg-label-success",  ActionUrl = "/HumanResources",  AllowedRoles = "HumanResources", CronExpression = "*/30 * * * *" },
        new() { NotificationCode = "HR_BIRTHDAY",          NotificationName = "Employee birthday",              Category = "HumanResources",    Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 30,  IconClass = "bx-gift",             IconBgClass = "bg-label-success",  ActionUrl = "/HumanResources",  AllowedRoles = "*", CronExpression = "*/30 * * * *" },
        new() { NotificationCode = "HR_NEW_EMPLOYEE",      NotificationName = "New employee onboarded",         Category = "HumanResources",    Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 30,  DelayHours = 168,        IconClass = "bx-user",             IconBgClass = "bg-label-primary", ActionUrl = "/HumanResources",  AllowedRoles = "HumanResources", CronExpression = "*/30 * * * *" },
        new() { NotificationCode = "SAL_NEW_ORDER",        NotificationName = "New order",                      Category = "Sales",             Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 10,  DelayHours = 1,         IconClass = "bx-shopping-bag",     IconBgClass = "bg-label-primary", ActionUrl = "/Sales",           AllowedRoles = "Sales,Executive", CronExpression = "*/10 * * * *" },
        new() { NotificationCode = "SAL_LARGE_ORDER",      NotificationName = "Large order",                   Category = "Sales",             Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 10,  ThresholdValue = 100000000m, IconClass = "bx-error",          IconBgClass = "bg-label-warning", ActionUrl = "/Sales",           AllowedRoles = "Sales,Executive", CronExpression = "*/10 * * * *" },
        new() { NotificationCode = "SAL_DELIVERY_DELAYED",NotificationName = "Order delivery delayed",         Category = "Sales",             Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 10,  DelayHours = 72,         IconClass = "bx-truck",            IconBgClass = "bg-label-warning", ActionUrl = "/Sales",           AllowedRoles = "Sales,Inventory", CronExpression = "*/10 * * * *" },
        new() { NotificationCode = "SAL_OPP_STAGE_CHANGED",NotificationName = "Opportunity stage changed",     Category = "Sales",             Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 10,  DelayHours = 1,         IconClass = "bx-git-branch",       IconBgClass = "bg-label-primary", ActionUrl = "/Sales",           AllowedRoles = "Sales,Executive", CronExpression = "*/10 * * * *" },
        new() { NotificationCode = "SAL_TARGET_ACHIEVED",  NotificationName = "Target achieved",               Category = "Sales",             Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 10,  IconClass = "bx-trophy",           IconBgClass = "bg-label-success",  ActionUrl = "/Sales",           AllowedRoles = "Sales,Executive", CronExpression = "*/10 * * * *" },
        new() { NotificationCode = "SAL_NEW_CUSTOMER",     NotificationName = "New customer",                  Category = "Sales",             Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 10,  DelayHours = 24,        IconClass = "bx-user-circle",      IconBgClass = "bg-label-primary", ActionUrl = "/Sales",           AllowedRoles = "Sales", CronExpression = "*/10 * * * *" },
        new() { NotificationCode = "CS_NEW_TICKET",        NotificationName = "New ticket",                    Category = "CustomerService",   Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 5,   DelayHours = 1,         IconClass = "bx-support",          IconBgClass = "bg-label-primary", ActionUrl = "/CustomerService",AllowedRoles = "CustomerService", CronExpression = "*/5 * * * *" },
        new() { NotificationCode = "CS_HIGH_PRIORITY",     NotificationName = "High priority ticket",          Category = "CustomerService",   Severity = "Critical", IsEnabled = true,  CheckIntervalMinutes = 5,   IconClass = "bx-error-circle",    IconBgClass = "bg-label-danger",  ActionUrl = "/CustomerService",AllowedRoles = "CustomerService,Executive", CronExpression = "*/5 * * * *" },
        new() { NotificationCode = "CS_TICKET_SLA_BREACH",NotificationName = "Ticket SLA breach",             Category = "CustomerService",   Severity = "Critical", IsEnabled = true,  CheckIntervalMinutes = 5,   DelayHours = 24,        IconClass = "bx-time-five",        IconBgClass = "bg-label-danger",  ActionUrl = "/CustomerService",AllowedRoles = "CustomerService,Executive", CronExpression = "*/5 * * * *" },
        new() { NotificationCode = "CS_TICKET_NO_RESPONSE",NotificationName = "Ticket awaiting response",     Category = "CustomerService",   Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 5,   DelayHours = 4,         IconClass = "bx-message-rounded", IconBgClass = "bg-label-warning", ActionUrl = "/CustomerService",AllowedRoles = "CustomerService", CronExpression = "*/5 * * * *" },
        new() { NotificationCode = "CS_LOW_SATISFACTION", NotificationName = "Customer dissatisfaction",       Category = "CustomerService",   Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 5,   IconClass = "bx-sad",             IconBgClass = "bg-label-warning", ActionUrl = "/CustomerService",AllowedRoles = "CustomerService,Executive", CronExpression = "*/5 * * * *" },
        new() { NotificationCode = "MKT_BUDGET_80",       NotificationName = "Campaign budget almost exhausted", Category = "Marketing",   Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 30,  ThresholdValue = 80m,     IconClass = "bx-trending-up",      IconBgClass = "bg-label-warning", ActionUrl = "/Marketing",      AllowedRoles = "Marketing,Executive", CronExpression = "*/30 * * * *" },
        new() { NotificationCode = "MKT_BUDGET_EXCEEDED",  NotificationName = "Campaign exceeded budget",      Category = "Marketing",         Severity = "Critical", IsEnabled = true,  CheckIntervalMinutes = 30,  IconClass = "bx-warning",          IconBgClass = "bg-label-danger",  ActionUrl = "/Marketing",      AllowedRoles = "Marketing,Executive", CronExpression = "*/30 * * * *" },
        new() { NotificationCode = "MKT_NEW_LEAD",         NotificationName = "New lead",                     Category = "Marketing",         Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 30,  DelayHours = 24,        IconClass = "bx-user-plus",       IconBgClass = "bg-label-primary", ActionUrl = "/Marketing",      AllowedRoles = "Marketing,Sales", CronExpression = "*/30 * * * *" },
        new() { NotificationCode = "MKT_LEAD_CONVERTED",  NotificationName = "Lead converted",                Category = "Marketing",         Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 30,  DelayHours = 24,        IconClass = "bx-check-circle",    IconBgClass = "bg-label-success",  ActionUrl = "/Marketing",      AllowedRoles = "Marketing,Sales", CronExpression = "*/30 * * * *" },
        new() { NotificationCode = "MKT_LOW_ROAS",         NotificationName = "Low ROAS",                     Category = "Marketing",         Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 30,  ThresholdValue = 1m,       IconClass = "bx-line-chart",       IconBgClass = "bg-label-warning", ActionUrl = "/Marketing",      AllowedRoles = "Marketing,Executive", CronExpression = "*/30 * * * *" },
        new() { NotificationCode = "EXE_DAILY_REPORT",     NotificationName = "Daily report ready",           Category = "Executive",         Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 1440, IconClass = "bx-file",            IconBgClass = "bg-label-info",    ActionUrl = "/Executive",      AllowedRoles = "Executive", CronExpression = "0 8 * * *" },
        new() { NotificationCode = "EXE_KPI_ANOMALY",      NotificationName = "KPI anomaly warning",           Category = "Executive",         Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 1440, ThresholdValue = 20m,     IconClass = "bx-alert",            IconBgClass = "bg-label-warning", ActionUrl = "/Executive",      AllowedRoles = "Executive", CronExpression = "0 8 * * *" },
        new() { NotificationCode = "EXE_DAILY_DIGEST",    NotificationName = "Daily activity summary",         Category = "Executive",         Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 1440, IconClass = "bx-news",            IconBgClass = "bg-label-info",    ActionUrl = "/Executive",      AllowedRoles = "Executive", CronExpression = "0 8 * * *" },
    };
}

public class BulkToggleRequest
{
    public int[] Ids { get; set; } = Array.Empty<int>();
    public bool Enable { get; set; }
}

public class JobStatusUpdateRequest
{
    public int ConfigId { get; set; }
    public bool IsEnabled { get; set; }
}

public class CronUpdateRequest
{
    public int ConfigId { get; set; }
    public string CronExpression { get; set; } = string.Empty;
}

public class TriggerJobRequest
{
    public string JobId { get; set; } = string.Empty;
}
