using WebApplication3.Models;
using WebApplication3.Services;
using WebApplication3.Tests.Support;

namespace WebApplication3.Tests.Services;

public sealed class ErpReportServiceTests
{
    [Fact]
    public async Task BuildReportAsync_SeparatesRevenuePipelineAndReceivables()
    {
        using var database = new TestDatabase();
        database.Context.Companies.Add(Company(1));
        database.Context.Customers.Add(new Customer { Id = 5, CompanyId = 1, Name = "Acme", CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now });
        await database.Context.SaveChangesAsync();
        database.Context.SalesDocuments.AddRange(
            SalesDocument(1, "Invoice", "INV-1", 5, 1000, 250, DateTime.Today.AddDays(-10), "Issued", "PartiallyPaid"),
            SalesDocument(2, "Quote", "Q-1", 5, 500, 0, DateTime.Today, "Draft", "NotApplicable"),
            SalesDocument(3, "Order", "SO-1", 5, 300, 0, DateTime.Today, "Draft", "NotApplicable"),
            SalesDocument(4, "CreditNote", "CN-1", 5, -100, 0, DateTime.Today, "Draft", "NotApplicable"));
        await database.Context.SaveChangesAsync();
        var service = new ErpReportService(database.Context);

        var report = await service.BuildReportAsync(1, DateTime.Today.AddDays(-30), DateTime.Today);

        Assert.Equal(1000, report.InvoiceRevenueTotal);
        Assert.Equal(100, report.CreditNotesTotal);
        Assert.Equal(900, report.NetSalesTotal);
        Assert.Equal(250, report.PaidInvoicesTotal);
        Assert.Equal(500, report.QuotesPipelineTotal);
        Assert.Equal(300, report.OrdersPipelineTotal);
        Assert.Equal(750, report.AccountsReceivableTotal);
        Assert.Single(report.ReceivablesAging);
        Assert.Equal("Acme", report.ReceivablesAging[0].CustomerName);
    }

    [Fact]
    public async Task BuildReportAsync_BuildsInventoryPurchaseVarianceAndCustomerStatement()
    {
        using var database = new TestDatabase();
        database.Context.Companies.Add(Company(1));
        database.Context.Customers.Add(new Customer { Id = 7, CompanyId = 1, Name = "Retail Customer", CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now });
        database.Context.Products.Add(new Product
        {
            Id = 11,
            CompanyId = 1,
            Name = "Keyboard",
            SKU = "KEY-1",
            CostPrice = 20,
            Price = 35,
            Stock = 2,
            MinStock = 5,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        });
        database.Context.Warehouses.Add(new Warehouse { Id = 2, CompanyId = 1, Name = "Main", IsActive = true, CreatedDate = DateTime.Now });
        await database.Context.SaveChangesAsync();
        database.Context.WarehouseStocks.Add(new WarehouseStock
        {
            CompanyId = 1,
            WarehouseId = 2,
            ProductId = 11,
            QuantityOnHand = 2,
            UpdatedDate = DateTime.Now
        });
        database.Context.Sales.Add(new Sale
        {
            Id = 30,
            CompanyId = 1,
            CustomerId = 7,
            SaleNumber = "S-1",
            SaleDate = DateTime.Today,
            TotalAmount = 70,
            Status = "Completed",
            SaleItems =
            [
                new SaleItem
                {
                    ProductId = 11,
                    ProductNameSnapshot = "Keyboard",
                    Quantity = 2,
                    UnitPrice = 35,
                    UnitCost = 20,
                    TotalPrice = 70
                }
            ]
        });
        database.Context.PurchaseOrders.Add(new PurchaseOrder
        {
            Id = 40,
            CompanyId = 1,
            OrderNumber = "PO-1",
            SupplierName = "Supplier",
            OrderDate = DateTime.Today,
            Status = "Sent",
            TotalAmount = 500,
            CreatedDate = DateTime.Now,
            Lines =
            [
                new PurchaseOrderLine
                {
                    Description = "Keyboard",
                    ProductId = 11,
                    Quantity = 10,
                    UnitCost = 50,
                    LineTotal = 500
                }
            ]
        });
        await database.Context.SaveChangesAsync();
        database.Context.GoodsReceipts.Add(new GoodsReceipt
        {
            CompanyId = 1,
            PurchaseOrderId = 40,
            ReceiptNumber = "GR-1",
            ReceiptDate = DateTime.Today,
            Status = "Posted",
            CreatedDate = DateTime.Now,
            Lines = [new GoodsReceiptLine { ProductId = 11, Description = "Keyboard", Quantity = 6, UnitCost = 50 }]
        });
        database.Context.SupplierInvoices.Add(new SupplierInvoice
        {
            CompanyId = 1,
            PurchaseOrderId = 40,
            InvoiceNumber = "SI-1",
            SupplierName = "Supplier",
            InvoiceDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(10),
            Status = "Posted",
            TotalAmount = 450,
            CreatedDate = DateTime.Now
        });
        await database.Context.SaveChangesAsync();
        var service = new ErpReportService(database.Context);

        var report = await service.BuildReportAsync(1, DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1));

        Assert.Equal(40, report.CostOfGoodsSoldTotal);
        Assert.Equal(40, report.InventoryValue);
        Assert.Equal(1, report.StockAlertCount);
        Assert.Single(report.PurchaseVariance);
        Assert.Equal(500, report.PurchaseVariance[0].OrderedValue);
        Assert.Equal(300, report.PurchaseVariance[0].ReceivedValue);
        Assert.Equal(450, report.PurchaseVariance[0].InvoicedValue);
        Assert.Single(report.CustomerStatements);
        Assert.Equal(70, report.CustomerStatements[0].PosSalesTotal);
        Assert.True(report.AccountsPayableTotal >= 450);
    }

    private static SalesDocument SalesDocument(
        int id,
        string type,
        string number,
        int customerId,
        decimal total,
        decimal paid,
        DateTime dueDate,
        string status,
        string paymentStatus)
    {
        return new SalesDocument
        {
            Id = id,
            CompanyId = 1,
            CustomerId = customerId,
            DocumentType = type,
            DocumentNumber = number,
            DocumentDate = DateTime.Today,
            DueDate = dueDate,
            Status = status,
            PaymentStatus = paymentStatus,
            TotalAmount = total,
            PaidAmount = paid,
            CreatedDate = DateTime.Now
        };
    }

    private static Company Company(int id)
    {
        return new Company
        {
            Id = id,
            DisplayName = $"Company {id}",
            InvoicePrefix = "INV",
            PrimaryColor = "#2563eb",
            CurrencyCode = "USD",
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
    }
}
