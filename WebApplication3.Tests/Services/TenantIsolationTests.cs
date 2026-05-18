using Microsoft.EntityFrameworkCore;
using WebApplication3.Models;
using WebApplication3.Tests.Support;

namespace WebApplication3.Tests.Services;

public sealed class TenantIsolationTests
{
    [Fact]
    public async Task SaveChangesAsync_RejectsBusinessDataWithoutCompany()
    {
        using var database = new TestDatabase();
        database.Context.Products.Add(new Product
        {
            Name = "No Tenant",
            Description = "Invalid",
            SKU = "NO-TENANT",
            Price = 10,
            Stock = 1,
            IsActive = true,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => database.Context.SaveChangesAsync());
        Assert.Contains("must belong to a company", error.Message);
    }

    [Fact]
    public async Task SaveChangesAsync_RejectsSaleItemWithProductFromAnotherCompany()
    {
        using var database = new TestDatabase();
        database.Context.Companies.AddRange(
            Company(1, "Company A"),
            Company(2, "Company B"));
        database.Context.Products.Add(new Product
        {
            Id = 10,
            CompanyId = 2,
            Name = "Other Tenant Product",
            Description = "Other",
            SKU = "OTHER-TENANT",
            Price = 10,
            Stock = 5,
            IsActive = true,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        });
        await database.Context.SaveChangesAsync();

        database.Context.Sales.Add(new Sale
        {
            CompanyId = 1,
            SaleNumber = "SALE-CROSS",
            SaleDate = DateTime.Now,
            PaymentMethod = "Cash",
            Status = "Completed",
            TotalAmount = 10,
            SaleItems =
            [
                new SaleItem
                {
                    ProductId = 10,
                    Quantity = 1,
                    UnitPrice = 10,
                    TotalPrice = 10
                }
            ]
        });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => database.Context.SaveChangesAsync());
        Assert.Contains("belongs to another company", error.Message);
    }

    private static Company Company(int id, string name)
    {
        return new Company
        {
            Id = id,
            DisplayName = name,
            InvoicePrefix = "INV",
            PrimaryColor = "#2563eb",
            CurrencyCode = "USD",
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
    }
}

