using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Dashboard.Models;

namespace Dashboard.Controllers;

[AllowAnonymous]
public class HomeController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public HomeController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<IActionResult> Index()
    {
        // If user is not authenticated, redirect to login
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return RedirectToAction("LoginBasic", "Auth");
        }

        // Get user and their roles
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("LoginBasic", "Auth");
        }

        var roles = await _userManager.GetRolesAsync(user);

        // Redirect based on primary role (Executive has highest priority, then check others)
        if (roles.Contains(SD.Role_Executive))
            return RedirectToAction("Index", "Executive");
        if (roles.Contains(SD.Role_Sales))
            return RedirectToAction("Index", "Sales");
        if (roles.Contains(SD.Role_Marketing))
            return RedirectToAction("Index", "Marketing");
        if (roles.Contains(SD.Role_Inventory))
            return RedirectToAction("Index", "Inventory");
        if (roles.Contains(SD.Role_Finance))
            return RedirectToAction("Index", "Finance");
        if (roles.Contains(SD.Role_HumanResources))
            return RedirectToAction("Index", "HumanResources");
        if (roles.Contains(SD.Role_CustomerService))
            return RedirectToAction("Index", "CustomerService");

        // Fallback if no role matches
        return RedirectToAction("Index", "Dashboards");
    }
}
