using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Dashboard.Data;
using Dashboard.Models;

namespace Dashboard.Data;

public class RoleSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RoleSeeder> _logger;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public RoleSeeder(ApplicationDbContext context, ILogger<RoleSeeder> logger, 
        RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _logger = logger;
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task SeedRolesAndUsersAsync()
    {
        // Create Roles if they don't exist
        var roles = new[]
        {
            SD.Role_Executive,
            SD.Role_Sales,
            SD.Role_Marketing,
            SD.Role_Inventory,
            SD.Role_Finance,
            SD.Role_HumanResources,
            SD.Role_CustomerService
        };

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
                _logger.LogInformation("Created role: {Role}", role);
            }
        }

        // Create Users with roles
        var users = new[]
        {
            (Email: "executive@company.com", Password: "Executive123!", Role: SD.Role_Executive, FullName: "Executive User"),
            (Email: "sales@company.com", Password: "Sales123!", Role: SD.Role_Sales, FullName: "Sales User"),
            (Email: "marketing@company.com", Password: "Marketing123!", Role: SD.Role_Marketing, FullName: "Marketing User"),
            (Email: "inventory@company.com", Password: "Inventory123!", Role: SD.Role_Inventory, FullName: "Inventory User"),
            (Email: "finance@company.com", Password: "Finance123!", Role: SD.Role_Finance, FullName: "Finance User"),
            (Email: "hr@company.com", Password: "Hr123456!", Role: SD.Role_HumanResources, FullName: "HR User"),
            (Email: "cskh@company.com", Password: "Cskh123456!", Role: SD.Role_CustomerService, FullName: "Customer Service User")
        };

        foreach (var user in users)
        {
            var existingUser = await _userManager.FindByEmailAsync(user.Email);
            if (existingUser == null)
            {
                var appUser = new ApplicationUser
                {
                    UserName = user.Email,
                    Email = user.Email,
                    FullName = user.FullName,
                    CreatedAt = DateTime.Now
                };

                var result = await _userManager.CreateAsync(appUser, user.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(appUser, user.Role);
                    _logger.LogInformation("Created user: {Email} with role: {Role}", user.Email, user.Role);
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        _logger.LogError("Error creating user {Email}: {Error}", user.Email, error.Description);
                    }
                }
            }
            else
            {
                // Ensure user has the correct role
                if (!await _userManager.IsInRoleAsync(existingUser, user.Role))
                {
                    await _userManager.AddToRoleAsync(existingUser, user.Role);
                    _logger.LogInformation("Added role {Role} to existing user: {Email}", user.Role, user.Email);
                }
            }
        }

        _logger.LogInformation("Roles and Users seeding completed!");
    }
}
