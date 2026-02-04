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
                    .IgnoreQueryFilters() // Allow viewing sales of deleted products
                    .OrderByDescending(s => s.SaleDate)
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
                    .IgnoreQueryFilters()
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

        public async Task<(List<Sale> Sales, int TotalCount)> SearchSalesAsync(SalesFilterDto filter)
        {
            try
            {
                var query = _context.Sales
                    .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                    .IgnoreQueryFilters()
                    .AsQueryable();

                // Apply filters
                if (filter.DateFrom.HasValue)
                {
                    query = query.Where(s => s.SaleDate >= filter.DateFrom.Value);
                }

                if (filter.DateTo.HasValue)
                {
                    var dateTo = filter.DateTo.Value.AddDays(1); // Include entire day
                    query = query.Where(s => s.SaleDate < dateTo);
                }

                if (!string.IsNullOrEmpty(filter.CashierName))
                {
                    query = query.Where(s => s.CashierName == filter.CashierName);
                }

                if (!string.IsNullOrEmpty(filter.PaymentMethod))
                {
                    query = query.Where(s => s.PaymentMethod == filter.PaymentMethod);
                }

                if (!string.IsNullOrEmpty(filter.Status))
                {
                    query = query.Where(s => s.Status == filter.Status);
                }

                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    query = query.Where(s => s.SaleNumber.Contains(filter.SearchTerm));
                }

                var totalCount = await query.CountAsync();

                var sales = await query
                    .OrderByDescending(s => s.SaleDate)
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync();

                return (sales, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error searching sales: {ex.Message}");
                throw;
            }
        }

        public async Task<List<string>> GetCashierNamesAsync()
        {
            try
            {
                return await _context.Sales
                    .Where(s => !string.IsNullOrEmpty(s.CashierName))
                    .Select(s => s.CashierName!)
                    .Distinct()
                    .OrderBy(n => n)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting cashier names: {ex.Message}");
                throw;
            }
        }
    }
}
