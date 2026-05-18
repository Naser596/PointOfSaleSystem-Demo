using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{
    [Authorize(Policy = "PlatformOwner")]
    public class SettingsController : Controller
    {
        private readonly ICompanySettingsService _settingsService;
        private readonly IWebHostEnvironment _environment;

        public SettingsController(ICompanySettingsService settingsService, IWebHostEnvironment environment)
        {
            _settingsService = settingsService;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _settingsService.GetSettingsAsync();
            return View(ToViewModel(settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CompanySettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existing = await _settingsService.GetSettingsAsync();
            var logoPath = existing.LogoPath;

            if (model.LogoFile != null)
            {
                var (path, error) = await ProcessLogoUpload(model.LogoFile);
                if (error != null)
                {
                    ModelState.AddModelError(nameof(model.LogoFile), error);
                    model.CurrentLogoPath = logoPath;
                    return View(model);
                }

                logoPath = path;
            }

            var settings = new CompanySettings
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
                InvoicePrefix = model.InvoicePrefix,
                InvoiceFooterNote = model.InvoiceFooterNote,
                ReceiptFooterNote = model.ReceiptFooterNote,
                SupplierInvoiceFooterNote = model.SupplierInvoiceFooterNote
            };

            await _settingsService.SaveSettingsAsync(settings, User.Identity?.Name);
            TempData["Success"] = "Company settings saved successfully.";
            return RedirectToAction(nameof(Index));
        }

        private static CompanySettingsViewModel ToViewModel(CompanySettings settings)
        {
            return new CompanySettingsViewModel
            {
                Id = settings.Id,
                DisplayName = settings.DisplayName,
                LegalName = settings.LegalName,
                TaxNumber = settings.TaxNumber,
                Address = settings.Address,
                City = settings.City,
                Country = settings.Country,
                Phone = settings.Phone,
                Email = settings.Email,
                CurrentLogoPath = settings.LogoPath,
                PrimaryColor = settings.PrimaryColor,
                CurrencyCode = settings.CurrencyCode,
                DefaultTaxRate = settings.DefaultTaxRate,
                InvoicePrefix = settings.InvoicePrefix,
                InvoiceFooterNote = settings.InvoiceFooterNote,
                ReceiptFooterNote = settings.ReceiptFooterNote,
                SupplierInvoiceFooterNote = settings.SupplierInvoiceFooterNote
            };
        }

        private async Task<(string? Path, string? Error)> ProcessLogoUpload(IFormFile file)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return (null, "Only JPG, JPEG, PNG, and WEBP logo files are allowed.");
            }

            if (file.Length > 2 * 1024 * 1024)
            {
                return (null, "Logo size must be less than 2MB.");
            }

            var fileName = $"{Guid.NewGuid()}{extension}";
            var uploadDir = Path.Combine(_environment.WebRootPath, "images", "company");
            Directory.CreateDirectory(uploadDir);

            var filePath = Path.Combine(uploadDir, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return ($"/images/company/{fileName}", null);
        }
    }
}
