using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;

namespace WebApplication3.Tests.Support;

public sealed class TestDatabase : IDisposable
{
    public TestDatabase()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"pos-service-tests-{Guid.NewGuid():N}")
            .Options;

        Context = new ApplicationDbContext(options);
        Context.Database.EnsureCreated();
    }

    public ApplicationDbContext Context { get; }

    public void Dispose()
    {
        Context.Dispose();
    }
}
