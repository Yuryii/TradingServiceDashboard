using Dashboard.Models;
using Dashboard.Models.ViewModels;
using Dashboard.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Controllers;

[Authorize(Roles = "HumanResources,Executive")]
public class HumanResourcesController : Controller
{
    private readonly IHumanResourcesDashboardService _service;
    private readonly IPdfReportService _pdfService;

    public HumanResourcesController(IHumanResourcesDashboardService service, IPdfReportService pdfService)
    {
        _service = service;
        _pdfService = pdfService;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        ViewData["CurrentPage"] = "HR";
        ViewData["AIDepartment"] = "HR";
        var vm = await _service.GetDashboardDataAsync(from, to);
        return View(vm);
    }

    public async Task<IActionResult> ExportPdf(DateTime? from, DateTime? to)
    {
        var vm = await _service.GetDashboardDataAsync(from, to);
        var pdfBytes = await _pdfService.GenerateHrPdfAsync(vm, from, to);
        return File(pdfBytes, "application/pdf", $"HRDashboard_{DateTime.Now:yyyyMMdd}.pdf");
    }
}
