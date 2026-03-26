using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dashboard.Data;
using Dashboard.Models;
using Dashboard.Models.ViewModels;
using Dashboard.Services;

namespace Dashboard.Controllers
{

[Authorize(Policy = "FinancePolicy")]
public class FinanceCrudController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ExcelCrudService _excelService;

    public FinanceCrudController(ApplicationDbContext context, ExcelCrudService excelService)
    {
        _context = context;
        _excelService = excelService;
    }

    public IActionResult Index() => View();

    #region Expenses
    public async Task<IActionResult> Expenses_Index()
    {
        var expenses = await _context.Expenses.Include(e => e.Employee).Include(e => e.Category).Include(e => e.Branch).AsNoTracking().ToListAsync();
        var vms = expenses.Select(e => new ExpenseListVM { ExpenseID = e.ExpenseID, ExpenseNumber = e.ExpenseNumber, EmployeeName = e.Employee != null ? e.Employee.FullName : null, CategoryName = e.Category != null ? e.Category.CategoryName : null, Amount = e.Amount, Status = e.Status, ExpenseDate = e.ExpenseDate }).ToList();
        return View("Expenses/Index", vms);
    }

    public IActionResult Expenses_Create()
    {
        ViewBag.Employees = _context.Employees.Where(e => e.IsActive).ToList();
        ViewBag.Branches = _context.Branches.Where(b => b.IsActive).ToList();
        ViewBag.Categories = _context.ExpenseCategories.Where(c => c.IsActive).ToList();
        ViewBag.Approvers = _context.Employees.Where(e => e.IsActive).ToList();
        return View("Expenses/Create", new ExpenseCreateVM());
    }

    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Expenses_Create(ExpenseCreateVM model) {
        if (!ModelState.IsValid) { ViewBag.Employees = _context.Employees.Where(e => e.IsActive).ToList(); ViewBag.Branches = _context.Branches.Where(b => b.IsActive).ToList(); ViewBag.Categories = _context.ExpenseCategories.Where(c => c.IsActive).ToList(); ViewBag.Approvers = _context.Employees.Where(e => e.IsActive).ToList(); return View("Expenses/Create", model); }
        _context.Expenses.Add(new Expense { ExpenseNumber = model.ExpenseNumber, EmployeeID = model.EmployeeID, BranchID = model.BranchID, CategoryID = model.CategoryID, ExpenseDate = model.ExpenseDate, Amount = model.Amount, Description = model.Description, ReceiptPath = model.ReceiptPath, Status = model.Status, ApprovedByEmployeeID = model.ApprovedByEmployeeID, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Expense '{model.ExpenseNumber}' created successfully."; return RedirectToAction(nameof(Expenses_Index));
    }

    public async Task<IActionResult> Expenses_Edit(int id) {
        var expense = await _context.Expenses.FindAsync(id); if (expense == null) return NotFound();
        ViewBag.Employees = _context.Employees.Where(e => e.IsActive).ToList(); ViewBag.Branches = _context.Branches.Where(b => b.IsActive).ToList(); ViewBag.Categories = _context.ExpenseCategories.Where(c => c.IsActive).ToList(); ViewBag.Approvers = _context.Employees.Where(e => e.IsActive).ToList();
        return View("Expenses/Edit", new ExpenseEditVM { ExpenseID = expense.ExpenseID, ExpenseNumber = expense.ExpenseNumber, EmployeeID = expense.EmployeeID, BranchID = expense.BranchID, CategoryID = expense.CategoryID, ExpenseDate = expense.ExpenseDate, Amount = expense.Amount, Description = expense.Description, ReceiptPath = expense.ReceiptPath, Status = expense.Status, ApprovedByEmployeeID = expense.ApprovedByEmployeeID, ApprovedDate = expense.ApprovedDate, RejectionReason = expense.RejectionReason, CreatedAt = expense.CreatedAt });
    }

    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Expenses_Edit(ExpenseEditVM model) {
        if (!ModelState.IsValid) { ViewBag.Employees = _context.Employees.Where(e => e.IsActive).ToList(); ViewBag.Branches = _context.Branches.Where(b => b.IsActive).ToList(); ViewBag.Categories = _context.ExpenseCategories.Where(c => c.IsActive).ToList(); ViewBag.Approvers = _context.Employees.Where(e => e.IsActive).ToList(); return View("Expenses/Edit", model); }
        var expense = await _context.Expenses.FindAsync(model.ExpenseID); if (expense == null) return NotFound();
        expense.ExpenseNumber = model.ExpenseNumber; expense.EmployeeID = model.EmployeeID; expense.BranchID = model.BranchID; expense.CategoryID = model.CategoryID; expense.ExpenseDate = model.ExpenseDate; expense.Amount = model.Amount; expense.Description = model.Description; expense.ReceiptPath = model.ReceiptPath; expense.Status = model.Status; expense.ApprovedByEmployeeID = model.ApprovedByEmployeeID; expense.ApprovedDate = model.ApprovedDate; expense.RejectionReason = model.RejectionReason;
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Expense '{model.ExpenseNumber}' updated successfully."; return RedirectToAction(nameof(Expenses_Index));
    }

    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Expenses_Delete(int id) {
        var expense = await _context.Expenses.FindAsync(id); if (expense == null) return NotFound();
        _context.Expenses.Remove(expense); await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Expense '{expense.ExpenseNumber}' deleted successfully."; return RedirectToAction(nameof(Expenses_Index));
    }
    #endregion

    #region ExpenseCategories
    public async Task<IActionResult> ExpenseCategories_Index() {
        var categories = await _context.ExpenseCategories.AsNoTracking().ToListAsync();
        var vms = categories.Select(c => new ExpenseCategoryListVM { ExpenseCategoryID = c.ExpenseCategoryID, CategoryCode = c.CategoryCode, CategoryName = c.CategoryName, Description = c.Description, IsActive = c.IsActive }).ToList();
        return View("ExpenseCategories/Index", vms);
    }

    public IActionResult ExpenseCategories_Create() => View("ExpenseCategories/Create", new ExpenseCategoryCreateVM());

    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> ExpenseCategories_Create(ExpenseCategoryCreateVM model) {
        if (!ModelState.IsValid) return View("ExpenseCategories/Create", model);
        _context.ExpenseCategories.Add(new ExpenseCategory { CategoryCode = model.CategoryCode, CategoryName = model.CategoryName, Description = model.Description, IsActive = model.IsActive });
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Category '{model.CategoryName}' created successfully."; return RedirectToAction(nameof(ExpenseCategories_Index));
    }

    public async Task<IActionResult> ExpenseCategories_Edit(int id) {
        var category = await _context.ExpenseCategories.FindAsync(id); if (category == null) return NotFound();
        return View("ExpenseCategories/Edit", new ExpenseCategoryEditVM { ExpenseCategoryID = category.ExpenseCategoryID, CategoryCode = category.CategoryCode, CategoryName = category.CategoryName, Description = category.Description, IsActive = category.IsActive });
    }

    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> ExpenseCategories_Edit(ExpenseCategoryEditVM model) {
        if (!ModelState.IsValid) return View("ExpenseCategories/Edit", model);
        var category = await _context.ExpenseCategories.FindAsync(model.ExpenseCategoryID); if (category == null) return NotFound();
        category.CategoryCode = model.CategoryCode; category.CategoryName = model.CategoryName; category.Description = model.Description; category.IsActive = model.IsActive;
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Category '{model.CategoryName}' updated successfully."; return RedirectToAction(nameof(ExpenseCategories_Index));
    }

    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> ExpenseCategories_Delete(int id) {
        var category = await _context.ExpenseCategories.FindAsync(id); if (category == null) return NotFound();
        _context.ExpenseCategories.Remove(category); await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Category '{category.CategoryName}' deleted successfully."; return RedirectToAction(nameof(ExpenseCategories_Index));
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

    public class FinanceCrudIndexVM
    {
    }
}