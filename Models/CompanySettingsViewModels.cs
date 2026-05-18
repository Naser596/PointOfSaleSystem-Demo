using System.ComponentModel.DataAnnotations;

namespace WebApplication3.Models
{
    public class CompanySettingsViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Company Display Name")]
        public string DisplayName { get; set; } = string.Empty;

        [Display(Name = "Legal Name")]
        public string? LegalName { get; set; }

        [Display(Name = "Tax / VAT Number")]
        public string? TaxNumber { get; set; }

        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Phone { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string? CurrentLogoPath { get; set; }

        [Display(Name = "Company Logo")]
        public IFormFile? LogoFile { get; set; }

        [Display(Name = "Primary Color")]
        [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Use a hex color like #2563eb.")]
        public string PrimaryColor { get; set; } = "#2563eb";

        [Display(Name = "Currency")]
        [StringLength(10)]
        public string CurrencyCode { get; set; } = "USD";

        [Display(Name = "Default Tax Rate %")]
        [Range(0, 100)]
        public decimal DefaultTaxRate { get; set; }

        [Required]
        [Display(Name = "Invoice Prefix")]
        public string InvoicePrefix { get; set; } = "INV";

        [Display(Name = "Invoice Footer Note")]
        public string? InvoiceFooterNote { get; set; }

        [Display(Name = "Receipt Footer Note")]
        public string? ReceiptFooterNote { get; set; }

        [Display(Name = "Supplier Invoice Footer Note")]
        public string? SupplierInvoiceFooterNote { get; set; }
    }
}
