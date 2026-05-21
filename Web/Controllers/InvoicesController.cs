using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using onePdfFile.Web.Data;
using onePdfFile.Web.Models;
using onePdfFile.Web.Services;

namespace onePdfFile.Web.Controllers;

[Authorize]
public class InvoicesController(
    AppDbContext db,
    UserManager<AppUser> userManager,
    FileStorageService storage,
    PdfMergeService merger) : Controller
{
    // ── Index ──────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(int? year = null, int? month = null)
    {
        var customer = await GetCustomerAsync();
        if (customer is null) return Problem("לא נמצא פרופיל לקוח.");

        var now = DateTime.Today;
        int y = year ?? now.Year;
        int m = month ?? now.Month;

        var files = storage.GetMonthFiles(customer, y, m)
            .Select(path => new InvoiceFile
            {
                FileName = Path.GetFileName(path),
                FullPath = path,
                SizeBytes = new FileInfo(path).Length,
                LastModified = System.IO.File.GetLastWriteTime(path)
            })
            .ToList();

        var hebrewMonthName = new CultureInfo("he-IL").DateTimeFormat.GetMonthName(m);

        return View(new InvoicesViewModel
        {
            Year = y,
            Month = m,
            MonthName = hebrewMonthName,
            Files = files,
            CustomerName = customer.DisplayName
        });
    }

    // ── Upload ─────────────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(List<IFormFile> files, int year, int month)
    {
        var customer = await GetCustomerAsync();
        if (customer is null) return Problem("לא נמצא פרופיל לקוח.");

        if (files.Count == 0)
        {
            TempData["Error"] = "לא נבחרו קבצים.";
            return RedirectToAction(nameof(Index), new { year, month });
        }

        var errors = new List<string>();
        var uploaded = 0;
        foreach (var file in files)
        {
            try
            {
                await storage.SaveUploadAsync(customer, year, month, file);
                uploaded++;
            }
            catch (Exception ex)
            {
                errors.Add($"{file.FileName}: {ex.Message}");
            }
        }

        if (errors.Count > 0)
            TempData["Warning"] = $"הועלו {uploaded} קבצים. שגיאות: {string.Join("; ", errors)}";
        else
            TempData["Success"] = $"הועלו בהצלחה {uploaded} קבצים.";

        return RedirectToAction(nameof(Index), new { year, month });
    }

    // ── Delete a single file ───────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFile(string fileName, int year, int month)
    {
        var customer = await GetCustomerAsync();
        if (customer is null) return Problem("לא נמצא פרופיל לקוח.");

        try
        {
            storage.DeleteFile(customer, year, month, fileName);
            TempData["Success"] = $"הקובץ '{fileName}' נמחק.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"מחיקה נכשלה: {ex.Message}";
        }

        return RedirectToAction(nameof(Index), new { year, month });
    }

    // ── Merge ──────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Merge(int year, int month)
    {
        var customer = await GetCustomerAsync();
        if (customer is null) return Problem("לא נמצא פרופיל לקוח.");

        var files = storage.GetMonthFiles(customer, year, month);
        if (files.Count == 0)
        {
            TempData["Error"] = "אין קבצים לחודש זה לביצוע מיזוג.";
            return RedirectToAction(nameof(Index), new { year, month });
        }

        var hebrewMonth = new CultureInfo("he-IL").DateTimeFormat.GetMonthName(month);
        var model = new MergeRequestViewModel
        {
            Year = year,
            Month = month,
            OutputFileName = $"{customer.DisplayName}_{hebrewMonth}_{year}",
            SelectedFiles = files.Select(Path.GetFileName).ToList()!
        };
        ViewBag.AllFiles = files.Select(Path.GetFileName).ToList();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Merge(MergeRequestViewModel model)
    {
        var customer = await GetCustomerAsync();
        if (customer is null) return Problem("לא נמצא פרופיל לקוח.");

        if (model.SelectedFiles.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "יש לבחור לפחות קובץ אחד למיזוג.");
        }

        if (model.OutputFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            ModelState.AddModelError(nameof(model.OutputFileName), "שם הקובץ מכיל תווים לא חוקיים.");
        }

        if (!ModelState.IsValid)
        {
            var allFiles = storage.GetMonthFiles(customer, model.Year, model.Month)
                .Select(Path.GetFileName).ToList();
            ViewBag.AllFiles = allFiles;
            return View(model);
        }

        // Resolve full paths from the month folder
        var monthFolder = storage.GetMonthFolder(customer, model.Year, model.Month);
        var inputPaths = model.SelectedFiles
            .Select(name => Path.Combine(monthFolder, name))
            .Where(System.IO.File.Exists)
            .ToList();

        var outputPath = storage.GetMergedOutputPath(customer, model.OutputFileName);

        try
        {
            merger.Merge(inputPaths, outputPath);
            TempData["Success"] = $"קובץ '{Path.GetFileName(outputPath)}' נשמר בתיקיית הפלט.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"המיזוג נכשל: {ex.Message}";
        }

        return RedirectToAction(nameof(Index), new { model.Year, model.Month });
    }

    // ── Download merged output ─────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Download(string fileName)
    {
        var customer = await GetCustomerAsync();
        if (customer is null) return Problem("לא נמצא פרופיל לקוח.");

        var safeName = Path.GetFileName(fileName);
        var path = Path.Combine(customer.OutputFolder, safeName);

        if (!System.IO.File.Exists(path))
            return NotFound("הקובץ לא נמצא בתיקיית הפלט.");

        // Prevent path traversal
        if (!Path.GetFullPath(path).StartsWith(
                Path.GetFullPath(customer.OutputFolder), StringComparison.OrdinalIgnoreCase))
            return BadRequest();

        var bytes = await System.IO.File.ReadAllBytesAsync(path);
        return new FileContentResult(bytes, "application/pdf") { FileDownloadName = safeName };
    }

    // ── Helper ─────────────────────────────────────────────────────────

    private async Task<Customer?> GetCustomerAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return null;

        // Admins browsing the invoice section: not applicable — admins use Admin panel
        return await db.Customers
            .FirstOrDefaultAsync(c => c.UserId == user.Id);
    }
}
