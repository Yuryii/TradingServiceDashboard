using Dashboard.Models;
using Dashboard.Models.ViewModels;
using Dashboard.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Controllers;

[Authorize]
public class ExecutiveController : Controller
{
    private readonly IExecutiveDashboardService _service;
    private readonly IPdfReportService _pdfService;

    public ExecutiveController(IExecutiveDashboardService service, IPdfReportService pdfService)
    {
        _service = service;
        _pdfService = pdfService;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        ViewData["CurrentPage"] = "Executive";
        var vm = await _service.GetDashboardDataAsync(from, to);
        return View(vm);
    }

    public async Task<IActionResult> ExportPdf(DateTime? from, DateTime? to)
    {
        var vm = await _service.GetDashboardDataAsync(from, to);
        var pdfBytes = await _pdfService.GenerateExecutivePdfAsync(vm, from, to);
        return File(pdfBytes, "application/pdf", $"ExecutiveDashboard_{DateTime.Now:yyyyMMdd}.pdf");
    }

    public async Task<IActionResult> DebugRevenueData(DateTime? from, DateTime? to)
    {
        var vm = await _service.GetDashboardDataAsync(from, to);
        return Json(vm.RevenueProfitAreaChart);
    }
}
