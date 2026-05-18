namespace WebApplication3.Models
{
    public class SaleItem
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public int ProductId { get; set; }
        public string ProductNameSnapshot { get; set; } = string.Empty;
        public string? ProductSkuSnapshot { get; set; }
        public int Quantity { get; set; }
        public int ReturnedQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal RefundedAmount { get; set; }
        public decimal TotalPrice { get; set; }

        public int ReturnableQuantity => Math.Max(Quantity - ReturnedQuantity, 0);
        
        // Navigation properties
        public Sale Sale { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
