using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebApplication3.Models
{
    public class Product
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        [ValidateNever]
        public Company Company { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? SKU { get; set; }
        public string? Barcode { get; set; }
        public decimal CostPrice { get; set; }
        public decimal Price { get; set; }
        public decimal TaxRate { get; set; }
        public int Stock { get; set; }
        public int MinStock { get; set; } = 5;
        public string? ImagePath { get; set; }
        public bool IsActive { get; set; } = true;

        // Soft Delete
        public bool IsDeleted { get; set; }
        public DateTime? DeletedDate { get; set; }
        public string? DeletedBy { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }

        // Category relationship
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        // Navigation
        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

        // Computed property
        public bool IsLowStock => Stock <= MinStock;
    }
}
