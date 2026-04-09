using Dashboard.Models;
using Dashboard.Models.ViewModels;
using Dashboard.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Controllers;

[Authorize(Policy = "ExecutivePolicy")]
public class MarketingController : Controller
{
    private readonly IMarketingDashboardService _service;
    private readonly IPdfReportService _pdfService;

    public MarketingController(IMarketingDashboardService service, IPdfReportService pdfService)
    {
        _service = service;
        _pdfService = pdfService;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        ViewData["CurrentPage"] = "Marketing";
        var vm = await _service.GetDashboardDataAsync(from, to);
        return View(vm);
    }

    public async Task<IActionResult> ExportPdf(DateTime? from, DateTime? to)
    {
        var vm = await _service.GetDashboardDataAsync(from, to);
        var pdfBytes = await _pdfService.GenerateMarketingPdfAsync(vm, from, to);
        return File(pdfBytes, "application/pdf", $"MarketingDashboard_{DateTime.Now:yyyyMMdd}.pdf");
    }
}
