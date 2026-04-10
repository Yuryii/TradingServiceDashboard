using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dashboard.Models;
using Dashboard.Models.ViewModels;
using Dashboard.Services.Interfaces;
using Dashboard.Data;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Controllers;

[Authorize(Roles = SD.Role_Executive)]
public class JobManagementController : Controller
{
    private readonly IJobSchedulerService _jobScheduler;
    private readonly ApplicationDbContext _context;

    public JobManagementController(
        IJobSchedulerService jobScheduler,
        ApplicationDbContext context)
    {
        _jobScheduler = jobScheduler;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["CurrentPage"] = "JobManagement";
        var configs = await _jobScheduler.GetAllJobConfigsAsync();
        return View(configs);
    }

    public async Task<IActionResult> GetJobConfigs()
    {
        var configs = await _jobScheduler.GetAllJobConfigsAsync();
        return Json(configs);
    }
}
