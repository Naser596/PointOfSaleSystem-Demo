using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Services
{
    public class SaleService : ISaleService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SaleService> _logger;
        private readonly ICurrentCompanyService _currentCompany;

        public SaleService(ApplicationDbContext context, ILogger<SaleService> logger, ICurrentCompanyService currentCompany)
        {
            _context = context;
            _logger = logger;
            _currentCompany = currentCompany;
        }

        public async Task<List<Sale>> GetAllSalesAsync()
        {
            try
            {
                var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
                return await _context.Sales
                    .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                    .IgnoreQueryFilters() // Allow viewing sales of deleted products
                    .Where(s => s.CompanyId == companyId)
                    .OrderByDescending(s => s.SaleDate)
                    .ToListAsync();
            }

            catch (Exception ex)
            {
                _logger.LogError($"Error getting all sales: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Sale>> GetSalesByCashierAsync(string cashierName)
        {
            try
            {
                var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
                return await _context.Sales
                    .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                    .IgnoreQueryFilters()
                    .Where(s => s.CompanyId == companyId && s.CashierName == cashierName)
                    .OrderByDescending(s => s.SaleDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting sales by cashier: {ex.Message}");
                throw;
            }
        }

        public async Task<Sale?> GetSaleByIdAsync(int id)
        {
            try
            {
                var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
                return await _context.Sales
                    .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(s => s.Id == id && s.CompanyId == companyId);

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
                sale.CompanyId = await _currentCompany.GetRequiredCompanyIdAsync();
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
                var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
                var existing = await _context.Sales.FirstOrDefaultAsync(s => s.Id == sale.Id && s.CompanyId == companyId);
                if (existing == null)
                {
                    throw new InvalidOperationException("Sale not found for this company.");
                }

                existing.Status = sale.Status;
                existing.PaymentMethod = sale.PaymentMethod;
                await _context.SaveChangesAsync();
                return existing;
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
                var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
                var sale = await _context.Sales.FirstOrDefaultAsync(s => s.Id == id && s.CompanyId == companyId);
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
                var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
                return await _context.Sales
                    .Where(s => s.CompanyId == companyId)
                    .SumAsync(s => s.TotalAmount);
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
                var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
                var query = _context.Sales
                    .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                    .IgnoreQueryFilters()
                    .Where(s => s.CompanyId == companyId)
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
                var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
                return await _context.Sales
                    .Where(s => s.CompanyId == companyId && !string.IsNullOrEmpty(s.CashierName))
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
