using System.Net;
using System.Net.Mail;

namespace onePdfFile.Web.Services;

public class EmailSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "מערכת חשבוניות";
}

public class EmailService(EmailSettings settings)
{
    public async Task SendTempPasswordAsync(string toEmail, string userName, string tempPassword)
    {
        var subject = "פרטי כניסה למערכת חשבוניות";
        var body = $"""
            שלום,

            חשבונך במערכת החשבוניות נוצר בהצלחה.

            שם משתמש: {userName}
            סיסמה זמנית: {tempPassword}

            בכניסה הראשונה תתבקש/י לשנות את הסיסמה.

            בברכה,
            {settings.FromName}
            """;

        await SendAsync(toEmail, subject, body);
    }

    private async Task SendAsync(string to, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(settings.Host))
            return; // SMTP not configured — skip silently (useful in dev)

        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.EnableSsl,
            Credentials = new NetworkCredential(settings.UserName, settings.Password)
        };
        var message = new MailMessage(
            new MailAddress(settings.FromAddress, settings.FromName),
            new MailAddress(to))
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        await client.SendMailAsync(message);
    }
}
