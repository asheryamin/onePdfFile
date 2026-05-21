using onePdfFile.Web.Models;

namespace onePdfFile.Web.Services;

public class FileStorageService
{
    private static readonly HashSet<string> AllowedExtensions =
        [".pdf", ".png", ".jpg", ".jpeg", ".tif", ".tiff", ".bmp", ".gif", ".webp", ".svg"];

    // Returns the monthly invoice folder, creating it if it doesn't exist.
    public string GetMonthFolder(Customer customer, int year, int month)
    {
        var path = Path.Combine(customer.InvoiceFolder, year.ToString(), month.ToString("D2"));
        Directory.CreateDirectory(path);
        return path;
    }

    public async Task<string> SaveUploadAsync(Customer customer, int year, int month, IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException($"סוג קובץ לא נתמך: {ext}");

        var folder = GetMonthFolder(customer, year, month);
        // Sanitize the filename — strip directory components the browser might send
        var safeName = Path.GetFileName(file.FileName);
        var dest = Path.Combine(folder, safeName);

        // Avoid collisions: append a counter when the name already exists
        if (File.Exists(dest))
        {
            var nameNoExt = Path.GetFileNameWithoutExtension(safeName);
            var counter = 1;
            while (File.Exists(dest))
            {
                dest = Path.Combine(folder, $"{nameNoExt}_{counter++}{ext}");
            }
        }

        await using var stream = File.Create(dest);
        await file.CopyToAsync(stream);
        return dest;
    }

    public List<string> GetMonthFiles(Customer customer, int year, int month)
    {
        var folder = GetMonthFolder(customer, year, month);
        return Directory.GetFiles(folder)
            .Where(f => AllowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public void DeleteFile(Customer customer, int year, int month, string fileName)
    {
        var folder = GetMonthFolder(customer, year, month);
        // Prevent path traversal — only delete files that live directly in the month folder
        var target = Path.GetFullPath(Path.Combine(folder, Path.GetFileName(fileName)));
        if (!target.StartsWith(Path.GetFullPath(folder), StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("נתיב לא חוקי");
        if (File.Exists(target))
            File.Delete(target);
    }

    public string GetMergedOutputPath(Customer customer, string outputFileName)
    {
        Directory.CreateDirectory(customer.OutputFolder);
        var safeName = Path.GetFileName(outputFileName);
        if (!safeName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            safeName += ".pdf";
        return Path.Combine(customer.OutputFolder, safeName);
    }
}
