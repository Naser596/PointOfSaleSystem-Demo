using WebApplication3.Models;

namespace WebApplication3.Services;

public interface IWarehouseWorkflowService
{
    Task<GoodsReceipt> ReceivePurchaseOrderAsync(GoodsReceiptInput input, string? userName = null);
    Task<StockTransfer> TransferStockAsync(StockTransferInput input, string? userName = null);
    Task<StockTransfer> RequestTransferApprovalAsync(StockTransferInput input, string? userName = null);
    Task<StockTransfer> PostApprovedTransferAsync(int transferId, string? userName = null);
    Task<WarehouseStock> AdjustStockAsync(StockAdjustmentInput input, string? userName = null);
    Task<StockCount> CreateStockCountAsync(StockCountCreateInput input, string? userName = null);
    Task<StockCountLine> UpdateStockCountLineAsync(StockCountLineUpdateInput input);
    Task<StockCount> PostStockCountAsync(int stockCountId, string? userName = null);
}
