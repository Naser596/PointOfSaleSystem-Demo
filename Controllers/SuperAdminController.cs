using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using WebApplication3.Data;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{
    [Authorize(Policy = "PlatformOwner")]
    public class SuperAdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ICompanySubscriptionService _subscriptionService;
        private readonly IAuditLogService _auditLog;

        public SuperAdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment,
            ICompanySubscriptionService subscriptionService,
            IAuditLogService auditLog)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _subscriptionService = subscriptionService;
            _auditLog = auditLog;
        }

        public async Task<IActionResult> Index()
        {
            var autoDisabledCount = await _subscriptionService.DisableExpiredCompaniesAsync();
            ViewBag.CompanyCount = await _context.Companies.CountAsync();
            ViewBag.ActiveCompanyCount = await _context.Companies.CountAsync(c => c.IsActive);
            ViewBag.UserCount = await _context.Users.CountAsync(u => u.CompanyId != null);
            ViewBag.PlatformSales = await _context.Sales.CountAsync();
            ViewBag.AutoDisabledCount = autoDisabledCount;
            ViewBag.SubscriptionAlerts = await _subscriptionService.GetSubscriptionAlertsAsync();

            var companies = await _context.Companies
                .Include(c => c.Users)
                .OrderBy(c => c.PlatformAccessEndDate ?? DateTime.MaxValue)
                .ThenBy(c => c.DisplayName)
                .ToListAsync();
            var health = await BuildCompanyHealthAsync(companies);
            ViewBag.CompanyHealth = health;
            ViewBag.AtRiskCompanyCount = health.Count(h => h.HealthStatus == "At Risk");

            return View(companies);
        }

        public IActionResult CreateCompany()
        {
            return View(new CompanyCreateViewModel
            {
                PlatformAccessStartDate = DateTime.Today,
                PlatformAccessEndDate = DateTime.Today.AddMonths(1),
                AutoDisableGraceDays = 3
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCompany(CompanyCreateViewModel model)
        {
            if (model.PlatformAccessEndDate.Date < model.PlatformAccessStartDate.Date)
            {
                ModelState.AddModelError(nameof(model.PlatformAccessEndDate), "Platform access end date must be after the start date.");
            }

            if (!ModelState.IsValid) return View(model);

            if (await _userManager.FindByEmailAsync(model.AdminEmail) != null)
            {
                ModelState.AddModelError(nameof(model.AdminEmail), "A user with this email already exists.");
                return View(model);
            }

            var logoPath = await SaveLogoAsync(model.LogoFile);
            var company = new Company
            {
                DisplayName = model.DisplayName,
                LegalName = model.LegalName,
                TaxNumber = model.TaxNumber,
                Address = model.Address,
                City = model.City,
                Country = model.Country,
                Phone = model.Phone,
                Email = model.Email,
                LogoPath = logoPath,
                PrimaryColor = model.PrimaryColor,
                CurrencyCode = model.CurrencyCode,
                DefaultTaxRate = model.DefaultTaxRate,
                InvoicePrefix = "INV",
                PlatformAccessStartDate = model.PlatformAccessStartDate.Date,
                PlatformAccessEndDate = model.PlatformAccessEndDate.Date,
                AutoDisableGraceDays = model.AutoDisableGraceDays,
                IsActive = true,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            var admin = new ApplicationUser
            {
                UserName = model.AdminEmail,
                Email = model.AdminEmail,
                EmailConfirmed = true,
                CompanyId = company.Id
            };

            var result = await _userManager.CreateAsync(admin, model.AdminPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }

            await _userManager.AddToRoleAsync(admin, "Admin");
            TempData["Success"] = "Company and company admin created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> EditCompany(int id)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null) return NotFound();

            return View(new CompanyEditViewModel
            {
                Id = company.Id,
                DisplayName = company.DisplayName,
                LegalName = company.LegalName,
                TaxNumber = company.TaxNumber,
                Address = company.Address,
                City = company.City,
                Country = company.Country,
                Phone = company.Phone,
                Email = company.Email,
                CurrentLogoPath = company.LogoPath,
                PrimaryColor = company.PrimaryColor,
                CurrencyCode = company.CurrencyCode,
                DefaultTaxRate = company.DefaultTaxRate,
                PlatformAccessStartDate = company.PlatformAccessStartDate,
                PlatformAccessEndDate = company.PlatformAccessEndDate,
                AutoDisableGraceDays = company.AutoDisableGraceDays,
                PlatformDisabledDate = company.PlatformDisabledDate,
                PlatformDisabledReason = company.PlatformDisabledReason,
                InvoicePrefix = company.InvoicePrefix,
                InvoiceFooterNote = company.InvoiceFooterNote,
                ReceiptFooterNote = company.ReceiptFooterNote,
                SupplierInvoiceFooterNote = company.SupplierInvoiceFooterNote,
                IsActive = company.IsActive
            });
        }

        public async Task<IActionResult> ExportCompany(int id)
        {
            var company = await _context.Companies
                .Include(c => c.Users)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (company == null) return NotFound();

            var products = await _context.Products
                .IgnoreQueryFilters()
                .Where(p => p.CompanyId == id)
                .OrderBy(p => p.Name)
                .ToListAsync();
            var customers = await _context.Customers
                .IgnoreQueryFilters()
                .Where(c => c.CompanyId == id)
                .OrderBy(c => c.Name)
                .ToListAsync();
            var sales = await _context.Sales
                .Where(s => s.CompanyId == id)
                .OrderByDescending(s => s.SaleDate)
                .Take(5000)
                .ToListAsync();
            var syncRecords = await _context.OfflineSyncRecords
                .Where(r => r.CompanyId == id)
                .OrderByDescending(r => r.ReceivedAt)
                .Take(1000)
                .ToListAsync();
            var auditLogs = await _context.AuditLogs
                .Where(a => a.CompanyId == id)
                .OrderByDescending(a => a.CreatedDate)
                .Take(1000)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var summary = workbook.Worksheets.Add("Summary");
            AddHeader(summary, "Metric", "Value");
            var summaryRows = new (string Label, object? Value)[]
            {
                ("Company", company.DisplayName),
                ("Legal Name", company.LegalName),
                ("Currency", company.CurrencyCode),
                ("Default Tax Rate", company.DefaultTaxRate),
                ("Active", company.IsActive ? "Yes" : "No"),
                ("Users", company.Users.Count),
                ("Products", products.Count),
                ("Customers", customers.Count),
                ("Sales Exported", sales.Count),
                ("Sync Records Exported", syncRecords.Count),
                ("Audit Rows Exported", auditLogs.Count),
                ("Exported At", DateTime.Now)
            };
            for (var i = 0; i < summaryRows.Length; i++)
            {
                summary.Cell(i + 2, 1).Value = summaryRows[i].Label;
                summary.Cell(i + 2, 2).Value = summaryRows[i].Value?.ToString();
            }

            var users = workbook.Worksheets.Add("Users");
            AddHeader(users, "Email", "User Name", "Company ID");
            var companyUsers = company.Users.OrderBy(u => u.Email).ToList();
            for (var i = 0; i < companyUsers.Count; i++)
            {
                users.Cell(i + 2, 1).Value = companyUsers[i].Email;
                users.Cell(i + 2, 2).Value = companyUsers[i].UserName;
                users.Cell(i + 2, 3).Value = companyUsers[i].CompanyId;
            }

            var productSheet = workbook.Worksheets.Add("Products");
            AddHeader(productSheet, "Name", "SKU", "Barcode", "Cost", "Price", "Tax", "Stock", "Active", "Deleted");
            for (var i = 0; i < products.Count; i++)
            {
                var product = products[i];
                productSheet.Cell(i + 2, 1).Value = product.Name;
                productSheet.Cell(i + 2, 2).Value = product.SKU;
                productSheet.Cell(i + 2, 3).Value = product.Barcode;
                productSheet.Cell(i + 2, 4).Value = product.CostPrice;
                productSheet.Cell(i + 2, 5).Value = product.Price;
                productSheet.Cell(i + 2, 6).Value = product.TaxRate;
                productSheet.Cell(i + 2, 7).Value = product.Stock;
                productSheet.Cell(i + 2, 8).Value = product.IsActive ? "Yes" : "No";
                productSheet.Cell(i + 2, 9).Value = product.IsDeleted ? "Yes" : "No";
            }

            var customerSheet = workbook.Worksheets.Add("Customers");
            AddHeader(customerSheet, "Name", "Email", "Phone", "Total Purchases", "Visits", "Updated", "Deleted");
            for (var i = 0; i < customers.Count; i++)
            {
                var customer = customers[i];
                customerSheet.Cell(i + 2, 1).Value = customer.Name;
                customerSheet.Cell(i + 2, 2).Value = customer.Email;
                customerSheet.Cell(i + 2, 3).Value = customer.Phone;
                customerSheet.Cell(i + 2, 4).Value = customer.TotalPurchases;
                customerSheet.Cell(i + 2, 5).Value = customer.VisitCount;
                customerSheet.Cell(i + 2, 6).Value = customer.UpdatedDate;
                customerSheet.Cell(i + 2, 7).Value = customer.IsDeleted ? "Yes" : "No";
            }

            var salesSheet = workbook.Worksheets.Add("Sales");
            AddHeader(salesSheet, "Sale Number", "Date", "Cashier", "Payment", "Status", "Subtotal", "Tax", "Discount", "Total", "Refunded");
            for (var i = 0; i < sales.Count; i++)
            {
                var sale = sales[i];
                salesSheet.Cell(i + 2, 1).Value = sale.SaleNumber;
                salesSheet.Cell(i + 2, 2).Value = sale.SaleDate;
                salesSheet.Cell(i + 2, 3).Value = sale.CashierName;
                salesSheet.Cell(i + 2, 4).Value = sale.PaymentMethod;
                salesSheet.Cell(i + 2, 5).Value = sale.Status;
                salesSheet.Cell(i + 2, 6).Value = sale.SubTotal;
                salesSheet.Cell(i + 2, 7).Value = sale.TaxAmount;
                salesSheet.Cell(i + 2, 8).Value = sale.DiscountAmount;
                salesSheet.Cell(i + 2, 9).Value = sale.TotalAmount;
                salesSheet.Cell(i + 2, 10).Value = sale.RefundedAmount;
            }

            var syncSheet = workbook.Worksheets.Add("Offline Sync");
            AddHeader(syncSheet, "Client ID", "Type", "Status", "Queued", "Received", "Processed", "Sale ID", "Error");
            for (var i = 0; i < syncRecords.Count; i++)
            {
                var record = syncRecords[i];
                syncSheet.Cell(i + 2, 1).Value = record.ClientId;
                syncSheet.Cell(i + 2, 2).Value = record.SyncType;
                syncSheet.Cell(i + 2, 3).Value = record.Status;
                syncSheet.Cell(i + 2, 4).Value = record.QueuedAt;
                syncSheet.Cell(i + 2, 5).Value = record.ReceivedAt;
                syncSheet.Cell(i + 2, 6).Value = record.ProcessedAt;
                syncSheet.Cell(i + 2, 7).Value = record.SaleId;
                syncSheet.Cell(i + 2, 8).Value = record.ErrorMessage;
            }

            var auditSheet = workbook.Worksheets.Add("Audit");
            AddHeader(auditSheet, "Date", "User", "Action", "Entity", "Entity ID", "Summary");
            for (var i = 0; i < auditLogs.Count; i++)
            {
                var log = auditLogs[i];
                auditSheet.Cell(i + 2, 1).Value = log.CreatedDate;
                auditSheet.Cell(i + 2, 2).Value = log.UserName;
                auditSheet.Cell(i + 2, 3).Value = log.Action;
                auditSheet.Cell(i + 2, 4).Value = log.EntityName;
                auditSheet.Cell(i + 2, 5).Value = log.EntityId;
                auditSheet.Cell(i + 2, 6).Value = log.Summary;
            }

            foreach (var worksheet in workbook.Worksheets) worksheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            await _auditLog.LogAsync("TenantExport", nameof(Company), company.Id.ToString(), $"Exported tenant data for {company.DisplayName}", company.Id);
            var fileName = $"tenant-export-{company.DisplayName.Replace(' ', '-')}-{DateTime.Today:yyyyMMdd}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCompany(int id, CompanyEditViewModel model)
        {
            if (id != model.Id) return NotFound();
            if (model.PlatformAccessStartDate.HasValue &&
                model.PlatformAccessEndDate.HasValue &&
                model.PlatformAccessEndDate.Value.Date < model.PlatformAccessStartDate.Value.Date)
            {
                ModelState.AddModelError(nameof(model.PlatformAccessEndDate), "Platform access end date must be after the start date.");
            }

            if (!ModelState.IsValid) return View(model);

            var company = await _context.Companies.FindAsync(id);
            if (company == null) return NotFound();

            company.DisplayName = model.DisplayName;
            company.LegalName = model.LegalName;
            company.TaxNumber = model.TaxNumber;
            company.Address = model.Address;
            company.City = model.City;
            company.Country = model.Country;
            company.Phone = model.Phone;
            company.Email = model.Email;
            company.PrimaryColor = model.PrimaryColor;
            company.CurrencyCode = model.CurrencyCode;
            company.DefaultTaxRate = model.DefaultTaxRate;
            company.PlatformAccessStartDate = model.PlatformAccessStartDate?.Date;
            company.PlatformAccessEndDate = model.PlatformAccessEndDate?.Date;
            company.AutoDisableGraceDays = Math.Max(model.AutoDisableGraceDays, 0);
            if (model.IsActive && !company.IsActive)
            {
                company.PlatformDisabledDate = null;
                company.PlatformDisabledReason = null;
            }
            company.InvoicePrefix = model.InvoicePrefix;
            company.InvoiceFooterNote = model.InvoiceFooterNote;
            company.ReceiptFooterNote = model.ReceiptFooterNote;
            company.SupplierInvoiceFooterNote = model.SupplierInvoiceFooterNote;
            company.IsActive = model.IsActive;
            company.UpdatedDate = DateTime.Now;

            var logoPath = await SaveLogoAsync(model.LogoFile);
            if (logoPath != null)
            {
                company.LogoPath = logoPath;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Company settings updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableCompany(int id)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null) return NotFound();

            company.IsActive = false;
            company.PlatformDisabledDate = DateTime.Now;
            company.PlatformDisabledReason = "Disabled manually from Super Admin subscription alert.";
            company.UpdatedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{company.DisplayName} disabled.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string?> SaveLogoAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Only JPG, JPEG, PNG, and WEBP logo files are allowed.");
            }

            if (file.Length > 2 * 1024 * 1024)
            {
                throw new InvalidOperationException("Logo size must be less than 2MB.");
            }

            var uploadDir = Path.Combine(_environment.WebRootPath, "images", "company");
            Directory.CreateDirectory(uploadDir);
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadDir, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/images/company/{fileName}";
        }

        private async Task<List<CompanyHealthViewModel>> BuildCompanyHealthAsync(List<Company> companies)
        {
            var since = DateTime.Today.AddDays(-30);
            var companyIds = companies.Select(c => c.Id).ToList();

            var sales = await _context.Sales
                .Where(s => companyIds.Contains(s.CompanyId))
                .GroupBy(s => s.CompanyId)
                .Select(g => new
                {
                    CompanyId = g.Key,
                    SalesLast30Days = g.Count(s => s.SaleDate >= since),
                    LastSaleDate = g.Max(s => (DateTime?)s.SaleDate)
                })
                .ToListAsync();
            var salesByCompany = sales.ToDictionary(s => s.CompanyId);

            var syncIssues = await _context.OfflineSyncRecords
                .Where(r => companyIds.Contains(r.CompanyId) && (r.Status == "Failed" || r.Status == "Conflict" || r.Status == "Pending"))
                .GroupBy(r => r.CompanyId)
                .Select(g => new { CompanyId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.CompanyId, g => g.Count);

            var lastAudits = await _context.AuditLogs
                .Where(a => a.CompanyId.HasValue && companyIds.Contains(a.CompanyId.Value))
                .GroupBy(a => a.CompanyId!.Value)
                .Select(g => new { CompanyId = g.Key, LastAuditDate = g.Max(a => (DateTime?)a.CreatedDate) })
                .ToDictionaryAsync(g => g.CompanyId, g => g.LastAuditDate);

            return companies.Select(company =>
            {
                salesByCompany.TryGetValue(company.Id, out var saleStats);
                syncIssues.TryGetValue(company.Id, out var issueCount);
                lastAudits.TryGetValue(company.Id, out var lastAuditDate);

                var score = 100;
                if (!company.IsActive) score -= 40;
                if (company.PlatformAccessEndDate.HasValue && company.PlatformAccessEndDate.Value.Date < DateTime.Today) score -= 25;
                if (issueCount > 0) score -= Math.Min(issueCount * 10, 30);
                if (saleStats?.LastSaleDate == null) score -= 10;
                else if (saleStats.LastSaleDate.Value.Date < since) score -= 10;

                score = Math.Clamp(score, 0, 100);
                var status = score < 55 ? "At Risk" : score < 80 ? "Watch" : "Good";

                return new CompanyHealthViewModel
                {
                    CompanyId = company.Id,
                    CompanyName = company.DisplayName,
                    UserCount = company.Users.Count,
                    SalesLast30Days = saleStats?.SalesLast30Days ?? 0,
                    FailedSyncCount = issueCount,
                    LastSaleDate = saleStats?.LastSaleDate,
                    LastAuditDate = lastAuditDate,
                    IsActive = company.IsActive,
                    HealthScore = score,
                    HealthStatus = status
                };
            }).OrderBy(h => h.HealthScore).ThenBy(h => h.CompanyName).ToList();
        }

        private static void AddHeader(IXLWorksheet sheet, params string[] headers)
        {
            for (var i = 0; i < headers.Length; i++)
            {
                sheet.Cell(1, i + 1).Value = headers[i];
            }

            sheet.Range(1, 1, 1, headers.Length).Style.Font.Bold = true;
        }
    }
}
