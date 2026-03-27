using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dashboard.Data;
using Dashboard.Models;
using Dashboard.Models.ViewModels;
using Dashboard.Services;

namespace Dashboard.Controllers
{

[Authorize(Roles = SD.Role_Executive)]
public class ExecutiveCrudController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ExcelCrudService _excelService;

    public ExecutiveCrudController(ApplicationDbContext context, ExcelCrudService excelService)
    {
        _context = context;
        _excelService = excelService;
    }

    public IActionResult Index()
    {
        var regionCount = _context.Regions.Count();
        var branchCount = _context.Branches.Count();
        var departmentCount = _context.Departments.Count();
        ViewBag.RegionCount = regionCount;
        ViewBag.BranchCount = branchCount;
        ViewBag.DepartmentCount = departmentCount;
        return View();
    }

    #region Regions
    public IActionResult Regions_Index()
    {
        var regions = _context.Regions.OrderBy(r => r.RegionName)
            .Select(r => new RegionListVM { RegionID = r.RegionID, RegionCode = r.RegionCode, RegionName = r.RegionName, Description = r.Description, IsActive = r.IsActive })
            .ToList();
        return View("Regions/Index", regions);
    }

    public IActionResult Regions_Create() => View("Regions/Create");

    [HttpPost][ValidateAntiForgeryToken] public IActionResult Regions_Create(RegionCreateVM model) {
        if (!ModelState.IsValid) return View("Regions/Create", model);
        _context.Regions.Add(new Region { RegionCode = model.RegionCode, RegionName = model.RegionName, Description = model.Description, IsActive = model.IsActive, CreatedAt = DateTime.UtcNow });
        _context.SaveChanges();
        TempData["SuccessMessage"] = $"Region '{model.RegionName}' created successfully."; return RedirectToAction(nameof(Regions_Index));
    }

    public IActionResult Regions_Edit(int id) {
        var region = _context.Regions.Find(id); if (region == null) return NotFound();
        return View("Regions/Edit", new RegionEditVM { RegionID = region.RegionID, RegionCode = region.RegionCode, RegionName = region.RegionName, Description = region.Description, IsActive = region.IsActive, CreatedAt = region.CreatedAt });
    }

    [HttpPost][ValidateAntiForgeryToken] public IActionResult Regions_Edit(RegionEditVM model) {
        if (!ModelState.IsValid) return View("Regions/Edit", model);
        var region = _context.Regions.Find(model.RegionID); if (region == null) return NotFound();
        region.RegionCode = model.RegionCode; region.RegionName = model.RegionName; region.Description = model.Description; region.IsActive = model.IsActive; region.UpdatedAt = DateTime.UtcNow;
        _context.SaveChanges();
        TempData["SuccessMessage"] = $"Region '{model.RegionName}' updated successfully."; return RedirectToAction(nameof(Regions_Index));
    }

    [HttpPost][ValidateAntiForgeryToken] public IActionResult Regions_Delete(int id) {
        var region = _context.Regions.Find(id); if (region == null) return NotFound();
        var hasBranches = _context.Branches.Any(b => b.RegionID == id);
        if (hasBranches) { TempData["ErrorMessage"] = "Cannot delete region. It has associated branches."; return RedirectToAction(nameof(Regions_Index)); }
        var regionName = region.RegionName;
        _context.Regions.Remove(region); _context.SaveChanges();
        TempData["SuccessMessage"] = $"Region '{regionName}' deleted successfully."; return RedirectToAction(nameof(Regions_Index));
    }
    #endregion

    #region Branches
    public IActionResult Branches_Index()
    {
        var branches = _context.Branches.Include(b => b.Region).OrderBy(b => b.BranchName)
            .Select(b => new BranchListVM { BranchID = b.BranchID, BranchCode = b.BranchCode, BranchName = b.BranchName, RegionName = b.Region != null ? b.Region.RegionName : null, City = b.City, IsHeadOffice = b.IsHeadOffice, IsActive = b.IsActive })
            .ToList();
        return View("Branches/Index", branches);
    }

    public IActionResult Branches_Create() { ViewBag.Regions = _context.Regions.Where(r => r.IsActive).OrderBy(r => r.RegionName).ToList(); return View("Branches/Create"); }

    [HttpPost][ValidateAntiForgeryToken] public IActionResult Branches_Create(BranchCreateVM model) {
        if (!ModelState.IsValid) { ViewBag.Regions = _context.Regions.Where(r => r.IsActive).OrderBy(r => r.RegionName).ToList(); return View("Branches/Create", model); }
        _context.Branches.Add(new Branch { BranchCode = model.BranchCode, BranchName = model.BranchName, RegionID = model.RegionID, AddressLine = model.AddressLine, City = model.City, Province = model.Province, Country = model.Country, Phone = model.Phone, Email = model.Email, IsHeadOffice = model.IsHeadOffice, IsActive = model.IsActive, CreatedAt = DateTime.UtcNow });
        _context.SaveChanges();
        TempData["SuccessMessage"] = $"Branch '{model.BranchName}' created successfully."; return RedirectToAction(nameof(Branches_Index));
    }

