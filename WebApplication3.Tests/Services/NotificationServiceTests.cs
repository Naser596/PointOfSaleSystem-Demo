using Microsoft.EntityFrameworkCore;
using WebApplication3.Models;
using WebApplication3.Services;
using WebApplication3.Tests.Support;

namespace WebApplication3.Tests.Services;

public sealed class NotificationServiceTests
{
    [Fact]
    public async Task GenerateInvoiceAndOverdueRemindersAsync_UsesDocumentDateWhenDueDateIsEmpty()
    {
        using var database = new TestDatabase();
        database.Context.Companies.Add(new Company { Id = 1, DisplayName = "Tenant", IsActive = true });
        database.Context.Customers.Add(new Customer
        {
            Id = 10,
            CompanyId = 1,
            Name = "Acme",
            Email = "billing@acme.test",
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        });
        await database.Context.SaveChangesAsync();
        database.Context.SalesDocuments.Add(new SalesDocument
        {
            Id = 20,
            CompanyId = 1,
            DocumentType = "Invoice",
            DocumentNumber = "INV-20",
            CustomerId = 10,
            DocumentDate = DateTime.Today,
            DueDate = null,
            Status = "Issued",
            PaymentStatus = "Unpaid",
            TotalAmount = 100,
            PaidAmount = 0,
            CreatedDate = DateTime.Now
        });
        await database.Context.SaveChangesAsync();
        var service = new NotificationService(database.Context);

        var created = await service.GenerateInvoiceAndOverdueRemindersAsync(1);

        Assert.Equal(1, created);
        var notification = await database.Context.NotificationMessages.SingleAsync();
        Assert.Equal("InvoiceReminder", notification.NotificationType);
        Assert.Equal("billing@acme.test", notification.Recipient);
    }

    [Fact]
    public async Task GenerateInvoiceAndOverdueRemindersAsync_CreatesOverdueSmsWhenDocumentDateIsPast()
    {
        using var database = new TestDatabase();
        database.Context.Companies.Add(new Company { Id = 1, DisplayName = "Tenant", IsActive = true });
        database.Context.Customers.Add(new Customer
        {
            Id = 10,
            CompanyId = 1,
            Name = "Acme",
            Email = "billing@acme.test",
            Phone = "+38344111222",
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        });
        await database.Context.SaveChangesAsync();
        database.Context.SalesDocuments.Add(new SalesDocument
        {
            Id = 20,
            CompanyId = 1,
            DocumentType = "Invoice",
            DocumentNumber = "INV-20",
            CustomerId = 10,
            DocumentDate = DateTime.Today.AddDays(-1),
            DueDate = null,
            Status = "Issued",
            PaymentStatus = "Unpaid",
            TotalAmount = 100,
            PaidAmount = 0,
            CreatedDate = DateTime.Now
        });
        await database.Context.SaveChangesAsync();
        var service = new NotificationService(database.Context);

        var created = await service.GenerateInvoiceAndOverdueRemindersAsync(1);

        Assert.Equal(2, created);
        var notifications = await database.Context.NotificationMessages.ToListAsync();
        Assert.All(notifications, n => Assert.Equal("OverdueCustomerReminder", n.NotificationType));
        Assert.Contains(notifications, n => n.Channel == "Email");
        Assert.Contains(notifications, n => n.Channel == "SMS");
    }
}
