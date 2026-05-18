using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Services
{
    public class ProductService(ApplicationDbContext context, ILogger<ProductService> logger, ICurrentCompanyService currentCompany) : IProductService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<ProductService> _logger = logger;
        private readonly ICurrentCompanyService _currentCompany = currentCompany;

        public async Task<List<Product>> GetAllProductsAsync()
        {
            try
            {
                var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
                return await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.CompanyId == companyId && p.IsActive)
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all products: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            try
            {
                var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
                return await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product by id: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<Product> AddProductAsync(Product product)
        {
            try
            {
                product.CreatedDate = DateTime.Now;
                product.UpdatedDate = DateTime.Now;
                product.CompanyId = await _currentCompany.GetRequiredCompanyIdAsync();
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<Product> UpdateProductAsync(Product product)
        {
            try
            {
                var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
                var existing = await _context.Products.FirstOrDefaultAsync(p => p.Id == product.Id && p.CompanyId == companyId);
                if (existing == null) throw new KeyNotFoundException("Product not found");

                existing.Name = product.Name;
                existing.Description = product.Description;
                existing.SKU = product.SKU;
                existing.Barcode = product.Barcode;
                existing.CostPrice = product.CostPrice;
                existing.Price = product.Price;
                existing.TaxRate = product.TaxRate;
                existing.Stock = product.Stock;
                existing.MinStock = product.MinStock;
                existing.ImagePath = product.ImagePath;
                existing.IsActive = product.IsActive;
                existing.CategoryId = product.CategoryId;
                existing.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync();
                return existing;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<bool> DeleteProductAsync(int id, string? username = null)
        {
            try
            {
                var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);
                if (product == null)
                    return false;

                // Soft Delete
                product.IsDeleted = true;
                product.DeletedDate = DateTime.Now;
                product.DeletedBy = username ?? "System";
                product.IsActive = false; // Also mark as inactive

                _context.Products.Update(product);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<List<Product>> SearchProductsAsync(string searchTerm)
        {
            try
            {
                var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
                return await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.CompanyId == companyId && p.IsActive &&
                        (p.Name.Contains(searchTerm) ||
                         p.Description.Contains(searchTerm) ||
                         (p.SKU != null && p.SKU.Contains(searchTerm))))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<List<Product>> GetProductsAsync(string? searchTerm = null, int? categoryId = null)
        {
            try
            {
                var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
                var query = _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.CompanyId == companyId && !p.IsDeleted && p.IsActive);

                if (categoryId.HasValue)
                {
                    query = query.Where(p => p.CategoryId == categoryId.Value);
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(p =>
                        p.Name.Contains(searchTerm) ||
                        p.Description.Contains(searchTerm) ||
                        (p.SKU != null && p.SKU.Contains(searchTerm)) ||
                        (p.Barcode != null && p.Barcode.Contains(searchTerm)));
                }

                return await query.OrderBy(p => p.Name).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products (Search: {SearchTerm}, Category: {CategoryId}): {Message}", searchTerm, categoryId, ex.Message);
                throw;
            }
        }



        public async Task<Product?> GetProductByBarcodeAsync(string barcode)
        {
            try
            {
                var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
                return await _context.Products
                    .Where(p => p.CompanyId == companyId && !p.IsDeleted && p.IsActive && p.Barcode == barcode)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product by barcode: {Message}", ex.Message);
                throw;
            }
        }

    }
}
