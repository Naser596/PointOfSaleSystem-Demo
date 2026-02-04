namespace WebApplication3.Models
{
    public class Sale
    {
        public int Id { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; } = DateTime.Now;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Completed"; // Completed, Pending, Cancelled
        public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    }
}
