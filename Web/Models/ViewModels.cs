using System.ComponentModel.DataAnnotations;

namespace onePdfFile.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "שם משתמש נדרש")]
    [Display(Name = "שם משתמש")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "סיסמה נדרשת")]
    [DataType(DataType.Password)]
    [Display(Name = "סיסמה")]
    public string Password { get; set; } = string.Empty;
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "סיסמה נוכחית נדרשת")]
    [DataType(DataType.Password)]
    [Display(Name = "סיסמה נוכחית")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "סיסמה חדשה נדרשת")]
    [MinLength(8, ErrorMessage = "הסיסמה חייבת להכיל לפחות 8 תווים")]
    [DataType(DataType.Password)]
    [Display(Name = "סיסמה חדשה")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "אישור סיסמה נדרש")]
    [DataType(DataType.Password)]
    [Display(Name = "אישור סיסמה")]
    [Compare(nameof(NewPassword), ErrorMessage = "הסיסמאות אינן תואמות")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class CreateCustomerViewModel
{
    [Required(ErrorMessage = "שם לקוח נדרש")]
    [Display(Name = "שם לקוח")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "שם משתמש נדרש")]
    [Display(Name = "שם משתמש")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "כתובת אימייל נדרשת")]
    [EmailAddress(ErrorMessage = "כתובת אימייל לא תקינה")]
    [Display(Name = "אימייל")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "תיקיית חשבוניות נדרשת")]
    [Display(Name = "תיקיית חשבוניות (נתיב בשרת)")]
    public string InvoiceFolder { get; set; } = string.Empty;

    [Required(ErrorMessage = "תיקיית פלט נדרשת")]
    [Display(Name = "תיקיית פלט (נתיב בשרת)")]
    public string OutputFolder { get; set; } = string.Empty;
}

public class EditCustomerViewModel
{
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "שם לקוח נדרש")]
    [Display(Name = "שם לקוח")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "כתובת אימייל נדרשת")]
    [EmailAddress(ErrorMessage = "כתובת אימייל לא תקינה")]
    [Display(Name = "אימייל")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "תיקיית חשבוניות נדרשת")]
    [Display(Name = "תיקיית חשבוניות (נתיב בשרת)")]
    public string InvoiceFolder { get; set; } = string.Empty;

    [Required(ErrorMessage = "תיקיית פלט נדרשת")]
    [Display(Name = "תיקיית פלט (נתיב בשרת)")]
    public string OutputFolder { get; set; } = string.Empty;
}

public class CustomerListItem
{
    public int CustomerId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsFirstLogin { get; set; }
}

public class InvoiceFile
{
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime LastModified { get; set; }
    public string Extension => Path.GetExtension(FileName).TrimStart('.').ToUpperInvariant();
}

public class InvoicesViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public List<InvoiceFile> Files { get; set; } = [];
    public bool OutputExists { get; set; }
    public string OutputFileName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
}

public class MergeRequestViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }

    [Required(ErrorMessage = "שם קובץ נדרש")]
    [Display(Name = "שם קובץ הפלט (ללא סיומת)")]
    public string OutputFileName { get; set; } = string.Empty;

    public List<string> SelectedFiles { get; set; } = [];
}
