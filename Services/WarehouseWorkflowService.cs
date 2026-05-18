using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Services;

public class WarehouseWorkflowService(
    ApplicationDbContext context,
    IErpAccountingService accounting) : IWarehouseWorkflowService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IErpAccountingService _accounting = accounting;

    public async Task<GoodsReceipt> ReceivePurchaseOrderAsync(GoodsReceiptInput input, string? userName = null)
    {
        if (input.PurchaseOrderId <= 0 || input.WarehouseId <= 0)
        {
            throw new InvalidOperationException("Purchase order and warehouse are required.");
        }

        var order = await _context.PurchaseOrders
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == input.PurchaseOrderId);
        if (order == null)
        {
            throw new InvalidOperationException("Purchase order was not found.");
        }

        await ValidateWarehouseAsync(order.CompanyId, input.WarehouseId, input.StockLocationId);

        var receipt = new GoodsReceipt
        {
            CompanyId = order.CompanyId,
            PurchaseOrderId = order.Id,
            ReceiptNumber = $"GR-{DateTime.UtcNow:yyyyMMddHHmmss}",
            ReceiptDate = input.ReceiptDate.Date,
            Status = "Posted",
            ReceivedBy = userName,
            CreatedDate = DateTime.Now
        };

        foreach (var lineInput in input.Lines.Where(l => l.Quantity > 0))
        {
            var orderLine = order.Lines.FirstOrDefault(l => l.Id == lineInput.PurchaseOrderLineId);
            if (orderLine == null) continue;

            var remaining = Math.Max(orderLine.Quantity - orderLine.ReceivedQuantity, 0);
            var receiveQuantity = Math.Min(lineInput.Quantity, remaining);
            if (receiveQuantity <= 0) continue;

            receipt.Lines.Add(new GoodsReceiptLine
            {
                ProductId = orderLine.ProductId,
                Description = orderLine.Description,
                Quantity = receiveQuantity,
                UnitCost = orderLine.UnitCost
            });

            orderLine.ReceivedQuantity += receiveQuantity;

            if (orderLine.ProductId.HasValue)
            {
                await IncreaseWarehouseStockAsync(
                    order.CompanyId,
                    input.WarehouseId,
                    input.StockLocationId,
                    orderLine.ProductId.Value,
                    receiveQuantity,
                    $"Received from purchase order {order.OrderNumber}",
                    userName);
            }
        }

        if (!receipt.Lines.Any())
        {
            throw new InvalidOperationException("No receivable quantities were entered.");
        }

        _context.GoodsReceipts.Add(receipt);
        order.Status = order.Lines.All(l => l.ReceivedQuantity >= l.Quantity) ? "Received" : "PartiallyReceived";
        await _context.SaveChangesAsync();
        await _accounting.PostGoodsReceiptAsync(receipt.Id, userName);

        return receipt;
    }

    public async Task<StockTransfer> TransferStockAsync(StockTransferInput input, string? userName = null)
    {
        if (input.ProductId <= 0 || input.FromWarehouseId <= 0 || input.ToWarehouseId <= 0 || input.Quantity <= 0)
        {
            throw new InvalidOperationException("Product, warehouses, and positive quantity are required.");
        }

        if (input.FromWarehouseId == input.ToWarehouseId)
        {
            throw new InvalidOperationException("Transfer destination warehouse must be different from the source.");
        }

        var product = await _context.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == input.ProductId);
        if (product == null)
        {
            throw new InvalidOperationException("Product was not found.");
        }

        await ValidateWarehouseAsync(product.CompanyId, input.FromWarehouseId, input.FromStockLocationId);
        await ValidateWarehouseAsync(product.CompanyId, input.ToWarehouseId, input.ToStockLocationId);

        var source = await GetOrCreateWarehouseStockAsync(product.CompanyId, input.FromWarehouseId, input.FromStockLocationId, input.ProductId);
        if (source.QuantityOnHand < input.Quantity)
        {
            throw new InvalidOperationException("Source warehouse does not have enough stock.");
        }

        var destination = await GetOrCreateWarehouseStockAsync(product.CompanyId, input.ToWarehouseId, input.ToStockLocationId, input.ProductId);
        source.QuantityOnHand -= input.Quantity;
        destination.QuantityOnHand += input.Quantity;
        source.UpdatedDate = DateTime.Now;
        destination.UpdatedDate = DateTime.Now;

        var transfer = new StockTransfer
        {
            CompanyId = product.CompanyId,
            TransferNumber = $"ST-{DateTime.UtcNow:yyyyMMddHHmmss}",
            FromWarehouseId = input.FromWarehouseId,
            ToWarehouseId = input.ToWarehouseId,
            Status = "Posted",
            TransferDate = input.TransferDate.Date,
            CreatedBy = userName,
            CreatedDate = DateTime.Now,
            Lines =
            [
                new StockTransferLine
                {
                    ProductId = product.Id,
                    Quantity = input.Quantity
                }
            ]
        };

        _context.StockTransfers.Add(transfer);
        await _context.SaveChangesAsync();
        return transfer;
    }

    public async Task<StockTransfer> RequestTransferApprovalAsync(StockTransferInput input, string? userName = null)
    {
        if (input.ProductId <= 0 || input.FromWarehouseId <= 0 || input.ToWarehouseId <= 0 || input.Quantity <= 0)
        {
            throw new InvalidOperationException("Product, warehouses, and positive quantity are required.");
        }

        if (input.FromWarehouseId == input.ToWarehouseId)
        {
            throw new InvalidOperationException("Transfer destination warehouse must be different from the source.");
        }

        var product = await _context.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == input.ProductId);
        if (product == null)
        {
            throw new InvalidOperationException("Product was not found.");
        }

        await ValidateWarehouseAsync(product.CompanyId, input.FromWarehouseId, input.FromStockLocationId);
        await ValidateWarehouseAsync(product.CompanyId, input.ToWarehouseId, input.ToStockLocationId);

        var transfer = new StockTransfer
        {
            CompanyId = product.CompanyId,
            TransferNumber = $"ST-{DateTime.UtcNow:yyyyMMddHHmmss}",
            FromWarehouseId = input.FromWarehouseId,
            ToWarehouseId = input.ToWarehouseId,
            Status = "PendingApproval",
            TransferDate = input.TransferDate.Date,
            CreatedBy = userName,
            CreatedDate = DateTime.Now,
            Lines =
            [
                new StockTransferLine
                {
                    ProductId = product.Id,
                    Quantity = input.Quantity
                }
            ]
        };

        _context.StockTransfers.Add(transfer);
        await _context.SaveChangesAsync();
        _context.ApprovalRequests.Add(new ApprovalRequest
        {
            CompanyId = product.CompanyId,
            RequestType = "Stock transfer approval",
            EntityName = nameof(StockTransfer),
            EntityId = transfer.Id.ToString(),
            Status = "Pending",
            Notes = $"Transfer {input.Quantity:N2} of {product.Name} requires approval.",
            RequestedBy = userName,
            RequestedDate = DateTime.Now
        });
        await _context.SaveChangesAsync();
        return transfer;
    }

    public async Task<StockTransfer> PostApprovedTransferAsync(int transferId, string? userName = null)
    {
        var transfer = await _context.StockTransfers
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == transferId);
        if (transfer == null)
        {
            throw new InvalidOperationException("Stock transfer was not found.");
        }

        if (transfer.Status == "Posted")
        {
            return transfer;
        }

        var approved = await _context.ApprovalRequests.AnyAsync(r =>
            r.CompanyId == transfer.CompanyId &&
            r.EntityName == nameof(StockTransfer) &&
            r.EntityId == transfer.Id.ToString() &&
            r.Status == "Approved");
        if (!approved)
        {
            throw new InvalidOperationException("Stock transfer must be approved before posting.");
        }

        foreach (var line in transfer.Lines)
        {
            await MoveStockAsync(
                transfer.CompanyId,
                line.ProductId,
                transfer.FromWarehouseId,
                null,
                transfer.ToWarehouseId,
                null,
                line.Quantity,
                userName);
        }

        transfer.Status = "Posted";
        await _context.SaveChangesAsync();
        return transfer;
    }

    public async Task<WarehouseStock> AdjustStockAsync(StockAdjustmentInput input, string? userName = null)
    {
        if (input.ProductId <= 0 || input.WarehouseId <= 0 || input.QuantityDelta == 0)
        {
            throw new InvalidOperationException("Product, warehouse, and non-zero adjustment are required.");
        }

        var product = await _context.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == input.ProductId);
        if (product == null)
        {
            throw new InvalidOperationException("Product was not found.");
        }

        await ValidateWarehouseAsync(product.CompanyId, input.WarehouseId, input.StockLocationId);
        var stock = await GetOrCreateWarehouseStockAsync(product.CompanyId, input.WarehouseId, input.StockLocationId, input.ProductId);
        if (stock.QuantityOnHand + input.QuantityDelta < 0)
        {
            throw new InvalidOperationException("Adjustment would make warehouse stock negative.");
        }

        var previousProductStock = product.Stock;
        stock.QuantityOnHand += input.QuantityDelta;
        stock.UpdatedDate = DateTime.Now;
        product.Stock += (int)Math.Round(input.QuantityDelta, MidpointRounding.AwayFromZero);
        product.UpdatedDate = DateTime.Now;

        _context.StockMovements.Add(new StockMovement
        {
            CompanyId = product.CompanyId,
            ProductId = product.Id,
            MovementType = "Adjustment",
            Quantity = (int)Math.Round(input.QuantityDelta, MidpointRounding.AwayFromZero),
            PreviousStock = previousProductStock,
            NewStock = product.Stock,
            ReferenceType = "WarehouseAdjustment",
            Notes = string.IsNullOrWhiteSpace(input.Reason) ? "Warehouse stock adjustment" : input.Reason.Trim(),
            CreatedBy = userName,
            CreatedDate = DateTime.Now
        });

        await _context.SaveChangesAsync();
        return stock;
    }

    public async Task<StockCount> CreateStockCountAsync(StockCountCreateInput input, string? userName = null)
    {
        if (input.WarehouseId <= 0)
        {
            throw new InvalidOperationException("Warehouse is required.");
        }

        var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == input.WarehouseId);
        if (warehouse == null)
        {
            throw new InvalidOperationException("Warehouse was not found.");
        }

        var stocks = await _context.WarehouseStocks
            .Include(s => s.Product)
            .Where(s => s.CompanyId == warehouse.CompanyId && s.WarehouseId == warehouse.Id)
            .OrderBy(s => s.Product.Name)
            .ToListAsync();
        if (!stocks.Any())
        {
            throw new InvalidOperationException("No stock exists in this warehouse to count.");
        }

        var count = new StockCount
        {
            CompanyId = warehouse.CompanyId,
            WarehouseId = warehouse.Id,
            CountNumber = $"SC-{DateTime.UtcNow:yyyyMMddHHmmss}",
            CountDate = input.CountDate.Date,
            Status = "Draft",
            Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
            CreatedBy = userName,
            CreatedDate = DateTime.Now
        };

        foreach (var stock in stocks)
        {
            count.Lines.Add(new StockCountLine
            {
                ProductId = stock.ProductId,
                StockLocationId = stock.StockLocationId,
                SystemQuantity = stock.QuantityOnHand,
                CountedQuantity = stock.QuantityOnHand
            });
        }

        _context.StockCounts.Add(count);
        await _context.SaveChangesAsync();
        return count;
    }

    public async Task<StockCountLine> UpdateStockCountLineAsync(StockCountLineUpdateInput input)
    {
        var line = await _context.StockCountLines
            .Include(l => l.StockCount)
            .FirstOrDefaultAsync(l => l.Id == input.LineId && l.StockCountId == input.StockCountId);
        if (line == null)
        {
            throw new InvalidOperationException("Stock count line was not found.");
        }

        if (line.StockCount.Status == "Posted")
        {
            throw new InvalidOperationException("Posted stock counts cannot be edited.");
        }

        if (input.CountedQuantity < 0)
        {
            throw new InvalidOperationException("Counted quantity cannot be negative.");
        }

        line.CountedQuantity = input.CountedQuantity;
        line.Reason = string.IsNullOrWhiteSpace(input.Reason) ? null : input.Reason.Trim();
        await _context.SaveChangesAsync();
        return line;
    }

    public async Task<StockCount> PostStockCountAsync(int stockCountId, string? userName = null)
    {
        var count = await _context.StockCounts
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == stockCountId);
        if (count == null)
        {
            throw new InvalidOperationException("Stock count was not found.");
        }

        if (count.Status == "Posted")
        {
            return count;
        }

        foreach (var line in count.Lines)
        {
            var variance = line.Variance;
            if (variance == 0)
            {
                continue;
            }

            await AdjustStockAsync(new StockAdjustmentInput
            {
                ProductId = line.ProductId,
                WarehouseId = count.WarehouseId,
                StockLocationId = line.StockLocationId,
                QuantityDelta = variance,
                Reason = string.IsNullOrWhiteSpace(line.Reason)
                    ? $"Stock count {count.CountNumber}"
                    : $"Stock count {count.CountNumber}: {line.Reason}"
            }, userName);
        }

        count.Status = "Posted";
        count.PostedDate = DateTime.Now;
        await _context.SaveChangesAsync();
        return count;
    }

    private async Task ValidateWarehouseAsync(int companyId, int warehouseId, int? locationId)
    {
        var warehouseExists = await _context.Warehouses
            .AnyAsync(w => w.Id == warehouseId && w.CompanyId == companyId);
        if (!warehouseExists)
        {
            throw new InvalidOperationException("Warehouse belongs to another company or does not exist.");
        }

        if (locationId.HasValue)
        {
            var locationExists = await _context.StockLocations.AnyAsync(l =>
                l.Id == locationId.Value &&
                l.CompanyId == companyId &&
                l.WarehouseId == warehouseId);
            if (!locationExists)
            {
                throw new InvalidOperationException("Stock location belongs to another warehouse or company.");
            }
        }
    }

    private async Task MoveStockAsync(
        int companyId,
        int productId,
        int fromWarehouseId,
        int? fromLocationId,
        int toWarehouseId,
        int? toLocationId,
        decimal quantity,
        string? userName)
    {
        var source = await GetOrCreateWarehouseStockAsync(companyId, fromWarehouseId, fromLocationId, productId);
        if (source.QuantityOnHand < quantity)
        {
            throw new InvalidOperationException("Source warehouse does not have enough stock.");
        }

        var destination = await GetOrCreateWarehouseStockAsync(companyId, toWarehouseId, toLocationId, productId);
        source.QuantityOnHand -= quantity;
        destination.QuantityOnHand += quantity;
        source.UpdatedDate = DateTime.Now;
        destination.UpdatedDate = DateTime.Now;

        var product = await _context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == productId);
        if (product != null)
        {
            _context.StockMovements.Add(new StockMovement
            {
                CompanyId = companyId,
                ProductId = productId,
                MovementType = "Transfer",
                Quantity = (int)Math.Round(quantity, MidpointRounding.AwayFromZero),
                PreviousStock = product.Stock,
                NewStock = product.Stock,
                ReferenceType = "StockTransfer",
                Notes = $"Transferred between warehouses by {userName}",
                CreatedBy = userName,
                CreatedDate = DateTime.Now
            });
        }
    }

    private async Task<WarehouseStock> GetOrCreateWarehouseStockAsync(int companyId, int warehouseId, int? locationId, int productId)
    {
        var stock = await _context.WarehouseStocks.FirstOrDefaultAsync(s =>
            s.CompanyId == companyId &&
            s.WarehouseId == warehouseId &&
            s.StockLocationId == locationId &&
            s.ProductId == productId);

        if (stock != null)
        {
            return stock;
        }

        stock = new WarehouseStock
        {
            CompanyId = companyId,
            WarehouseId = warehouseId,
            StockLocationId = locationId,
            ProductId = productId,
            QuantityOnHand = 0,
            QuantityReserved = 0,
            UpdatedDate = DateTime.Now
        };
        _context.WarehouseStocks.Add(stock);
        return stock;
    }

    private async Task IncreaseWarehouseStockAsync(
        int companyId,
        int warehouseId,
        int? stockLocationId,
        int productId,
        decimal quantity,
        string notes,
        string? userName)
    {
        var stock = await GetOrCreateWarehouseStockAsync(companyId, warehouseId, stockLocationId, productId);
        stock.QuantityOnHand += quantity;
        stock.UpdatedDate = DateTime.Now;

        var product = await _context.Products.FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == productId);
        if (product == null)
        {
            return;
        }

        var previousStock = product.Stock;
        var stockIncrease = (int)Math.Round(quantity, MidpointRounding.AwayFromZero);
        product.Stock += stockIncrease;
        product.UpdatedDate = DateTime.Now;

        _context.StockMovements.Add(new StockMovement
        {
            CompanyId = companyId,
            ProductId = product.Id,
            MovementType = "Restock",
            Quantity = stockIncrease,
            PreviousStock = previousStock,
            NewStock = product.Stock,
            ReferenceType = "GoodsReceipt",
            Notes = notes,
            CreatedBy = userName,
            CreatedDate = DateTime.Now
        });
    }
}
