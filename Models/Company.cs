using System.ComponentModel.DataAnnotations;

namespace WebApplication3.Models
{
    public class Company
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? LegalName { get; set; }

        [MaxLength(80)]
        public string? TaxNumber { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        [MaxLength(40)]
        public string? Phone { get; set; }

        [MaxLength(256)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? LogoPath { get; set; }

        [MaxLength(20)]
        public string PrimaryColor { get; set; } = "#2563eb";

        [MaxLength(10)]
        public string CurrencyCode { get; set; } = "USD";

        public decimal DefaultTaxRate { get; set; }

        [MaxLength(20)]
        public string InvoicePrefix { get; set; } = "INV";

        [MaxLength(1000)]
        public string? InvoiceFooterNote { get; set; }

        [MaxLength(1000)]
        public string? ReceiptFooterNote { get; set; }

        [MaxLength(1000)]
        public string? SupplierInvoiceFooterNote { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime? PlatformAccessStartDate { get; set; }
        public DateTime? PlatformAccessEndDate { get; set; }
        public int AutoDisableGraceDays { get; set; } = 3;
        public DateTime? PlatformDisabledDate { get; set; }

        [MaxLength(500)]
        public string? PlatformDisabledReason { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }

        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();

        public bool HasPlatformAccessPeriod => PlatformAccessStartDate.HasValue && PlatformAccessEndDate.HasValue;

        public bool IsPlatformExpired => PlatformAccessEndDate.HasValue && DateTime.Today > PlatformAccessEndDate.Value.Date;

        public bool IsInPlatformGracePeriod =>
            PlatformAccessEndDate.HasValue &&
            DateTime.Today > PlatformAccessEndDate.Value.Date &&
            DateTime.Today <= PlatformAccessEndDate.Value.Date.AddDays(AutoDisableGraceDays);

        public bool ShouldAutoDisableForExpiredAccess =>
            IsActive &&
            PlatformAccessEndDate.HasValue &&
            DateTime.Today > PlatformAccessEndDate.Value.Date.AddDays(AutoDisableGraceDays);

        public int? DaysUntilPlatformExpiry =>
            PlatformAccessEndDate.HasValue
                ? (PlatformAccessEndDate.Value.Date - DateTime.Today).Days
                : null;
    }
}
