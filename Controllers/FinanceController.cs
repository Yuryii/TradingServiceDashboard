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

    public FinanceController(IFinanceDashboardService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        ViewData["CurrentPage"] = "Finance";
        var vm = await _service.GetDashboardDataAsync(from, to);
        return View(vm);
    }
}
