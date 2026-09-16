using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Core.Security;
using PropertyManagement.Data;
using PropertyManagement.Web.Models.Account;

namespace PropertyManagement.Web.Controllers;

/// <summary>Registering, logging in and logging out. Identity's own Razor Pages UI isn't used.</summary>
public class AccountController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToHome();

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        // The role comes from the form, so it's never trusted: only the two known roles are accepted.
        if (!string.IsNullOrEmpty(model.Role) && !Roles.All.Contains(model.Role))
            ModelState.AddModelError(nameof(model.Role), "Choose whether you are an applicant or a property manager.");

        if (!ModelState.IsValid)
            return View(model);

        var email = model.Email.Trim();
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim()
        };

        var created = await _userManager.CreateAsync(user, model.Password);
        if (!created.Succeeded)
        {
            AddErrors(created);
            return View(model);
        }

        var roleAdded = await _userManager.AddToRoleAsync(user, model.Role);
        if (!roleAdded.Succeeded)
        {
            // Don't leave behind an account with no role, which couldn't do anything.
            await _userManager.DeleteAsync(user);
            AddErrors(roleAdded);
            return View(model);
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToHome();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToHome();

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _signInManager.PasswordSignInAsync(
            model.Email.Trim(), model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            // Only redirect inside this site, so a crafted returnUrl can't send users elsewhere.
            if (Url.IsLocalUrl(model.ReturnUrl))
                return LocalRedirect(model.ReturnUrl!);

            return RedirectToHome();
        }

        if (result.IsLockedOut)
            ModelState.AddModelError(string.Empty, "This account is locked after too many failed attempts. Try again later.");
        else
            ModelState.AddModelError(string.Empty, "Invalid email or password.");

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToHome();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private IActionResult RedirectToHome()
    {
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);
    }
}
