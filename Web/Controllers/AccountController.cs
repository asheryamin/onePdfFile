using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using onePdfFile.Web.Models;

namespace onePdfFile.Web.Controllers;

public class AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Invoices");
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await signInManager.PasswordSignInAsync(
            model.UserName, model.Password, isPersistent: false, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var user = await userManager.FindByNameAsync(model.UserName);
            if (user?.IsFirstLogin == true)
                return RedirectToAction(nameof(ChangePassword));

            return LocalRedirect(returnUrl ?? Url.Action("Index", "Invoices")!);
        }

        if (result.IsLockedOut)
            ModelState.AddModelError(string.Empty, "החשבון נעול זמנית עקב ניסיונות כניסה כושלים. נסה שוב מאוחר יותר.");
        else
            ModelState.AddModelError(string.Empty, "שם משתמש או סיסמה שגויים.");

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction(nameof(Login));

        var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        user.IsFirstLogin = false;
        await userManager.UpdateAsync(user);
        await signInManager.RefreshSignInAsync(user);

        TempData["Success"] = "הסיסמה שונתה בהצלחה.";
        return RedirectToAction("Index", "Invoices");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
