using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Services
{
    public class CompanySettingsService : ICompanySettingsService
    {
        private readonly ApplicationDbContext _context;

        public CompanySettingsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CompanySettings> GetSettingsAsync()
        {
            var settings = await _context.CompanySettings.OrderBy(s => s.Id).FirstOrDefaultAsync();
            if (settings != null) return settings;

            settings = new CompanySettings
            {
                DisplayName = "MiniPOS",
                PrimaryColor = "#2563eb",
                CurrencyCode = "USD",
                InvoicePrefix = "INV",
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            _context.CompanySettings.Add(settings);
            await _context.SaveChangesAsync();
            return settings;
        }

        public async Task<CompanySettings> SaveSettingsAsync(CompanySettings settings, string? updatedBy = null)
        {
            var existing = await _context.CompanySettings.OrderBy(s => s.Id).FirstOrDefaultAsync();
            if (existing == null)
            {
                settings.CreatedDate = DateTime.Now;
                settings.UpdatedDate = DateTime.Now;
                settings.UpdatedBy = updatedBy;
                _context.CompanySettings.Add(settings);
                await _context.SaveChangesAsync();
                return settings;
            }

            existing.DisplayName = settings.DisplayName;
            existing.LegalName = settings.LegalName;
            existing.TaxNumber = settings.TaxNumber;
            existing.Address = settings.Address;
            existing.City = settings.City;
            existing.Country = settings.Country;
            existing.Phone = settings.Phone;
            existing.Email = settings.Email;
            existing.LogoPath = settings.LogoPath;
            existing.PrimaryColor = settings.PrimaryColor;
            existing.CurrencyCode = settings.CurrencyCode;
            existing.DefaultTaxRate = settings.DefaultTaxRate;
            existing.InvoicePrefix = settings.InvoicePrefix;
            existing.InvoiceFooterNote = settings.InvoiceFooterNote;
            existing.ReceiptFooterNote = settings.ReceiptFooterNote;
            existing.SupplierInvoiceFooterNote = settings.SupplierInvoiceFooterNote;
            existing.UpdatedDate = DateTime.Now;
            existing.UpdatedBy = updatedBy;

            await _context.SaveChangesAsync();
            return existing;
        }
    }
}
