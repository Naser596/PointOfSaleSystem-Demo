using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Services
{
    public class SaleService : ISaleService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SaleService> _logger;

        public SaleService(ApplicationDbContext context, ILogger<SaleService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Sale>> GetAllSalesAsync()
        {
            try
            {
                return await _context.Sales
                    .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting all sales: {ex.Message}");
                throw;
            }
        }

        public async Task<Sale?> GetSaleByIdAsync(int id)
        {
            try
            {
                return await _context.Sales
                    .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                    .FirstOrDefaultAsync(s => s.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting sale by id: {ex.Message}");
                throw;
            }
        }

        public async Task<Sale> CreateSaleAsync(Sale sale)
        {
            try
            {
                sale.SaleDate = DateTime.Now;
                sale.SaleNumber = $"SALE-{DateTime.Now:yyyyMMddHHmmss}";
                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();
                return sale;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating sale: {ex.Message}");
                throw;
            }
        }

        public async Task<Sale> UpdateSaleAsync(Sale sale)
        {
            try
            {
                _context.Sales.Update(sale);
                await _context.SaveChangesAsync();
                return sale;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating sale: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteSaleAsync(int id)
        {
            try
            {
                var sale = await _context.Sales.FirstOrDefaultAsync(s => s.Id == id);
                if (sale == null)
                    return false;

                _context.Sales.Remove(sale);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting sale: {ex.Message}");
                throw;
            }
        }

        public async Task<decimal> GetTotalSalesAsync()
        {
            try
            {
                return await _context.Sales.SumAsync(s => s.TotalAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting total sales: {ex.Message}");
                throw;
            }
        }
    }
}
