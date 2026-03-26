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

    public HumanResourcesController(IHumanResourcesDashboardService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        ViewData["CurrentPage"] = "HR";
        var vm = await _service.GetDashboardDataAsync(from, to);
        return View(vm);
    }
}
