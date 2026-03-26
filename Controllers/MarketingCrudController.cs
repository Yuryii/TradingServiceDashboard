using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dashboard.Data;
using Dashboard.Models;
using Dashboard.Models.ViewModels;
using Dashboard.Services;

namespace Dashboard.Controllers;

[Authorize(Policy = "MarketingPolicy")]
public class MarketingCrudController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ExcelCrudService _excelService;

    public MarketingCrudController(ApplicationDbContext context, ExcelCrudService excelService)
    {
        _context = context;
        _excelService = excelService;
    }

    public IActionResult Index()
    {
        ViewData["CurrentPage"] = "MarketingCrud";
        var totalCampaigns = _context.MarketingCampaigns.Count();
        var activeCampaigns = _context.MarketingCampaigns.Count(c => c.IsActive && c.Status != "Completed" && c.Status != "Cancelled");
        var totalLeads = _context.MarketingLeads.Count();
        var totalSpend = _context.MarketingSpendDailies.Sum(s => s.Amount);

        var vm = new MarketingCrudDashboardVM
        {
            TotalCampaigns = totalCampaigns,
            ActiveCampaigns = activeCampaigns,
            TotalLeads = totalLeads,
            TotalSpend = totalSpend
        };

        return View(vm);
    }

    #region MarketingCampaigns

    public IActionResult MarketingCampaignsIndex()
    {
        var list = _context.MarketingCampaigns
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new MarketingCampaignListVM
            {
                CampaignID = c.CampaignID,
                CampaignCode = c.CampaignCode,
                CampaignName = c.CampaignName,
                Channel = c.Channel,
                Budget = c.Budget,
                ActualSpend = c.ActualSpend,
                Status = c.Status,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                IsActive = c.IsActive
            })
            .ToList();
        return View("MarketingCampaigns/Index", list);
    }

    public IActionResult MarketingCampaignsCreate()
    {
        return View("MarketingCampaigns/Create", new MarketingCampaignCreateVM());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult MarketingCampaignsCreate(MarketingCampaignCreateVM model)
    {
        if (!ModelState.IsValid)
            return View("MarketingCampaigns/Create", model);

        var entity = new MarketingCampaign
        {
            CampaignCode = model.CampaignCode,
            CampaignName = model.CampaignName,
            Channel = model.Channel,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            Budget = model.Budget,
            ActualSpend = model.ActualSpend,
            Objective = model.Objective,
            Status = model.Status,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.MarketingCampaigns.Add(entity);
        _context.SaveChanges();

        TempData["SuccessMessage"] = $"Campaign '{model.CampaignName}' created successfully.";
        return RedirectToAction(nameof(MarketingCampaignsIndex));
    }

    public IActionResult MarketingCampaignsEdit(int id)
    {
        var entity = _context.MarketingCampaigns.Find(id);
        if (entity == null) return NotFound();

        var vm = new MarketingCampaignEditVM
        {
            CampaignID = entity.CampaignID,
            CampaignCode = entity.CampaignCode,
            CampaignName = entity.CampaignName,
            Channel = entity.Channel,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Budget = entity.Budget,
            ActualSpend = entity.ActualSpend,
            Objective = entity.Objective,
            Status = entity.Status,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };

        return View("MarketingCampaigns/Edit", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult MarketingCampaignsEdit(MarketingCampaignEditVM model)
    {
        if (!ModelState.IsValid)
            return View("MarketingCampaigns/Edit", model);

        var entity = _context.MarketingCampaigns.Find(model.CampaignID);
        if (entity == null) return NotFound();

        entity.CampaignCode = model.CampaignCode;
        entity.CampaignName = model.CampaignName;
        entity.Channel = model.Channel;
        entity.StartDate = model.StartDate;
        entity.EndDate = model.EndDate;
        entity.Budget = model.Budget;
        entity.ActualSpend = model.ActualSpend;
        entity.Objective = model.Objective;
        entity.Status = model.Status;
        entity.IsActive = model.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        _context.MarketingCampaigns.Update(entity);
        _context.SaveChanges();

        TempData["SuccessMessage"] = $"Campaign '{model.CampaignName}' updated successfully.";
        return RedirectToAction(nameof(MarketingCampaignsIndex));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult MarketingCampaignsDelete(int id)
    {
        var entity = _context.MarketingCampaigns.Find(id);
        if (entity == null) return NotFound();

        _context.MarketingCampaigns.Remove(entity);
        _context.SaveChanges();

        TempData["SuccessMessage"] = $"Campaign '{entity.CampaignName}' deleted successfully.";
        return RedirectToAction(nameof(MarketingCampaignsIndex));
    }

    #endregion

    #region MarketingLeads

    public IActionResult MarketingLeadsIndex()
    {
        var list = _context.MarketingLeads
            .OrderByDescending(l => l.CreatedDate)
            .Select(l => new MarketingLeadListVM
            {
                LeadID = l.LeadID,
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                CompanyName = l.CompanyName,
                Phone = l.Phone,
                Email = l.Email,
                Status = l.Status,
                LeadScore = l.LeadScore,
                CampaignName = l.Campaign != null ? l.Campaign.CampaignName : null,
                CreatedDate = l.CreatedDate
            })
            .ToList();
        return View("MarketingLeads/Index", list);
    }

    public IActionResult MarketingLeadsCreate()
    {
        ViewBag.Campaigns = _context.MarketingCampaigns.Where(c => c.IsActive).ToList();
        ViewBag.Employees = _context.Employees.Where(e => e.IsActive).ToList();
        return View("MarketingLeads/Create", new MarketingLeadCreateVM());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult MarketingLeadsCreate(MarketingLeadCreateVM model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Campaigns = _context.MarketingCampaigns.Where(c => c.IsActive).ToList();
            ViewBag.Employees = _context.Employees.Where(e => e.IsActive).ToList();
            return View("MarketingLeads/Create", model);
        }

        var entity = new MarketingLead
        {
            LeadCode = model.LeadCode,
            CampaignID = model.CampaignID,
            LeadName = model.LeadName,
            CompanyName = model.CompanyName,
            Phone = model.Phone,
            Email = model.Email,
            Source = model.Source,
            Status = model.Status,
            LeadScore = model.LeadScore,
            AssignedEmployeeID = model.AssignedEmployeeID,
            UtmSource = model.UtmSource,
            UtmMedium = model.UtmMedium,
            UtmCampaign = model.UtmCampaign,
            CreatedDate = DateTime.UtcNow
        };

        _context.MarketingLeads.Add(entity);
        _context.SaveChanges();

        TempData["SuccessMessage"] = $"Lead '{model.LeadName}' created successfully.";
        return RedirectToAction(nameof(MarketingLeadsIndex));
    }

    public IActionResult MarketingLeadsEdit(long id)
    {
        var entity = _context.MarketingLeads.Find(id);
        if (entity == null) return NotFound();

        ViewBag.Campaigns = _context.MarketingCampaigns.Where(c => c.IsActive).ToList();
        ViewBag.Employees = _context.Employees.Where(e => e.IsActive).ToList();

        var vm = new MarketingLeadEditVM
        {
            LeadID = entity.LeadID,
            LeadCode = entity.LeadCode,
            CampaignID = entity.CampaignID,
            LeadName = entity.LeadName,
            CompanyName = entity.CompanyName,
            Phone = entity.Phone,
            Email = entity.Email,
            Source = entity.Source,
            Status = entity.Status,
            LeadScore = entity.LeadScore,
            AssignedEmployeeID = entity.AssignedEmployeeID,
            UtmSource = entity.UtmSource,
            UtmMedium = entity.UtmMedium,
            UtmCampaign = entity.UtmCampaign,
            CreatedDate = entity.CreatedDate
        };

        return View("MarketingLeads/Edit", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult MarketingLeadsEdit(MarketingLeadEditVM model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Campaigns = _context.MarketingCampaigns.Where(c => c.IsActive).ToList();
            ViewBag.Employees = _context.Employees.Where(e => e.IsActive).ToList();
            return View("MarketingLeads/Edit", model);
        }

        var entity = _context.MarketingLeads.Find(model.LeadID);
        if (entity == null) return NotFound();

        entity.LeadCode = model.LeadCode;
        entity.CampaignID = model.CampaignID;
        entity.LeadName = model.LeadName;
        entity.CompanyName = model.CompanyName;
        entity.Phone = model.Phone;
        entity.Email = model.Email;
        entity.Source = model.Source;
        entity.Status = model.Status;
        entity.LeadScore = model.LeadScore;
        entity.AssignedEmployeeID = model.AssignedEmployeeID;
        entity.UtmSource = model.UtmSource;
        entity.UtmMedium = model.UtmMedium;
        entity.UtmCampaign = model.UtmCampaign;

        _context.MarketingLeads.Update(entity);
        _context.SaveChanges();

        TempData["SuccessMessage"] = $"Lead '{model.LeadName}' updated successfully.";
        return RedirectToAction(nameof(MarketingLeadsIndex));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult MarketingLeadsDelete(long id)
    {
        var entity = _context.MarketingLeads.Find(id);
        if (entity == null) return NotFound();

        _context.MarketingLeads.Remove(entity);
        _context.SaveChanges();

        TempData["SuccessMessage"] = $"Lead '{entity.LeadName}' deleted successfully.";
        return RedirectToAction(nameof(MarketingLeadsIndex));
    }

    #endregion

    #region MarketingSpendDailies

    public IActionResult MarketingSpendDailiesIndex()
    {
        var list = _context.MarketingSpendDailies
            .OrderByDescending(s => s.SpendDate)
            .Select(s => new MarketingSpendDailyListVM
            {
                SpendID = s.SpendID,
                CampaignName = s.Campaign != null ? s.Campaign.CampaignName : null,
                Amount = s.Amount,
                Impressions = s.Impressions,
                Clicks = s.Clicks,
                Conversions = s.Conversions,
                CPM = s.CPM,
                CPC = s.CPC,
                SpendDate = s.SpendDate
            })
            .ToList();
        return View("MarketingSpendDailies/Index", list);
    }

    public IActionResult MarketingSpendDailiesCreate()
    {
        ViewBag.Campaigns = _context.MarketingCampaigns.ToList();
        return View("MarketingSpendDailies/Create", new MarketingSpendDailyCreateVM());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult MarketingSpendDailiesCreate(MarketingSpendDailyCreateVM model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Campaigns = _context.MarketingCampaigns.ToList();
            return View("MarketingSpendDailies/Create", model);
        }

        var entity = new MarketingSpendDaily
        {
            CampaignID = model.CampaignID,
            SpendDate = model.SpendDate,
            Amount = model.Amount,
            Impressions = model.Impressions,
            Clicks = model.Clicks,
            Conversions = model.Conversions,
            CPM = model.CPM,
            CPC = model.CPC,
            CPA = model.CPA,
            Notes = model.Notes
        };

        _context.MarketingSpendDailies.Add(entity);
        _context.SaveChanges();

        TempData["SuccessMessage"] = "Marketing spend record created successfully.";
        return RedirectToAction(nameof(MarketingSpendDailiesIndex));
    }

    public IActionResult MarketingSpendDailiesEdit(long id)
    {
        var entity = _context.MarketingSpendDailies.Find(id);
        if (entity == null) return NotFound();

        ViewBag.Campaigns = _context.MarketingCampaigns.ToList();

        var vm = new MarketingSpendDailyEditVM
        {
            SpendID = entity.SpendID,
            CampaignID = entity.CampaignID,
            SpendDate = entity.SpendDate,
            Amount = entity.Amount,
            Impressions = entity.Impressions,
            Clicks = entity.Clicks,
            Conversions = entity.Conversions,
            CPM = entity.CPM,
            CPC = entity.CPC,
            CPA = entity.CPA,
            Notes = entity.Notes
        };

        return View("MarketingSpendDailies/Edit", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult MarketingSpendDailiesEdit(MarketingSpendDailyEditVM model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Campaigns = _context.MarketingCampaigns.ToList();
            return View("MarketingSpendDailies/Edit", model);
        }

        var entity = _context.MarketingSpendDailies.Find(model.SpendID);
        if (entity == null) return NotFound();

        entity.CampaignID = model.CampaignID;
        entity.SpendDate = model.SpendDate;
        entity.Amount = model.Amount;
        entity.Impressions = model.Impressions;
        entity.Clicks = model.Clicks;
        entity.Conversions = model.Conversions;
        entity.CPM = model.CPM;
        entity.CPC = model.CPC;
        entity.CPA = model.CPA;
        entity.Notes = model.Notes;

        _context.MarketingSpendDailies.Update(entity);
        _context.SaveChanges();

        TempData["SuccessMessage"] = "Marketing spend record updated successfully.";
        return RedirectToAction(nameof(MarketingSpendDailiesIndex));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult MarketingSpendDailiesDelete(long id)
    {
        var entity = _context.MarketingSpendDailies.Find(id);
        if (entity == null) return NotFound();

        _context.MarketingSpendDailies.Remove(entity);
        _context.SaveChanges();

        TempData["SuccessMessage"] = "Marketing spend record deleted successfully.";
        return RedirectToAction(nameof(MarketingSpendDailiesIndex));
    }

    #endregion

    public async Task<IActionResult> Export(string? entityType, string? entityName)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            var actionName = RouteData.Values["action"]?.ToString() ?? "";
            if (actionName.EndsWith("_Export", StringComparison.OrdinalIgnoreCase))
                entityType = actionName[..^7];
        }

        if (string.IsNullOrWhiteSpace(entityType))
            return BadRequest("entityType is required");

        var entityName2 = entityName ?? entityType;
        try
        {
            var bytes = await _excelService.ExportAsync(entityType);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{entityName2}_Export_{DateTime.Now:yyyyMMdd}.xlsx");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Export error: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }
}

public class MarketingCrudDashboardVM
{
    public int TotalCampaigns { get; set; }
    public int ActiveCampaigns { get; set; }
    public int TotalLeads { get; set; }
    public decimal TotalSpend { get; set; }
}
