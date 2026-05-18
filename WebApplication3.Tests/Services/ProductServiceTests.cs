using Microsoft.Extensions.Logging.Abstractions;
using WebApplication3.Models;
using WebApplication3.Services;
using WebApplication3.Tests.Support;

namespace WebApplication3.Tests.Services;

public sealed class ProductServiceTests
{
    [Fact]
    public async Task GetAllProductsAsync_ReturnsOnlyCurrentCompanyActiveProducts()
    {
        using var database = new TestDatabase();
        database.Context.Companies.AddRange(
            new Company { Id = 1, DisplayName = "Company A", InvoicePrefix = "A", CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now },
            new Company { Id = 2, DisplayName = "Company B", InvoicePrefix = "B", CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now });
        database.Context.Products.AddRange(
            Product("Visible", 1, isActive: true),
            Product("Inactive", 1, isActive: false),
            Product("Other Company", 2, isActive: true));
        await database.Context.SaveChangesAsync();

        var service = new ProductService(
            database.Context,
            NullLogger<ProductService>.Instance,
            new FixedCurrentCompanyService(1));

        var products = await service.GetAllProductsAsync();

        var product = Assert.Single(products);
        Assert.Equal("Visible", product.Name);
    }

    [Fact]
    public async Task AddProductAsync_AssignsCurrentCompanyId()
    {
        using var database = new TestDatabase();
        database.Context.Companies.Add(new Company { Id = 7, DisplayName = "Company", InvoicePrefix = "INV", CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now });
        await database.Context.SaveChangesAsync();

        var service = new ProductService(
            database.Context,
            NullLogger<ProductService>.Instance,
            new FixedCurrentCompanyService(7));

        var created = await service.AddProductAsync(Product("New", 0, isActive: true));

        Assert.Equal(7, created.CompanyId);
        Assert.Equal(7, database.Context.Products.Single(p => p.Name == "New").CompanyId);
    }

    private static Product Product(string name, int companyId, bool isActive)
    {
        return new Product
        {
            Name = name,
            Description = name,
            CompanyId = companyId,
            SKU = $"{name}-SKU",
            Price = 10,
            Stock = 5,
            MinStock = 2,
            IsActive = isActive,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
    }
}
