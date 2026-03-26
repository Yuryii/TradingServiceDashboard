using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Dashboard.Models;
using Dashboard.Models.ViewModels;

namespace Dashboard.Controllers;

[Authorize(Roles = SD.Role_Executive)]
public class UserManagementController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserManagementController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // GET: /UserManagement
    public async Task<IActionResult> Index()
    {
        var users = new List<UserListVM>();

        foreach (var user in _userManager.Users.ToList())
        {
            var roles = await _userManager.GetRolesAsync(user);
            users.Add(new UserListVM
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                UserName = user.UserName,
                Roles = roles.ToList()
            });
        }

        return View(users);
    }

    // GET: /UserManagement/Create
    public IActionResult Create()
    {
        var allRoles = _roleManager.Roles.Select(r => r.Name!).ToList();
        ViewBag.AllRoles = allRoles;
        return View();
    }

    // POST: /UserManagement/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserVM model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.AllRoles = _roleManager.Roles.Select(r => r.Name!).ToList();
            return View(model);
        }

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError(string.Empty, "A user with this email already exists.");
            ViewBag.AllRoles = _roleManager.Roles.Select(r => r.Name!).ToList();
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            PhoneNumber = model.PhoneNumber,
            CreatedAt = DateTime.Now
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, model.Role);
            TempData["SuccessMessage"] = $"User '{model.Email}' created successfully with role '{model.Role}'.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        ViewBag.AllRoles = _roleManager.Roles.Select(r => r.Name!).ToList();
        return View(model);
    }

    // GET: /UserManagement/Edit/{id}
    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrEmpty(id))
            return NotFound();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);

        var vm = new EditUserVM
        {
            Id = user.Id,
            FullName = user.FullName ?? string.Empty,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            CurrentRoles = currentRoles.ToList()
        };

        ViewBag.AllRoles = _roleManager.Roles.Select(r => r.Name!).ToList();
        return View(vm);
    }

    // POST: /UserManagement/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserVM model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.AllRoles = _roleManager.Roles.Select(r => r.Name!).ToList();
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null)
            return NotFound();

        user.FullName = model.FullName;
        user.PhoneNumber = model.PhoneNumber;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            ViewBag.AllRoles = _roleManager.Roles.Select(r => r.Name!).ToList();
            return View(model);
        }

        // Update roles
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Any())
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
        }
        await _userManager.AddToRoleAsync(user, model.NewRole);

        TempData["SuccessMessage"] = $"User '{user.Email}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /UserManagement/Delete/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrEmpty(id))
            return NotFound();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        // Prevent self-deletion
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser != null && currentUser.Id == id)
        {
            TempData["ErrorMessage"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(Index));
        }

        await _userManager.DeleteAsync(user);
        TempData["SuccessMessage"] = $"User '{user.Email}' deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}
