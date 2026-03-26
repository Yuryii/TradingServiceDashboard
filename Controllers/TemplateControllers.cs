using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Dashboard.Models;
using Dashboard.Models.ViewModels;

namespace Dashboard.Controllers;

public class CardsController : Controller
{
    public IActionResult Basic() => View();
}

public class TablesController : Controller
{
    public IActionResult Basic() => View();
}

    [Authorize]
    public class PagesController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public PagesController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public async Task<IActionResult> AccountSettings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("LoginBasic", "Auth");

            var vm = new UpdateProfileVM
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                UserName = user.UserName,
                EmployeeID = user.EmployeeID
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AccountSettings(UpdateProfileVM model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("LoginBasic", "Auth");

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                if (string.IsNullOrEmpty(model.CurrentPassword))
                {
                    ModelState.AddModelError(string.Empty, "Current password is required to change password.");
                    return View(model);
                }

                var passwordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (!passwordResult.Succeeded)
                {
                    foreach (var error in passwordResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }

                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "Profile and password updated successfully!";
            }
            else
            {
                TempData["SuccessMessage"] = "Profile updated successfully!";
            }

            return RedirectToAction(nameof(AccountSettings));
        }

    public IActionResult AccountSettingsNotifications() => View();
    public IActionResult AccountSettingsConnections() => View();
    public IActionResult MiscError() => View();
    public IActionResult MiscUnderMaintenance() => View();
}

public class LayoutExamplesController : Controller
{
    public IActionResult WithoutMenu() => View();
    public IActionResult WithoutNavbar() => View();
    public IActionResult Fluid() => View();
    public IActionResult Container() => View();
    public IActionResult Blank() => View();
}

public class IconsController : Controller
{
    public IActionResult Boxicons() => View();
}

public class FormsController : Controller
{
    public IActionResult BasicInputs() => View();
    public IActionResult InputGroups() => View();
}

public class FormLayoutsController : Controller
{
    public IActionResult Vertical() => View();
    public IActionResult Horizontal() => View();
}

public class ExtendedUiController : Controller
{
    public IActionResult PerfectScrollbar() => View();
    public IActionResult TextDivider() => View();
}

public class UiController : Controller
{
    public IActionResult Accordion() => View();
    public IActionResult Alerts() => View();
    public IActionResult Badges() => View();
    public IActionResult Buttons() => View();
    public IActionResult Carousel() => View();
    public IActionResult Collapse() => View();
    public IActionResult Dropdowns() => View();
    public IActionResult Footer() => View();
    public IActionResult ListGroups() => View();
    public IActionResult Modals() => View();
    public IActionResult Navbar() => View();
    public IActionResult Offcanvas() => View();
    public IActionResult PaginationBreadcrumbs() => View();
    public IActionResult Progress() => View();
    public IActionResult Spinners() => View();
    public IActionResult TabsPills() => View();
    public IActionResult Toasts() => View();
    public IActionResult TooltipsPopovers() => View();
    public IActionResult Typography() => View();
}

public class AuthController : Controller
{
    public IActionResult LoginBasic() => View();
    public IActionResult RegisterBasic() => View();
    public IActionResult ForgotPasswordBasic() => View();
}
