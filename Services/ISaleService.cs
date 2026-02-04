using WebApplication3.Models;

namespace WebApplication3.Services
{
    public interface ISaleService
    {
        Task<List<Sale>> GetAllSalesAsync();
        Task<Sale?> GetSaleByIdAsync(int id);
        Task<Sale> CreateSaleAsync(Sale sale);
        Task<Sale> UpdateSaleAsync(Sale sale);
        Task<bool> DeleteSaleAsync(int id);
        Task<decimal> GetTotalSalesAsync();
        
        // New filtering methods
        Task<(List<Sale> Sales, int TotalCount)> SearchSalesAsync(SalesFilterDto filter);
        Task<List<string>> GetCashierNamesAsync();
    }
    
    public class SalesFilterDto
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? CashierName { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Status { get; set; }
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
