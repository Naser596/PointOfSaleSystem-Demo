using System.ComponentModel.DataAnnotations;

namespace WebApplication3.Models
{
    public class SupplierInvoiceItem
    {
        public int Id { get; set; }
        public int SupplierInvoiceId { get; set; }

        public int? ProductId { get; set; }

        [Required]
        [MaxLength(300)]
        public string Description { get; set; } = string.Empty;

        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal LineTotal { get; set; }

        public SupplierInvoice SupplierInvoice { get; set; } = null!;
        public Product? Product { get; set; }
    }
}
