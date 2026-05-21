# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

ASP.NET Core 10 MVC web application — multi-tenant invoice portal. All source lives in `Web/`. The solution file (`onePdfFile.slnx`) contains only this one project.

## Commands

```powershell
cd Web
dotnet run                        # start the web server
dotnet ef migrations add <Name>   # add an EF Core migration
dotnet ef database update         # apply migrations manually (also runs on startup)
dotnet publish -c Release         # publish for deployment
```

There is no test project. Manual testing uses a browser against the running dev server. Sample PDFs are in `doc/april/`.

## Architecture (`Web/`)

### Key files

| File/Folder | Role |
|---|---|
| `Program.cs` | DI wiring, Identity config, cookie auth, EF migration-on-startup, Admin seed |
| `Data/AppDbContext.cs` | EF Core context — Identity tables + `Customers` |
| `Models/AppUser.cs` | `IdentityUser` + `IsFirstLogin bool` flag |
| `Models/Customer.cs` | Per-customer config: `InvoiceFolder`, `OutputFolder` (server paths) |
| `Models/ViewModels.cs` | All view models in one file |
| `Services/PdfMergeService.cs` | PDF/image merge logic |
| `Services/FileStorageService.cs` | Resolves monthly folders, saves uploads, path-traversal guards |
| `Services/EmailService.cs` | SMTP delivery of temporary passwords |
| `Controllers/AccountController.cs` | Login, forced-first-login password change, logout |
| `Controllers/AdminController.cs` | `[Authorize(Roles="Admin")]` — CRUD customers, reset passwords |
| `Controllers/InvoicesController.cs` | Upload files, list by month, merge → output folder, download |

### Auth & roles

- **Admin** role seeded from `appsettings.json` → `AdminSeed` section on first run.
- Cookie auth — 8-hour sliding session; lockout after 5 bad attempts.
- `IsFirstLogin = true` → any login redirects to `Account/ChangePassword` before the user can do anything else.
- Only `[Authorize(Roles="Admin")]` users can access `AdminController`; all other authenticated users reach `InvoicesController`.

### File storage layout

```
{Customer.InvoiceFolder}/
└── {year}/{month:D2}/    ← uploaded invoices, created automatically
{Customer.OutputFolder}/  ← merged PDFs saved here
```

Both paths are absolute server paths configured per-customer by the admin. Path-traversal is prevented in `FileStorageService` and `InvoicesController.Download`.

### Configuration (`appsettings.json`)

```jsonc
"AdminSeed": { "UserName": "admin", "Password": "...", "Email": "..." }
"Email":     { "Host": "", "Port": 587, "EnableSsl": true, ... }  // leave Host empty to skip email in dev
"ConnectionStrings": { "DefaultConnection": "Data Source=invoices.db" }
```

### iText7 / Path ambiguity

iText7 ships `iText.Kernel.Geom.Path` which conflicts with `System.IO.Path` in files that import iText namespaces. Always alias: `using IOPath = System.IO.Path;` and use `IOPath.*`.

### Supported input formats

PDF, PNG, JPG/JPEG, TIF/TIFF, BMP, GIF, WebP, SVG
