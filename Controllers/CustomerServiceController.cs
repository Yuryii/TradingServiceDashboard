using Dashboard.Models;
using Dashboard.Models.ViewModels;
using Dashboard.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Controllers;

[Authorize(Policy = "ExecutivePolicy")]
public class CustomerServiceController : Controller
{
    private readonly ICustomerServiceDashboardService _service;
    private readonly IPdfReportService _pdfService;

    public CustomerServiceController(ICustomerServiceDashboardService service, IPdfReportService pdfService)
    {
        _service = service;
        _pdfService = pdfService;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        ViewData["CurrentPage"] = "CustomerService";
        var vm = await _service.GetDashboardDataAsync(from, to);
        return View(vm);
    }

    public async Task<IActionResult> ExportPdf(DateTime? from, DateTime? to)
    {
        var vm = await _service.GetDashboardDataAsync(from, to);
        var pdfBytes = await _pdfService.GenerateCustomerServicePdfAsync(vm, from, to);
        return File(pdfBytes, "application/pdf", $"CustomerServiceDashboard_{DateTime.Now:yyyyMMdd}.pdf");
    }
}
