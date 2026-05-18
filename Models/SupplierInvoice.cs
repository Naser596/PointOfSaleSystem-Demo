using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebApplication3.Models
{
    public class SupplierInvoice
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        [ValidateNever]
        public Company Company { get; set; } = null!;

        public int? PurchaseOrderId { get; set; }

        [ValidateNever]
        public PurchaseOrder? PurchaseOrder { get; set; }

        public int? GoodsReceiptId { get; set; }

        [ValidateNever]
        public GoodsReceipt? GoodsReceipt { get; set; }

        [Required]
        [MaxLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? SupplierInvoiceNumber { get; set; }

        [Required]
        [MaxLength(200)]
        public string SupplierName { get; set; } = string.Empty;

        [MaxLength(80)]
        public string? SupplierTaxNumber { get; set; }

        [MaxLength(500)]
        public string? SupplierAddress { get; set; }

        [MaxLength(40)]
        public string? SupplierPhone { get; set; }

        [MaxLength(256)]
        public string? SupplierEmail { get; set; }

        public DateTime InvoiceDate { get; set; }
        public DateTime? DueDate { get; set; }

        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [MaxLength(30)]
        public string Status { get; set; } = "Draft";

        [MaxLength(30)]
        public string MatchStatus { get; set; } = "Unmatched";

        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }

        public List<SupplierInvoiceItem> Items { get; set; } = new();
    }
}
