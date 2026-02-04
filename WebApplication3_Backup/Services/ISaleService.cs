namespace WebApplication3.Services
{
    using WebApplication3.Models;

    public interface ISaleService
    {
        Task<List<Sale>> GetAllSalesAsync();
        Task<Sale?> GetSaleByIdAsync(int id);
        Task<Sale> CreateSaleAsync(Sale sale);
        Task<Sale> UpdateSaleAsync(Sale sale);
        Task<bool> DeleteSaleAsync(int id);
        Task<decimal> GetTotalSalesAsync();
    }
}
