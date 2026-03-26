using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dashboard.Data;
using Dashboard.Services;

namespace Dashboard.Controllers;

[Authorize]
public class ExcelCrudControllerBase : Controller
{
    protected readonly ApplicationDbContext _context;
    protected readonly ExcelCrudService _excelService;

    public ExcelCrudControllerBase(ApplicationDbContext context, ExcelCrudService excelService)
    {
        _context = context;
        _excelService = excelService;
    }

    // GET: Open Import Modal (returns partial view)
    public virtual IActionResult Import(string entityName)
    {
        return View($"~/Views/Shared/Partials/_ExcelImportModal.cshtml",
            new Models.ViewModels.ExcelImportModalVM
            {
                ModalId = entityName,
                EntityType = entityName,
                EntityName = entityName,
                EntityDisplayName = entityName
            });
    }
}
