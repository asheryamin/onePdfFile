using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using onePdfFile.Web.Data;
using onePdfFile.Web.Models;
using onePdfFile.Web.Services;

namespace onePdfFile.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(
    AppDbContext db,
    UserManager<AppUser> userManager,
    EmailService emailService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var customers = await db.Customers
            .Include(c => c.User)
            .Select(c => new CustomerListItem
            {
                CustomerId = c.Id,
                DisplayName = c.DisplayName,
                UserName = c.User.UserName ?? "",
                Email = c.User.Email ?? "",
                IsFirstLogin = c.User.IsFirstLogin
            })
            .OrderBy(c => c.DisplayName)
            .ToListAsync();

        return View(customers);
    }

    [HttpGet]
    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCustomerViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // Generate a random temporary password
        var tempPassword = GenerateTempPassword();

        var user = new AppUser
        {
            UserName = model.UserName,
            Email = model.Email,
            IsFirstLogin = true,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, tempPassword);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }

        var customer = new Customer
        {
            UserId = user.Id,
            DisplayName = model.DisplayName,
            InvoiceFolder = model.InvoiceFolder,
            OutputFolder = model.OutputFolder
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        // Send credentials by email (non-blocking — failure is logged, not thrown)
        try
        {
            await emailService.SendTempPasswordAsync(model.Email, model.UserName, tempPassword);
            TempData["Success"] = $"הלקוח '{model.DisplayName}' נוצר בהצלחה. פרטי הכניסה נשלחו ל-{model.Email}.";
        }
        catch (Exception ex)
        {
            TempData["Warning"] = $"הלקוח נוצר, אך שליחת האימייל נכשלה: {ex.Message}. הסיסמה הזמנית: {tempPassword}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await db.Customers.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id);
        if (customer is null) return NotFound();

        return View(new EditCustomerViewModel
        {
            CustomerId = customer.Id,
            DisplayName = customer.DisplayName,
            Email = customer.User.Email ?? "",
            InvoiceFolder = customer.InvoiceFolder,
            OutputFolder = customer.OutputFolder
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditCustomerViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var customer = await db.Customers.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == model.CustomerId);
        if (customer is null) return NotFound();

        customer.DisplayName = model.DisplayName;
        customer.InvoiceFolder = model.InvoiceFolder;
        customer.OutputFolder = model.OutputFolder;

        var emailResult = await userManager.SetEmailAsync(customer.User, model.Email);
        if (!emailResult.Succeeded)
        {
            foreach (var e in emailResult.Errors)
                ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }

        await db.SaveChangesAsync();
        TempData["Success"] = "פרטי הלקוח עודכנו בהצלחה.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var customer = await db.Customers.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id);
        if (customer is null) return NotFound();

        var tempPassword = GenerateTempPassword();
        var token = await userManager.GeneratePasswordResetTokenAsync(customer.User);
        var result = await userManager.ResetPasswordAsync(customer.User, token, tempPassword);
        if (!result.Succeeded)
        {
            TempData["Error"] = "איפוס הסיסמה נכשל.";
            return RedirectToAction(nameof(Index));
        }

        customer.User.IsFirstLogin = true;
        await userManager.UpdateAsync(customer.User);

        try
        {
            await emailService.SendTempPasswordAsync(
                customer.User.Email!, customer.User.UserName!, tempPassword);
            TempData["Success"] = $"הסיסמה אופסה ונשלחה ל-{customer.User.Email}.";
        }
        catch
        {
            TempData["Warning"] = $"הסיסמה אופסה, אך שליחת האימייל נכשלה. הסיסמה הזמנית: {tempPassword}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await db.Customers.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id);
        if (customer is null) return NotFound();

        db.Customers.Remove(customer);
        await db.SaveChangesAsync();
        await userManager.DeleteAsync(customer.User);

        TempData["Success"] = $"הלקוח '{customer.DisplayName}' נמחק.";
        return RedirectToAction(nameof(Index));
    }

    private static string GenerateTempPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#";
        var random = new Random();
        return new string(Enumerable.Range(0, 12).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
}
