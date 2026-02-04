namespace WebApplication3.Services
{
    using WebApplication3.Models;

    public interface IProductService
    {
        Task<List<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task<Product> AddProductAsync(Product product);
        Task<Product> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(int id);
        Task<List<Product>> SearchProductsAsync(string searchTerm);
    }
}
