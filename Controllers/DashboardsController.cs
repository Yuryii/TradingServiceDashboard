using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dashboard.Data;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Controllers;

[Authorize]
public class DashboardsController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["CurrentPage"] = "Dashboard";

        var userName = User.Identity?.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
        ViewData["FullName"] = user?.FullName ?? userName ?? "User";

        return View();
    }
}
