using WebApplication3.Models;

namespace WebApplication3.Services
{
    public interface IInventoryService
    {
        Task<List<Product>> GetLowStockProductsAsync();
        Task<int> GetLowStockCountAsync();
        Task<StockMovement> RecordStockMovementAsync(StockMovement movement);
        Task<List<StockMovement>> GetProductStockHistoryAsync(int productId, int take = 50);
        Task<bool> RestockProductAsync(int productId, int quantity, string? notes, string userId);
        Task<bool> AdjustStockAsync(int productId, int newStock, string reason, string userId);
    }
}
