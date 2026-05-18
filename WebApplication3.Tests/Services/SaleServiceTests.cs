using Microsoft.Extensions.Logging.Abstractions;
using WebApplication3.Models;
using WebApplication3.Services;
using WebApplication3.Tests.Support;

namespace WebApplication3.Tests.Services;

public sealed class SaleServiceTests
{
    [Fact]
    public async Task GetAllSalesAsync_ReturnsOnlyCurrentCompanySales()
    {
        using var database = new TestDatabase();
        database.Context.Companies.AddRange(
            new Company { Id = 1, DisplayName = "Company A", InvoicePrefix = "A", CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now },
            new Company { Id = 2, DisplayName = "Company B", InvoicePrefix = "B", CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now });
        database.Context.Sales.AddRange(
            Sale("SALE-A", 1, "admin@a.test"),
            Sale("SALE-B", 2, "admin@b.test"));
        await database.Context.SaveChangesAsync();

        var service = new SaleService(
            database.Context,
            NullLogger<SaleService>.Instance,
            new FixedCurrentCompanyService(1));

        var sales = await service.GetAllSalesAsync();

        var sale = Assert.Single(sales);
        Assert.Equal("SALE-A", sale.SaleNumber);
    }

    [Fact]
    public async Task CreateSaleAsync_AssignsCurrentCompanyId()
    {
        using var database = new TestDatabase();
        database.Context.Companies.Add(new Company { Id = 3, DisplayName = "Company", InvoicePrefix = "INV", CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now });
        await database.Context.SaveChangesAsync();

        var service = new SaleService(
            database.Context,
            NullLogger<SaleService>.Instance,
            new FixedCurrentCompanyService(3));

        var created = await service.CreateSaleAsync(new Sale
        {
            TotalAmount = 25,
            CashierName = "cashier@test.local",
            PaymentMethod = "Cash",
            Status = "Completed"
        });

        Assert.Equal(3, created.CompanyId);
        Assert.Equal(3, database.Context.Sales.Single().CompanyId);
    }

    private static Sale Sale(string number, int companyId, string cashier)
    {
        return new Sale
        {
            SaleNumber = number,
            CompanyId = companyId,
            SaleDate = DateTime.Now,
            TotalAmount = 10,
            CashierName = cashier,
            PaymentMethod = "Cash",
            Status = "Completed"
        };
    }
}
