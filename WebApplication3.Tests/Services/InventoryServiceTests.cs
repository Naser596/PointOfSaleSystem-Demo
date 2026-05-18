using WebApplication3.Models;
using WebApplication3.Services;
using WebApplication3.Tests.Support;

namespace WebApplication3.Tests.Services;

public sealed class InventoryServiceTests
{
    [Fact]
    public async Task GetLowStockProductsAsync_ReturnsOnlyCurrentCompanyLowStock()
    {
        using var database = new TestDatabase();
        database.Context.Companies.AddRange(
            new Company { Id = 1, DisplayName = "Company A", InvoicePrefix = "A", CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now },
            new Company { Id = 2, DisplayName = "Company B", InvoicePrefix = "B", CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now });
        database.Context.Products.AddRange(
            Product("Low", 1, stock: 2, minStock: 5),
            Product("Enough", 1, stock: 10, minStock: 5),
            Product("Other Low", 2, stock: 1, minStock: 5));
        await database.Context.SaveChangesAsync();

        var service = new InventoryService(database.Context, new FixedCurrentCompanyService(1));

        var products = await service.GetLowStockProductsAsync();

        var product = Assert.Single(products);
        Assert.Equal("Low", product.Name);
    }

    [Fact]
    public async Task RestockProductAsync_DoesNotRestockProductFromAnotherCompany()
    {
        using var database = new TestDatabase();
        database.Context.Companies.AddRange(
            new Company { Id = 1, DisplayName = "Company A", InvoicePrefix = "A", CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now },
            new Company { Id = 2, DisplayName = "Company B", InvoicePrefix = "B", CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now });
        var otherCompanyProduct = Product("Other", 2, stock: 1, minStock: 5);
        database.Context.Products.Add(otherCompanyProduct);
        await database.Context.SaveChangesAsync();

        var service = new InventoryService(database.Context, new FixedCurrentCompanyService(1));

        var restocked = await service.RestockProductAsync(otherCompanyProduct.Id, 10, "test", "admin@test.local");

        Assert.False(restocked);
        Assert.Equal(1, database.Context.Products.Single(p => p.Id == otherCompanyProduct.Id).Stock);
        Assert.Empty(database.Context.StockMovements);
    }

    private static Product Product(string name, int companyId, int stock, int minStock)
    {
        return new Product
        {
            Name = name,
            Description = name,
            CompanyId = companyId,
            SKU = $"{name}-SKU",
            Price = 10,
            Stock = stock,
            MinStock = minStock,
            IsActive = true,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
    }
}
