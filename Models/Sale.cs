using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebApplication3.Models
{
    public class Sale
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        [ValidateNever]
        public Company Company { get; set; } = null!;
        public string SaleNumber { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RefundedAmount { get; set; }
        public DateTime? ReturnedDate { get; set; }
        public string? ReturnedBy { get; set; }
        public string? ReturnReason { get; set; }
        public string Status { get; set; } = "Completed";
        public string PaymentMethod { get; set; } = "Cash";
        
        // Cashier tracking
        public string? CashierId { get; set; }
        public string? CashierName { get; set; }
        
        // Customer (optional)
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }
        
        // Discount applied (optional)
        public int? DiscountId { get; set; }
        public string? DiscountCode { get; set; }
        
        // Navigation property
        public List<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    }
}
