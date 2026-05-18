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

        [Display(Name = "Barcode")]
        public string? Barcode { get; set; }

        [Required]
        [Range(0.01, 100000.00)]
        public decimal Price { get; set; }

        [Display(Name = "Cost Price")]
        [Range(0, 100000.00)]
        public decimal CostPrice { get; set; }

        [Display(Name = "Tax Rate %")]
        [Range(0, 100)]
        public decimal TaxRate { get; set; }

        [Required]
        [Range(0, 10000)]
        public int Stock { get; set; }

        [Display(Name = "Initial Warehouse")]
        public int? InitialWarehouseId { get; set; }

        [Display(Name = "Initial Location")]
        public int? InitialStockLocationId { get; set; }

        [Display(Name = "Stock Origin")]
        public string? StockOriginNote { get; set; }

        [Display(Name = "Minimum Stock")]
        [Range(0, 1000)]
        public int MinStock { get; set; } = 5;

        [Display(Name = "Category")]
        public int? CategoryId { get; set; }

        [Display(Name = "Product Image")]
        public IFormFile? ImageFile { get; set; }

        public List<SelectListItem>? Categories { get; set; }
        public List<SelectListItem>? Warehouses { get; set; }
        public List<SelectListItem>? StockLocations { get; set; }
    }

    public class ProductEditViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? SKU { get; set; }

        [Display(Name = "Barcode")]
        public string? Barcode { get; set; }

        [Required]
        [Range(0.01, 100000.00)]
        public decimal Price { get; set; }

        [Display(Name = "Cost Price")]
        [Range(0, 100000.00)]
        public decimal CostPrice { get; set; }

        [Display(Name = "Tax Rate %")]
        [Range(0, 100)]
        public decimal TaxRate { get; set; }

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
