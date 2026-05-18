using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentCompanyService _currentCompany;

        public InventoryService(ApplicationDbContext context, ICurrentCompanyService currentCompany)
        {
            _context = context;
            _currentCompany = currentCompany;
        }

        public async Task<List<Product>> GetLowStockProductsAsync()
        {
            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.CompanyId == companyId && p.IsActive && p.Stock <= p.MinStock)
                .OrderBy(p => p.Stock)
                .ToListAsync();
        }

        public async Task<int> GetLowStockCountAsync()
        {
            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
            return await _context.Products
                .CountAsync(p => p.CompanyId == companyId && p.IsActive && p.Stock <= p.MinStock);
        }

        public async Task<StockMovement> RecordStockMovementAsync(StockMovement movement)
        {
            movement.CompanyId = await _currentCompany.GetRequiredCompanyIdAsync();
            movement.CreatedDate = DateTime.Now;
            _context.StockMovements.Add(movement);
            await _context.SaveChangesAsync();
            return movement;
        }

        public async Task<List<StockMovement>> GetProductStockHistoryAsync(int productId, int take = 50)
        {
            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
            return await _context.StockMovements
                .Where(m => m.CompanyId == companyId && m.ProductId == productId)
                .OrderByDescending(m => m.CreatedDate)
                .Take(take)
                .ToListAsync();
        }

        public async Task<bool> RestockProductAsync(int productId, int quantity, string? notes, string userId)
        {
            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId && p.CompanyId == companyId);
            if (product == null) return false;

            var previousStock = product.Stock;
            product.Stock += quantity;
            product.UpdatedDate = DateTime.Now;

            var movement = new StockMovement
            {
                CompanyId = companyId,
                ProductId = productId,
                MovementType = "Restock",
                Quantity = quantity,
                PreviousStock = previousStock,
                NewStock = product.Stock,
                Notes = notes,
                CreatedBy = userId,
                CreatedDate = DateTime.Now
            };

            _context.StockMovements.Add(movement);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AdjustStockAsync(int productId, int newStock, string reason, string userId)
        {
            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId && p.CompanyId == companyId);
            if (product == null) return false;

            var previousStock = product.Stock;
            var quantity = newStock - previousStock;
            product.Stock = newStock;
            product.UpdatedDate = DateTime.Now;

            var movement = new StockMovement
            {
                CompanyId = companyId,
                ProductId = productId,
                MovementType = "Adjustment",
                Quantity = quantity,
                PreviousStock = previousStock,
                NewStock = newStock,
                Notes = reason,
                CreatedBy = userId,
                CreatedDate = DateTime.Now
            };

            _context.StockMovements.Add(movement);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
