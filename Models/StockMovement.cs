namespace WebApplication3.Models
{
    public class StockMovement
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string MovementType { get; set; } = string.Empty; // Sale, Restock, Adjustment, Return
        public int Quantity { get; set; }
        public int PreviousStock { get; set; }
        public int NewStock { get; set; }
        public int? ReferenceId { get; set; }
        public string? ReferenceType { get; set; }
        public string? Notes { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        
        // Navigation
        public Product Product { get; set; } = null!;
    }
}
