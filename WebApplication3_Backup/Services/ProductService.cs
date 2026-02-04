using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductService> _logger;

        public ProductService(ApplicationDbContext context, ILogger<ProductService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            try
            {
                return await _context.Products.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting all products: {ex.Message}");
                throw;
            }
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            try
            {
                return await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting product by id: {ex.Message}");
                throw;
            }
        }

        public async Task<Product> AddProductAsync(Product product)
        {
            try
            {
                product.CreatedDate = DateTime.Now;
                product.UpdatedDate = DateTime.Now;
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding product: {ex.Message}");
                throw;
            }
        }

        public async Task<Product> UpdateProductAsync(Product product)
        {
            try
            {
                product.UpdatedDate = DateTime.Now;
                _context.Products.Update(product);
                await _context.SaveChangesAsync();
                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating product: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            try
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
                if (product == null)
                    return false;

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting product: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Product>> SearchProductsAsync(string searchTerm)
        {
            try
            {
                return await _context.Products
                    .Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error searching products: {ex.Message}");
                throw;
            }
        }
    }
}
