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

        var defaultConfigs = NotificationConfigDefaults.DefaultConfigs;
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
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            await _jobScheduler.TriggerNotificationCodeAsync(request.JobId, currentUserId);
            return Json(new { success = true, message = $"'{request.JobId}' triggered successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Failed to trigger job: {ex.Message}" });
        }
    }

    private static List<NotificationConfig> GetDefaultConfigs() =>
        NotificationConfigDefaults.DefaultConfigs;
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
