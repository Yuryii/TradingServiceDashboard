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

    public NotificationConfigController(
        ApplicationDbContext context,
        INotificationConfigCache configCache)
    {
        _context = context;
        _configCache = configCache;
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
                AllowedRoles = c.AllowedRoles
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
            Description = config.Description
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
        config.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Invalidate cache so jobs pick up new config
        await _configCache.InvalidateAsync();

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

        return Json(new
        {
            success = true,
            isEnabled = config.IsEnabled,
            message = config.IsEnabled
                ? $"'{config.NotificationName}' da duoc bat"
                : $"'{config.NotificationName}' da duoc tat"
        });
    }

    [HttpPost]
    public async Task<IActionResult> BulkToggle([FromBody] BulkToggleRequest request)
    {
        if (request?.Ids == null || request.Ids.Length == 0)
            return Json(new { success = false, message = "Khong co muc nao duoc chon." });

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

        var action = request.Enable ? "bat" : "tat";
        return Json(new { success = true, message = $"{configs.Count} muc da duoc {action}." });
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

        TempData["SuccessMessage"] = $"Da reset {defaultConfigs.Count} cau hinh ve mac dinh.";
        return RedirectToAction(nameof(List));
    }

    private static List<NotificationConfig> GetDefaultConfigs() => new()
    {
        new() { NotificationCode = "FIN_OVERDUE_30D",      NotificationName = "Cong no qua han 30 ngay",         Category = "Finance",           Severity = "Critical", IsEnabled = true,  CheckIntervalMinutes = 5,   DelayHours = 720,     IconClass = "bx-error-circle",    IconBgClass = "bg-label-danger",  ActionUrl = "/Finance",        AllowedRoles = "Finance,Executive" },
        new() { NotificationCode = "FIN_EXPENSE_PENDING",  NotificationName = "Chi phi cho phe duyet",          Category = "Finance",           Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 5,   IconClass = "bx-receipt",         IconBgClass = "bg-label-warning", ActionUrl = "/Finance",        AllowedRoles = "Finance,Executive" },
        new() { NotificationCode = "FIN_OVER_BUDGET",     NotificationName = "Chi phi vuot ngan sach",         Category = "Finance",           Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 5,   ThresholdValue = 0,      IconClass = "bx-wallet",          IconBgClass = "bg-label-warning", ActionUrl = "/Finance",        AllowedRoles = "Finance,Executive" },
        new() { NotificationCode = "FIN_NEW_PAYMENT",      NotificationName = "Thanh toan moi tu KH",            Category = "Finance",           Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 5,   DelayHours = 24,        IconClass = "bx-money",           IconBgClass = "bg-label-success",  ActionUrl = "/Finance",        AllowedRoles = "Finance,Sales" },
        new() { NotificationCode = "FIN_CASHFLOW_LOW",     NotificationName = "Dong tien bat thuong",            Category = "Finance",           Severity = "Critical", IsEnabled = true,  CheckIntervalMinutes = 5,   IconClass = "bx-trending-down",   IconBgClass = "bg-label-danger",  ActionUrl = "/Finance",        AllowedRoles = "Finance,Executive" },
        new() { NotificationCode = "FIN_LARGE_INVOICE",   NotificationName = "Hoa don lon chua thanh toan",    Category = "Finance",           Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 5,   ThresholdValue = 50000000m, IconClass = "bx-file",            IconBgClass = "bg-label-warning", ActionUrl = "/Finance",        AllowedRoles = "Finance,Executive" },
        new() { NotificationCode = "INV_LOW_STOCK",         NotificationName = "Ton kho thap",                   Category = "Inventory",         Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 5,   IconClass = "bx-box",             IconBgClass = "bg-label-warning", ActionUrl = "/Inventory",      AllowedRoles = "Inventory,Executive" },
        new() { NotificationCode = "INV_OUT_OF_STOCK",      NotificationName = "Het hang",                        Category = "Inventory",         Severity = "Critical", IsEnabled = true,  CheckIntervalMinutes = 5,   IconClass = "bx-x-circle",        IconBgClass = "bg-label-danger",  ActionUrl = "/Inventory",      AllowedRoles = "Inventory,Executive" },
        new() { NotificationCode = "INV_PO_PENDING",        NotificationName = "Don mua hang cho phe duyet",     Category = "Inventory",         Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 5,   IconClass = "bx-cart",             IconBgClass = "bg-label-warning", ActionUrl = "/Inventory",      AllowedRoles = "Inventory,Executive" },
        new() { NotificationCode = "INV_NEW_RECEIPT",       NotificationName = "Nhap kho moi",                   Category = "Inventory",         Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 5,   DelayHours = 24,        IconClass = "bx-package",         IconBgClass = "bg-label-primary", ActionUrl = "/Inventory",      AllowedRoles = "Inventory" },
        new() { NotificationCode = "HR_LEAVE_PENDING",      NotificationName = "Don nghi phep cho PD",           Category = "HumanResources",    Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 30,  IconClass = "bx-calendar",        IconBgClass = "bg-label-warning", ActionUrl = "/HumanResources",  AllowedRoles = "HumanResources,Executive" },
        new() { NotificationCode = "HR_PROBATION_ENDING",   NotificationName = "Nhan vien sap het thu viec",     Category = "HumanResources",    Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 30,  DelayHours = 168,        IconClass = "bx-user-check",       IconBgClass = "bg-label-warning", ActionUrl = "/HumanResources",  AllowedRoles = "HumanResources,Executive" },
        new() { NotificationCode = "HR_HIGH_TURNOVER",      NotificationName = "Ty le nghi viec cao",           Category = "HumanResources",    Severity = "Critical", IsEnabled = true,  CheckIntervalMinutes = 30,  ThresholdValue = 5,         IconClass = "bx-user-minus",     IconBgClass = "bg-label-danger",  ActionUrl = "/HumanResources",  AllowedRoles = "HumanResources,Executive" },
        new() { NotificationCode = "HR_NEW_APPLICANT",      NotificationName = "Ung vien moi",                   Category = "HumanResources",    Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 30,  DelayHours = 24,        IconClass = "bx-user-plus",       IconBgClass = "bg-label-success",  ActionUrl = "/HumanResources",  AllowedRoles = "HumanResources" },
        new() { NotificationCode = "HR_BIRTHDAY",           NotificationName = "Sinh nhat nhan vien",            Category = "HumanResources",    Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 30,  IconClass = "bx-gift",             IconBgClass = "bg-label-success",  ActionUrl = "/HumanResources",  AllowedRoles = "*" },
        new() { NotificationCode = "HR_NEW_EMPLOYEE",       NotificationName = "Nhan vien moi nhan viec",        Category = "HumanResources",    Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 30,  DelayHours = 168,        IconClass = "bx-user",             IconBgClass = "bg-label-primary", ActionUrl = "/HumanResources",  AllowedRoles = "HumanResources" },
        new() { NotificationCode = "SAL_NEW_ORDER",         NotificationName = "Don hang moi",                   Category = "Sales",             Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 10,  DelayHours = 1,         IconClass = "bx-shopping-bag",     IconBgClass = "bg-label-primary", ActionUrl = "/Sales",           AllowedRoles = "Sales,Executive" },
        new() { NotificationCode = "SAL_LARGE_ORDER",       NotificationName = "Don hang lon",                     Category = "Sales",             Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 10,  ThresholdValue = 100000000m, IconClass = "bx-error",          IconBgClass = "bg-label-warning", ActionUrl = "/Sales",           AllowedRoles = "Sales,Executive" },
        new() { NotificationCode = "SAL_DELIVERY_DELAYED",  NotificationName = "Don cho giao > 3 ngay",          Category = "Sales",             Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 10,  DelayHours = 72,         IconClass = "bx-truck",            IconBgClass = "bg-label-warning", ActionUrl = "/Sales",           AllowedRoles = "Sales,Inventory" },
        new() { NotificationCode = "SAL_OPP_STAGE_CHANGED", NotificationName = "Co hoi chuyen stage",             Category = "Sales",             Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 10,  DelayHours = 1,         IconClass = "bx-git-branch",       IconBgClass = "bg-label-primary", ActionUrl = "/Sales",           AllowedRoles = "Sales,Executive" },
        new() { NotificationCode = "SAL_TARGET_ACHIEVED",   NotificationName = "Dat muc tieu thang/quy",         Category = "Sales",             Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 10,  IconClass = "bx-trophy",           IconBgClass = "bg-label-success",  ActionUrl = "/Sales",           AllowedRoles = "Sales,Executive" },
        new() { NotificationCode = "SAL_NEW_CUSTOMER",      NotificationName = "Khach hang moi",                  Category = "Sales",             Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 10,  DelayHours = 24,        IconClass = "bx-user-circle",      IconBgClass = "bg-label-primary", ActionUrl = "/Sales",           AllowedRoles = "Sales" },
        new() { NotificationCode = "CS_NEW_TICKET",          NotificationName = "Ticket moi",                     Category = "CustomerService",   Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 5,   DelayHours = 1,         IconClass = "bx-support",          IconBgClass = "bg-label-primary", ActionUrl = "/CustomerService",AllowedRoles = "CustomerService" },
        new() { NotificationCode = "CS_HIGH_PRIORITY",       NotificationName = "Ticket uu tien cao",            Category = "CustomerService",   Severity = "Critical", IsEnabled = true,  CheckIntervalMinutes = 5,   IconClass = "bx-error-circle",    IconBgClass = "bg-label-danger",  ActionUrl = "/CustomerService",AllowedRoles = "CustomerService,Executive" },
        new() { NotificationCode = "CS_TICKET_SLA_BREACH",  NotificationName = "Ticket qua han SLA",            Category = "CustomerService",   Severity = "Critical", IsEnabled = true,  CheckIntervalMinutes = 5,   DelayHours = 24,        IconClass = "bx-time-five",        IconBgClass = "bg-label-danger",  ActionUrl = "/CustomerService",AllowedRoles = "CustomerService,Executive" },
        new() { NotificationCode = "CS_TICKET_NO_RESPONSE", NotificationName = "Ticket cho phan hoi",           Category = "CustomerService",   Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 5,   DelayHours = 4,         IconClass = "bx-message-rounded", IconBgClass = "bg-label-warning", ActionUrl = "/CustomerService",AllowedRoles = "CustomerService" },
        new() { NotificationCode = "CS_LOW_SATISFACTION",   NotificationName = "KH khong hai long",             Category = "CustomerService",   Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 5,   IconClass = "bx-sad",             IconBgClass = "bg-label-warning", ActionUrl = "/CustomerService",AllowedRoles = "CustomerService,Executive" },
        new() { NotificationCode = "MKT_BUDGET_80",         NotificationName = "Ngan sach campaign sap het",     Category = "Marketing",         Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 30,  ThresholdValue = 80m,     IconClass = "bx-trending-up",      IconBgClass = "bg-label-warning", ActionUrl = "/Marketing",      AllowedRoles = "Marketing,Executive" },
        new() { NotificationCode = "MKT_BUDGET_EXCEEDED",   NotificationName = "Campaign vuot ngan sach",        Category = "Marketing",         Severity = "Critical", IsEnabled = true,  CheckIntervalMinutes = 30,  IconClass = "bx-warning",          IconBgClass = "bg-label-danger",  ActionUrl = "/Marketing",      AllowedRoles = "Marketing,Executive" },
        new() { NotificationCode = "MKT_NEW_LEAD",          NotificationName = "Lead moi",                       Category = "Marketing",         Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 30,  DelayHours = 24,        IconClass = "bx-user-plus",       IconBgClass = "bg-label-primary", ActionUrl = "/Marketing",      AllowedRoles = "Marketing,Sales" },
        new() { NotificationCode = "MKT_LEAD_CONVERTED",   NotificationName = "Lead duoc chuyen doi",           Category = "Marketing",         Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 30,  DelayHours = 24,        IconClass = "bx-check-circle",    IconBgClass = "bg-label-success",  ActionUrl = "/Marketing",      AllowedRoles = "Marketing,Sales" },
        new() { NotificationCode = "MKT_LOW_ROAS",           NotificationName = "ROAS thap",                      Category = "Marketing",         Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 30,  ThresholdValue = 1m,       IconClass = "bx-line-chart",       IconBgClass = "bg-label-warning", ActionUrl = "/Marketing",      AllowedRoles = "Marketing,Executive" },
        new() { NotificationCode = "EXE_DAILY_REPORT",       NotificationName = "Bao cao ngay san sang",         Category = "Executive",         Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 1440, IconClass = "bx-file",            IconBgClass = "bg-label-info",    ActionUrl = "/Executive",      AllowedRoles = "Executive" },
        new() { NotificationCode = "EXE_KPI_ANOMALY",        NotificationName = "Canh bao KPI bat thuong",      Category = "Executive",         Severity = "Warning",  IsEnabled = true,  CheckIntervalMinutes = 1440, ThresholdValue = 20m,     IconClass = "bx-alert",            IconBgClass = "bg-label-warning", ActionUrl = "/Executive",      AllowedRoles = "Executive" },
        new() { NotificationCode = "EXE_DAILY_DIGEST",      NotificationName = "Tom tat hoat dong ngay",        Category = "Executive",         Severity = "Info",     IsEnabled = true,  CheckIntervalMinutes = 1440, IconClass = "bx-news",            IconBgClass = "bg-label-info",    ActionUrl = "/Executive",      AllowedRoles = "Executive" },
    };
}

public class BulkToggleRequest
{
    public int[] Ids { get; set; } = Array.Empty<int>();
    public bool Enable { get; set; }
}
