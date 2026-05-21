using System.ComponentModel.DataAnnotations;

namespace onePdfFile.Web.Models;

public class Customer
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    [Required, Display(Name = "שם לקוח")]
    public string DisplayName { get; set; } = string.Empty;

    // Server-side folder where uploaded invoices are stored (monthly sub-folders created automatically)
    [Required, Display(Name = "תיקיית חשבוניות")]
    public string InvoiceFolder { get; set; } = string.Empty;

    // Customer-agreed output folder where merged PDFs are saved
    [Required, Display(Name = "תיקיית פלט")]
    public string OutputFolder { get; set; } = string.Empty;
}
