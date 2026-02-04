using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApplication3.Models
{
    public class ProductCreateViewModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public string? SKU { get; set; }

        [Required]
        [Range(0.01, 100000.00)]
        public decimal Price { get; set; }

        [Required]
        [Range(0, 10000)]
        public int Stock { get; set; }
        
        [Display(Name = "Minimum Stock")]
        [Range(0, 1000)]
        public int MinStock { get; set; } = 5;
        
        [Display(Name = "Category")]
        public int? CategoryId { get; set; }
        
        [Display(Name = "Product Image")]
        public IFormFile? ImageFile { get; set; }
        
        public List<SelectListItem>? Categories { get; set; }
    }

    public class ProductEditViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public string? SKU { get; set; }

        [Required]
        [Range(0.01, 100000.00)]
        public decimal Price { get; set; }

        [Required]
        [Range(0, 10000)]
        public int Stock { get; set; }
        
        [Display(Name = "Minimum Stock")]
        [Range(0, 1000)]
        public int MinStock { get; set; } = 5;
        
        [Display(Name = "Category")]
        public int? CategoryId { get; set; }

        public string? CurrentImagePath { get; set; }
        
        [Display(Name = "Change Image")]
        public IFormFile? ImageFile { get; set; }
        
        public List<SelectListItem>? Categories { get; set; }
    }
}
