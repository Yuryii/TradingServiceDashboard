using Dashboard.Models;
using Dashboard.Models.ViewModels;
using Dashboard.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Controllers;

[Authorize(Policy = "ExecutivePolicy")]
public class SalesController : Controller
{
    private readonly ISalesDashboardService _service;

    public SalesController(ISalesDashboardService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        ViewData["CurrentPage"] = "Sales";
        var vm = await _service.GetDashboardDataAsync(from, to);
        return View(vm);
    }
}
