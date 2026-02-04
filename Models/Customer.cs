namespace WebApplication3.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Notes { get; set; }
        public int LoyaltyPoints { get; set; }
        public decimal TotalPurchases { get; set; }
        public int VisitCount { get; set; }
        
        // Soft Delete
        public bool IsDeleted { get; set; }
        public DateTime? DeletedDate { get; set; }
        public string? DeletedBy { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        
        // Navigation
        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }
}
