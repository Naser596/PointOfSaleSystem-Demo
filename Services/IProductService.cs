using WebApplication3.Models;

namespace WebApplication3.Services
{
    public interface IProductService
    {
        Task<List<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task<Product> AddProductAsync(Product product);
        Task<Product> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(int id, string? username = null);
        Task<List<Product>> SearchProductsAsync(string searchTerm);
        Task<List<Product>> GetProductsAsync(string? searchTerm = null, int? categoryId = null);
        Task<Product?> GetProductByBarcodeAsync(string barcode);
    }
}
