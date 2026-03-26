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

    public MarketingController(IMarketingDashboardService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        ViewData["CurrentPage"] = "Marketing";
        var vm = await _service.GetDashboardDataAsync(from, to);
        return View(vm);
    }
}
