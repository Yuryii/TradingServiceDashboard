using Dashboard.Models;
using Dashboard.Models.ViewModels;
using Dashboard.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Controllers;

[Authorize(Policy = "ExecutivePolicy")]
public class FinanceController : Controller
{
    private readonly IFinanceDashboardService _service;
    private readonly IPdfReportService _pdfService;

    public FinanceController(IFinanceDashboardService service, IPdfReportService pdfService)
    {
        _service = service;
        _pdfService = pdfService;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        ViewData["CurrentPage"] = "Finance";
        var vm = await _service.GetDashboardDataAsync(from, to);
        return View(vm);
    }

    public async Task<IActionResult> ExportPdf(DateTime? from, DateTime? to)
    {
        var vm = await _service.GetDashboardDataAsync(from, to);
        var pdfBytes = await _pdfService.GenerateFinancePdfAsync(vm, from, to);
        return File(pdfBytes, "application/pdf", $"FinanceDashboard_{DateTime.Now:yyyyMMdd}.pdf");
    }
}
