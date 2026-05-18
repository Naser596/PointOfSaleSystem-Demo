using Microsoft.EntityFrameworkCore;
using WebApplication3.Models;
using WebApplication3.Services;
using WebApplication3.Tests.Support;

namespace WebApplication3.Tests.Services;

public sealed class SupplierInvoiceMatchingTests
{
    [Fact]
    public async Task CreateAsync_MatchesPurchaseOrderAndGoodsReceipt()
    {
        using var database = new TestDatabase();
        database.Context.Companies.Add(Company(1));
        var order = new PurchaseOrder
        {
            CompanyId = 1,
            OrderNumber = "PO-MATCH",
            SupplierName = "Supplier",
            OrderDate = DateTime.Today,
            Status = "Received",
            TotalAmount = 25,
            CreatedDate = DateTime.Now
        };
        database.Context.PurchaseOrders.Add(order);
        await database.Context.SaveChangesAsync();

        var receipt = new GoodsReceipt
        {
            CompanyId = 1,
            PurchaseOrderId = order.Id,
            ReceiptNumber = "GR-MATCH",
            ReceiptDate = DateTime.Today,
            Status = "Posted",
            CreatedDate = DateTime.Now
        };
        database.Context.GoodsReceipts.Add(receipt);
        await database.Context.SaveChangesAsync();

        var service = new SupplierInvoiceService(database.Context, new FixedCurrentCompanyService(1));

        var invoice = await service.CreateAsync(new SupplierInvoiceCreateViewModel
        {
            PurchaseOrderId = order.Id,
            GoodsReceiptId = receipt.Id,
            SupplierName = "Supplier",
            InvoiceDate = DateTime.Today,
            Items =
            [
                new SupplierInvoiceItemInput
                {
                    Description = "Matched product",
                    Quantity = 5,
                    UnitCost = 5
                }
            ]
        });

        Assert.Equal("Matched", invoice.MatchStatus);
        Assert.Equal(order.Id, invoice.PurchaseOrderId);
        Assert.Equal(receipt.Id, invoice.GoodsReceiptId);
        Assert.Equal("Invoiced", database.Context.PurchaseOrders.Single(p => p.Id == order.Id).Status);
    }

    [Fact]
    public async Task CreateAsync_RejectsReceiptFromDifferentPurchaseOrder()
    {
        using var database = new TestDatabase();
        database.Context.Companies.Add(Company(1));
        var selectedOrder = new PurchaseOrder
        {
            CompanyId = 1,
            OrderNumber = "PO-1",
            SupplierName = "Supplier",
            OrderDate = DateTime.Today,
            Status = "Received",
            CreatedDate = DateTime.Now
        };
        var otherOrder = new PurchaseOrder
        {
            CompanyId = 1,
            OrderNumber = "PO-2",
            SupplierName = "Supplier",
            OrderDate = DateTime.Today,
            Status = "Received",
            CreatedDate = DateTime.Now
        };
        database.Context.PurchaseOrders.AddRange(selectedOrder, otherOrder);
        await database.Context.SaveChangesAsync();
        var receipt = new GoodsReceipt
        {
            CompanyId = 1,
            PurchaseOrderId = otherOrder.Id,
            ReceiptNumber = "GR-OTHER",
            ReceiptDate = DateTime.Today,
            Status = "Posted",
            CreatedDate = DateTime.Now
        };
        database.Context.GoodsReceipts.Add(receipt);
        await database.Context.SaveChangesAsync();
        var service = new SupplierInvoiceService(database.Context, new FixedCurrentCompanyService(1));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new SupplierInvoiceCreateViewModel
            {
                PurchaseOrderId = selectedOrder.Id,
                GoodsReceiptId = receipt.Id,
                SupplierName = "Supplier",
                InvoiceDate = DateTime.Today,
                Items =
                [
                    new SupplierInvoiceItemInput
                    {
                        Description = "Invalid match",
                        Quantity = 1,
                        UnitCost = 1
                    }
                ]
            }));

        Assert.Contains("does not belong", error.Message);
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
