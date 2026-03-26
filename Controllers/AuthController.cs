using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Dashboard.Models;

namespace AspnetCoreMvcFull.Controllers;

public class AuthController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult LoginBasic()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginBasic(string email, string password, bool rememberMe = false)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError(string.Empty, "Please enter email and password.");
            return View();
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = await _userManager.FindByNameAsync(email);
        }

        if (user != null)
        {
            var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: true);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");
                
                // Redirect based on user role
                var roles = await _userManager.GetRolesAsync(user);
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
                    
                return RedirectToAction("Index", "Dashboards");
            }
            
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                ModelState.AddModelError(string.Empty, "Account is locked. Please try again later.");
                return View();
            }
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View();
    }

    [HttpGet]
    public IActionResult RegisterBasic()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterBasic(string email, string password, string confirmPassword, string fullName)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError(string.Empty, "Email and password are required.");
            return View();
        }

        if (password != confirmPassword)
        {
            ModelState.AddModelError(string.Empty, "Passwords do not match.");
            return View();
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            CreatedAt = DateTime.Now
        };

        var result = await _userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            _logger.LogInformation("User created a new account with password.");

            // Assign default role to new user
            await _userManager.AddToRoleAsync(user, SD.Role_Sales);

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Dashboards");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        return RedirectToAction("LoginBasic", "Auth");
    }

    public IActionResult ForgotPasswordBasic() => View();

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
