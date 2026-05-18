using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebApplication3.Models
{
    public class Category
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        [ValidateNever]
        public Company Company { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconClass { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;

        // Soft Delete
        public bool IsDeleted { get; set; }
        public DateTime? DeletedDate { get; set; }
        public string? DeletedBy { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        
        // Navigation
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
