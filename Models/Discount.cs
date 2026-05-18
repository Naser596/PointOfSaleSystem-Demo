using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebApplication3.Models
{
    public class Discount
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        [ValidateNever]
        public Company Company { get; set; } = null!;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DiscountType DiscountType { get; set; }
        public decimal Value { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
    }

    public enum DiscountType
    {
        Percentage,
        FixedAmount
    }
}