    public IActionResult Branches_Edit(int id) {
        var branch = _context.Branches.Find(id); if (branch == null) return NotFound();
        ViewBag.Regions = _context.Regions.Where(r => r.IsActive).OrderBy(r => r.RegionName).ToList();
        return View("Branches/Edit", new BranchEditVM { BranchID = branch.BranchID, BranchCode = branch.BranchCode, BranchName = branch.BranchName, RegionID = branch.RegionID, AddressLine = branch.AddressLine, City = branch.City, Province = branch.Province, Country = branch.Country, Phone = branch.Phone, Email = branch.Email, IsHeadOffice = branch.IsHeadOffice, IsActive = branch.IsActive, CreatedAt = branch.CreatedAt });
    }

    [HttpPost][ValidateAntiForgeryToken] public IActionResult Branches_Edit(BranchEditVM model) {
        if (!ModelState.IsValid) { ViewBag.Regions = _context.Regions.Where(r => r.IsActive).OrderBy(r => r.RegionName).ToList(); return View("Branches/Edit", model); }
        var branch = _context.Branches.Find(model.BranchID); if (branch == null) return NotFound();
        branch.BranchCode = model.BranchCode; branch.BranchName = model.BranchName; branch.RegionID = model.RegionID; branch.AddressLine = model.AddressLine; branch.City = model.City; branch.Province = model.Province; branch.Country = model.Country; branch.Phone = model.Phone; branch.Email = model.Email; branch.IsHeadOffice = model.IsHeadOffice; branch.IsActive = model.IsActive; branch.UpdatedAt = DateTime.UtcNow;
        _context.SaveChanges();
        TempData["SuccessMessage"] = $"Branch '{model.BranchName}' updated successfully."; return RedirectToAction(nameof(Branches_Index));
    }

    [HttpPost][ValidateAntiForgeryToken] public IActionResult Branches_Delete(int id) {
        var branch = _context.Branches.Find(id); if (branch == null) return NotFound();
        var branchName = branch.BranchName;
        _context.Branches.Remove(branch); _context.SaveChanges();
        TempData["SuccessMessage"] = $"Branch '{branchName}' deleted successfully."; return RedirectToAction(nameof(Branches_Index));
    }
    #endregion

    #region Export
    public async Task<IActionResult> Region_Export() => await Export("Region", "Region");
    public async Task<IActionResult> Branch_Export() => await Export("Branch", "Branch");
    public async Task<IActionResult> Department_Export() => await Export("Department", "Department");
    #endregion

    #region Departments
    public IActionResult Departments_Index()
    {
        var departments = _context.Departments.OrderBy(d => d.DepartmentName)
            .Select(d => new DepartmentListVM { DepartmentID = d.DepartmentID, DepartmentCode = d.DepartmentCode, DepartmentName = d.DepartmentName, Description = d.Description, IsActive = d.IsActive })
            .ToList();
        return View("Departments/Index", departments);
    }

    public IActionResult Departments_Create() => View("Departments/Create");

    [HttpPost][ValidateAntiForgeryToken] public IActionResult Departments_Create(DepartmentCreateVM model) {
        if (!ModelState.IsValid) return View("Departments/Create", model);
        _context.Departments.Add(new Department { DepartmentCode = model.DepartmentCode, DepartmentName = model.DepartmentName, Description = model.Description, IsActive = model.IsActive, CreatedAt = DateTime.UtcNow });
        _context.SaveChanges();
        TempData["SuccessMessage"] = $"Department '{model.DepartmentName}' created successfully."; return RedirectToAction(nameof(Departments_Index));
    }

    public IActionResult Departments_Edit(int id) {
        var department = _context.Departments.Find(id); if (department == null) return NotFound();
        return View("Departments/Edit", new DepartmentEditVM { DepartmentID = department.DepartmentID, DepartmentCode = department.DepartmentCode, DepartmentName = department.DepartmentName, Description = department.Description, IsActive = department.IsActive, CreatedAt = department.CreatedAt });
    }

    [HttpPost][ValidateAntiForgeryToken] public IActionResult Departments_Edit(DepartmentEditVM model) {
        if (!ModelState.IsValid) return View("Departments/Edit", model);
        var department = _context.Departments.Find(model.DepartmentID); if (department == null) return NotFound();
        department.DepartmentCode = model.DepartmentCode; department.DepartmentName = model.DepartmentName; department.Description = model.Description; department.IsActive = model.IsActive; department.UpdatedAt = DateTime.UtcNow;
        _context.SaveChanges();
        TempData["SuccessMessage"] = $"Department '{model.DepartmentName}' updated successfully."; return RedirectToAction(nameof(Departments_Index));
    }

    [HttpPost][ValidateAntiForgeryToken] public IActionResult Departments_Delete(int id) {
        var department = _context.Departments.Find(id); if (department == null) return NotFound();
        var hasEmployees = _context.Employees.Any(e => e.DepartmentID == id);
        if (hasEmployees) { TempData["ErrorMessage"] = "Cannot delete department. It has associated employees."; return RedirectToAction(nameof(Departments_Index)); }
        var departmentName = department.DepartmentName;
        _context.Departments.Remove(department); _context.SaveChanges();
        TempData["SuccessMessage"] = $"Department '{departmentName}' deleted successfully."; return RedirectToAction(nameof(Departments_Index));
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

public class ExecutiveCrudIndexVM
    {
    }
}