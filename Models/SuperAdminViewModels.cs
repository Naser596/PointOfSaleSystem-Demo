using System.ComponentModel.DataAnnotations;

namespace WebApplication3.Models
{
    public class CompanyCreateViewModel
    {
        [Required]
        [Display(Name = "Company Name")]
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
        [Display(Name = "Platform Access From")]
        [DataType(DataType.Date)]
        public DateTime PlatformAccessStartDate { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Platform Access Until")]
        [DataType(DataType.Date)]
        public DateTime PlatformAccessEndDate { get; set; } = DateTime.Today.AddMonths(1);

        [Display(Name = "Auto Disable Grace Days")]
        [Range(0, 30)]
        public int AutoDisableGraceDays { get; set; } = 3;

        [Required]
        [Display(Name = "Admin Email")]
        [EmailAddress]
        public string AdminEmail { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        [Display(Name = "Admin Password")]
        public string AdminPassword { get; set; } = string.Empty;
    }

    public class CompanyEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Company Name")]
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

        [Display(Name = "Change Logo")]
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

        [Display(Name = "Platform Access From")]
        [DataType(DataType.Date)]
        public DateTime? PlatformAccessStartDate { get; set; }

        [Display(Name = "Platform Access Until")]
        [DataType(DataType.Date)]
        public DateTime? PlatformAccessEndDate { get; set; }

        [Display(Name = "Auto Disable Grace Days")]
        [Range(0, 30)]
        public int AutoDisableGraceDays { get; set; } = 3;

        public DateTime? PlatformDisabledDate { get; set; }
        public string? PlatformDisabledReason { get; set; }

        [Required]
        [Display(Name = "Invoice Prefix")]
        public string InvoicePrefix { get; set; } = "INV";

        [Display(Name = "Invoice Footer Note")]
        public string? InvoiceFooterNote { get; set; }

        [Display(Name = "Receipt Footer Note")]
        public string? ReceiptFooterNote { get; set; }

        [Display(Name = "Supplier Invoice Footer Note")]
        public string? SupplierInvoiceFooterNote { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class CompanySubscriptionAlertViewModel
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public DateTime? AccessEndDate { get; set; }
        public int? DaysUntilExpiry { get; set; }
        public int GraceDays { get; set; }
        public bool IsExpired { get; set; }
        public bool IsInGracePeriod { get; set; }
        public bool ShouldDisable { get; set; }
        public decimal SeverityOrder => ShouldDisable ? 0 : IsExpired ? 1 : 2;
    }

    public class CompanyHealthViewModel
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public int SalesLast30Days { get; set; }
        public int FailedSyncCount { get; set; }
        public DateTime? LastSaleDate { get; set; }
        public DateTime? LastAuditDate { get; set; }
        public bool IsActive { get; set; }
        public int HealthScore { get; set; }
        public string HealthStatus { get; set; } = "Good";

        public string HealthCssClass => HealthStatus switch
        {
            "At Risk" => "danger",
            "Watch" => "warning",
            _ => "success"
        };
    }
}
