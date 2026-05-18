using Microsoft.EntityFrameworkCore;
using WebApplication3.Models;
using WebApplication3.Services;
using WebApplication3.Tests.Support;

namespace WebApplication3.Tests.Services;

public sealed class WarehouseWorkflowServiceTests
{
    [Fact]
    public async Task ReceivePurchaseOrderAsync_UpdatesWarehouseStockProductStockAndPostsJournal()
    {
        using var database = new TestDatabase();
        SeedCompanyWarehouseAndProduct(database, productStock: 4);
        var order = new PurchaseOrder
        {
            CompanyId = 1,
            OrderNumber = "PO-1",
            SupplierName = "Supplier",
            OrderDate = DateTime.Today,
            Status = "Draft",
            CreatedDate = DateTime.Now,
            Lines =
            [
                new PurchaseOrderLine
                {
                    ProductId = 10,
                    Description = "Product",
                    Quantity = 3,
                    UnitCost = 5,
                    LineTotal = 15
                }
            ]
        };
        database.Context.PurchaseOrders.Add(order);
        await database.Context.SaveChangesAsync();
        var service = new WarehouseWorkflowService(database.Context, new ErpAccountingService(database.Context));

        var receipt = await service.ReceivePurchaseOrderAsync(new GoodsReceiptInput
        {
            PurchaseOrderId = order.Id,
            WarehouseId = 20,
            ReceiptDate = DateTime.Today,
            Lines = [new GoodsReceiptLineInput { PurchaseOrderLineId = order.Lines[0].Id, Quantity = 3 }]
        }, "tester");

        Assert.Equal("Posted", receipt.Status);
        Assert.Equal("Received", order.Status);
        Assert.Equal(7, database.Context.Products.Single(p => p.Id == 10).Stock);
        Assert.Equal(3, database.Context.WarehouseStocks.Single(s => s.ProductId == 10).QuantityOnHand);
        Assert.Contains(database.Context.JournalEntries, e => e.SourceType == nameof(GoodsReceipt) && e.SourceId == receipt.Id.ToString());
    }

    [Fact]
    public async Task TransferStockAsync_MovesStockBetweenWarehouses()
    {
        using var database = new TestDatabase();
        SeedCompanyWarehouseAndProduct(database, productStock: 10);
        await database.Context.SaveChangesAsync();
        database.Context.Warehouses.Add(new Warehouse
        {
            Id = 21,
            CompanyId = 1,
            Name = "Secondary",
            Code = "S",
            CreatedDate = DateTime.Now
        });
        database.Context.WarehouseStocks.Add(new WarehouseStock
        {
            CompanyId = 1,
            WarehouseId = 20,
            ProductId = 10,
            QuantityOnHand = 6,
            UpdatedDate = DateTime.Now
        });
        await database.Context.SaveChangesAsync();
        var service = new WarehouseWorkflowService(database.Context, new ErpAccountingService(database.Context));

        await service.TransferStockAsync(new StockTransferInput
        {
            ProductId = 10,
            FromWarehouseId = 20,
            ToWarehouseId = 21,
            Quantity = 2,
            TransferDate = DateTime.Today
        });

        Assert.Equal(4, database.Context.WarehouseStocks.Single(s => s.WarehouseId == 20).QuantityOnHand);
        Assert.Equal(2, database.Context.WarehouseStocks.Single(s => s.WarehouseId == 21).QuantityOnHand);
        Assert.Single(database.Context.StockTransfers);
    }

    [Fact]
    public async Task AdjustStockAsync_RejectsNegativeWarehouseResult()
    {
        using var database = new TestDatabase();
        SeedCompanyWarehouseAndProduct(database, productStock: 1);
        await database.Context.SaveChangesAsync();
        database.Context.WarehouseStocks.Add(new WarehouseStock
        {
            CompanyId = 1,
            WarehouseId = 20,
            ProductId = 10,
            QuantityOnHand = 1,
            UpdatedDate = DateTime.Now
        });
        await database.Context.SaveChangesAsync();
        var service = new WarehouseWorkflowService(database.Context, new ErpAccountingService(database.Context));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AdjustStockAsync(new StockAdjustmentInput
            {
                ProductId = 10,
                WarehouseId = 20,
                QuantityDelta = -2
            }));

        Assert.Contains("negative", error.Message);
    }

    private static void SeedCompanyWarehouseAndProduct(TestDatabase database, int productStock)
    {
        database.Context.Companies.Add(new Company
        {
            Id = 1,
            DisplayName = "Company",
            InvoicePrefix = "INV",
            PrimaryColor = "#2563eb",
            CurrencyCode = "USD",
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        });
        database.Context.Products.Add(new Product
        {
            Id = 10,
            CompanyId = 1,
            Name = "Product",
            Description = "Product",
            SKU = "P-1",
            Price = 10,
            CostPrice = 5,
            Stock = productStock,
            IsActive = true,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        });
        database.Context.Warehouses.Add(new Warehouse
        {
            Id = 20,
            CompanyId = 1,
            Name = "Main",
            Code = "M",
            CreatedDate = DateTime.Now
        });
    }
}
