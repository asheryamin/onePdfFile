# Invoice Portal — מערכת חשבוניות

Multi-tenant web application for managing, uploading, and merging monthly invoices into a single PDF. Built with ASP.NET Core 10 and a Hebrew RTL interface.

## Features

- **Customer portal** — each customer logs in to their own account, uploads invoices for the current month, and merges them into a single PDF saved to an agreed output folder
- **Admin panel** — create and manage customer accounts, configure per-customer storage folders, reset passwords
- **First-login flow** — new customers receive a temporary password by email and are forced to change it on first login
- **PDF merge** — combines PDF and image files (PNG, JPG, TIF, BMP, GIF, WebP, SVG) into a single A4 PDF with aspect-ratio-preserving centering
- **Hebrew RTL UI** — full right-to-left interface built with Bootstrap 5 RTL

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 10 MVC |
| Auth | ASP.NET Core Identity (cookie auth, lockout, role-based) |
| Database | Entity Framework Core + SQLite |
| PDF processing | iText7 9.6 |
| SVG rasterization | Svg 3.4 |
| UI | Bootstrap 5 RTL, Hebrew (`he-IL`) |

## Getting Started

### Prerequisites

- .NET 10 SDK
- Windows (required for `System.Drawing` used in WebP/SVG processing)

### Run locally

```powershell
cd Web
dotnet run
```

The app starts on `http://localhost:5000`. On first run it:
1. Creates the SQLite database and applies migrations automatically
2. Seeds an Admin role and the default admin user

### Default admin credentials

| Field | Value |
|---|---|
| Username | `admin` |
| Password | `Admin1234!` |

> Change these in `Web/appsettings.json` → `AdminSeed` before deploying.

### Configuration

All settings live in `Web/appsettings.json`:

```jsonc
{
  "AdminSeed": {
    "UserName": "admin",
    "Password": "Admin1234!",
    "Email": "admin@example.com"
  },
  "Email": {
    "Host": "",          // leave empty to skip email in development
    "Port": 587,
    "EnableSsl": true,
    "UserName": "",
    "Password": "",
    "FromAddress": "",
    "FromName": "מערכת חשבוניות"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=invoices.db"
  }
}
```

### Adding a customer (admin flow)

1. Log in as admin → **ניהול לקוחות** → **לקוח חדש**
2. Fill in the customer's name, username, email, and two server-side folder paths:
   - **Invoice folder** — where uploaded files are stored (monthly sub-folders are created automatically)
   - **Output folder** — where merged PDFs are saved
3. The system creates the account and emails a temporary password to the customer

### Customer flow

1. Customer receives credentials by email and logs in
2. On first login, forced to set a new password
3. Uploads invoices for the current month (PDF, PNG, JPG, TIF, BMP, GIF, WebP, SVG)
4. Selects files and merges them into a single PDF → saved to their output folder

## Project Structure

```
Web/
├── Controllers/        # AccountController, AdminController, InvoicesController
├── Data/               # EF Core DbContext
├── Migrations/         # EF Core migrations
├── Models/             # AppUser, Customer, ViewModels
├── Services/           # PdfMergeService, FileStorageService, EmailService
└── Views/              # Razor views (Hebrew RTL)
```

## License

MIT
