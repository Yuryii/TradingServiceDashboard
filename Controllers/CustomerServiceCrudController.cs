using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dashboard.Data;
using Dashboard.Models;
using Dashboard.Models.ViewModels;
using Dashboard.Services;

namespace Dashboard.Controllers
{

[Authorize(Policy = "CustomerServicePolicy")]
public class CustomerServiceCrudController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ExcelCrudService _excelService;

    public CustomerServiceCrudController(ApplicationDbContext context, ExcelCrudService excelService)
    {
        _context = context;
        _excelService = excelService;
    }

    public IActionResult Index() => View();

    #region SupportTickets
    public async Task<IActionResult> SupportTickets_Index()
    {
        var tickets = await _context.SupportTickets.Include(t => t.Customer).Include(t => t.AssignedToEmployee).Include(t => t.Branch).AsNoTracking().ToListAsync();
        var vms = tickets.Select(t => new SupportTicketListVM { TicketID = t.TicketID, TicketNumber = t.TicketNumber, Subject = t.Subject, CustomerName = t.Customer != null ? t.Customer.CustomerName : null, Priority = t.Priority, Status = t.Status, CreatedAt = t.CreatedAt }).ToList();
        return View("SupportTickets/Index", vms);
    }

    public IActionResult SupportTickets_Create()
    {
        ViewBag.Customers = _context.Customers.Where(c => c.IsActive).ToList();
        ViewBag.Employees = _context.Employees.Where(e => e.IsActive).ToList();
        ViewBag.Branches = _context.Branches.Where(b => b.IsActive).ToList();
        return View("SupportTickets/Create", new SupportTicketCreateVM());
    }

    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> SupportTickets_Create(SupportTicketCreateVM model) {
        if (!ModelState.IsValid) { ViewBag.Customers = _context.Customers.Where(c => c.IsActive).ToList(); ViewBag.Employees = _context.Employees.Where(e => e.IsActive).ToList(); ViewBag.Branches = _context.Branches.Where(b => b.IsActive).ToList(); return View("SupportTickets/Create", model); }
        _context.SupportTickets.Add(new SupportTicket { TicketNumber = model.TicketNumber, CustomerID = model.CustomerID, AssignedToEmployeeID = model.AssignedToEmployeeID, BranchID = model.BranchID, Subject = model.Subject, TicketType = model.TicketType, Priority = model.Priority, Status = model.Status, Description = model.Description, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Support ticket '{model.TicketNumber}' created successfully."; return RedirectToAction(nameof(SupportTickets_Index));
    }

    public async Task<IActionResult> SupportTickets_Edit(int id) {
        var ticket = await _context.SupportTickets.FindAsync(id); if (ticket == null) return NotFound();
        ViewBag.Customers = _context.Customers.Where(c => c.IsActive).ToList(); ViewBag.Employees = _context.Employees.Where(e => e.IsActive).ToList(); ViewBag.Branches = _context.Branches.Where(b => b.IsActive).ToList();
        return View("SupportTickets/Edit", new SupportTicketEditVM { TicketID = ticket.TicketID, TicketNumber = ticket.TicketNumber, CustomerID = ticket.CustomerID, AssignedToEmployeeID = ticket.AssignedToEmployeeID, BranchID = ticket.BranchID, Subject = ticket.Subject, TicketType = ticket.TicketType, Priority = ticket.Priority, Status = ticket.Status, Description = ticket.Description, ResolvedDate = ticket.ResolvedDate, Resolution = ticket.Resolution, CreatedAt = ticket.CreatedAt });
    }

    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> SupportTickets_Edit(SupportTicketEditVM model) {
        if (!ModelState.IsValid) { ViewBag.Customers = _context.Customers.Where(c => c.IsActive).ToList(); ViewBag.Employees = _context.Employees.Where(e => e.IsActive).ToList(); ViewBag.Branches = _context.Branches.Where(b => b.IsActive).ToList(); return View("SupportTickets/Edit", model); }
        var ticket = await _context.SupportTickets.FindAsync(model.TicketID); if (ticket == null) return NotFound();
        ticket.TicketNumber = model.TicketNumber; ticket.CustomerID = model.CustomerID; ticket.AssignedToEmployeeID = model.AssignedToEmployeeID; ticket.BranchID = model.BranchID; ticket.Subject = model.Subject; ticket.TicketType = model.TicketType; ticket.Priority = model.Priority; ticket.Status = model.Status; ticket.Description = model.Description; ticket.ResolvedDate = model.ResolvedDate; ticket.Resolution = model.Resolution; ticket.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Support ticket '{model.TicketNumber}' updated successfully."; return RedirectToAction(nameof(SupportTickets_Index));
    }

    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> SupportTickets_Delete(int id) {
        var ticket = await _context.SupportTickets.FindAsync(id); if (ticket == null) return NotFound();
        _context.SupportTickets.Remove(ticket); await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Support ticket '{ticket.TicketNumber}' deleted successfully."; return RedirectToAction(nameof(SupportTickets_Index));
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

public class CustomerServiceCrudIndexVM
    {
    }
}