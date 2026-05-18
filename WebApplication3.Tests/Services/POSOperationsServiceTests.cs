using WebApplication3.Models;
using WebApplication3.Services;
using WebApplication3.Tests.Support;

namespace WebApplication3.Tests.Services;

public sealed class POSOperationsServiceTests
{
    [Fact]
    public async Task SyncOfflineSalesAsync_IsIdempotentForSameClientId()
    {
        using var database = new TestDatabase();
        database.Context.Companies.Add(Company(1));
        database.Context.Products.Add(Product(1, 1, 10));
        await database.Context.SaveChangesAsync();
        var service = Service(database);
        var request = new OfflineSaleSyncInput
        {
            ClientId = "device-1-sale-1",
            QueuedAt = new DateTime(2026, 5, 15, 10, 0, 0),
            Items = [new PosSaleItemInput { ProductId = 1, Quantity = 2 }]
        };

        var first = (await service.SyncOfflineSalesAsync(1, [request], "cashier")).Single();
        var second = (await service.SyncOfflineSalesAsync(1, [request], "cashier")).Single();

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(first.SaleId, second.SaleId);
        Assert.Single(database.Context.Sales);
        Assert.Equal(8, database.Context.Products.Single().Stock);
        Assert.Equal("Synced", database.Context.OfflineSyncRecords.Single().Status);
    }

    [Fact]
    public async Task SyncOfflineSalesAsync_MarksInsufficientStockAsConflict()
    {
        using var database = new TestDatabase();
        database.Context.Companies.Add(Company(1));
        database.Context.Products.Add(Product(1, 1, 1));
        await database.Context.SaveChangesAsync();
        var service = Service(database);

        var result = (await service.SyncOfflineSalesAsync(1, [
            new OfflineSaleSyncInput
            {
                ClientId = "device-1-sale-2",
                Items = [new PosSaleItemInput { ProductId = 1, Quantity = 2 }]
            }
        ], "cashier")).Single();

        Assert.False(result.Success);
        Assert.Equal("Conflict", result.Status);
        Assert.Empty(database.Context.Sales);
        Assert.Equal(1, database.Context.Products.Single().Stock);
        Assert.Equal("Conflict", database.Context.OfflineSyncRecords.Single().Status);
    }

    private static POSOperationsService Service(TestDatabase database)
    {
        return new POSOperationsService(database.Context, new NoOpCustomerService(), new NoOpAuditLogService());
    }

    private static Product Product(int id, int companyId, int stock)
    {
        return new Product
        {
            Id = id,
            CompanyId = companyId,
            Name = $"Product {id}",
            Price = 10,
            CostPrice = 5,
            TaxRate = 0,
            Stock = stock,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
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

    private sealed class NoOpAuditLogService : IAuditLogService
    {
        public Task LogAsync(string action, string entityName, string? entityId = null, string? summary = null, int? companyId = null, string? actorUserId = null, string? actorUserName = null)
        {
            return Task.CompletedTask;
        }

        public Task LogChangeAsync<T>(string action, string entityName, string? entityId, T? before, T? after, string? summary = null, int? companyId = null, string? actorUserId = null, string? actorUserName = null)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpCustomerService : ICustomerService
    {
        public Task<List<Customer>> GetAllCustomersAsync() => Task.FromResult(new List<Customer>());
        public Task<Customer?> GetCustomerByIdAsync(int id) => Task.FromResult<Customer?>(null);
        public Task<Customer> CreateCustomerAsync(Customer customer) => Task.FromResult(customer);
        public Task<Customer> UpdateCustomerAsync(Customer customer) => Task.FromResult(customer);
        public Task<bool> DeleteCustomerAsync(int id, string? deletedBy = null) => Task.FromResult(true);
        public Task<List<Customer>> SearchCustomersAsync(string query) => Task.FromResult(new List<Customer>());
        public Task UpdateCustomerStatsAsync(int customerId, decimal purchaseAmount) => Task.CompletedTask;
    }
}
