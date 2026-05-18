using Microsoft.EntityFrameworkCore;
using WebApplication3.Models;
using WebApplication3.Services;
using WebApplication3.Tests.Support;

namespace WebApplication3.Tests.Services;

public sealed class ErpAccountingServiceTests
{
    [Fact]
    public async Task PostSalesDocumentAsync_PostsBalancedJournalEntryOnce()
    {
        using var database = new TestDatabase();
        database.Context.Companies.Add(Company(1));
        var document = new SalesDocument
        {
            CompanyId = 1,
            DocumentType = "Invoice",
            DocumentNumber = "INV-POST-1",
            DocumentDate = DateTime.Today,
            Status = "Issued",
            SubTotal = 100,
            TaxAmount = 18,
            DiscountAmount = 10,
            TotalAmount = 108,
            CreatedDate = DateTime.Now,
            Lines =
            [
                new SalesDocumentLine
                {
                    Description = "Service",
                    Quantity = 1,
                    UnitPrice = 100,
                    TaxRate = 18,
                    TaxAmount = 18,
                    LineTotal = 118
                }
            ]
        };
        database.Context.SalesDocuments.Add(document);
        await database.Context.SaveChangesAsync();

        var service = new ErpAccountingService(database.Context);

        var first = await service.PostSalesDocumentAsync(document.Id, "tester");
        var second = await service.PostSalesDocumentAsync(document.Id, "tester");

        Assert.NotNull(first);
        Assert.Same(first, second);
        var entry = await database.Context.JournalEntries
            .Include(e => e.Lines)
            .SingleAsync(e => e.SourceType == nameof(SalesDocument) && e.SourceId == document.Id.ToString());
        Assert.Equal(108, entry.TotalDebit);
        Assert.Equal(108, entry.TotalCredit);
        Assert.Equal(3, entry.Lines.Count);
    }

    [Fact]
    public async Task CreateBalancedEntryAsync_RejectsUnbalancedPosting()
    {
        using var database = new TestDatabase();
        database.Context.Companies.Add(Company(1));
        await database.Context.SaveChangesAsync();
        var service = new ErpAccountingService(database.Context);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateBalancedEntryAsync(
                1,
                "Test",
                "1",
                "Bad entry",
                [new JournalPostingLine("1000", "Cash", "Asset", "Bad", 10, 0)]));

        Assert.Contains("balance", error.Message);
    }

    [Fact]
    public async Task CreateBalancedEntryAsync_AssignsOpenFiscalPeriod()
    {
        using var database = new TestDatabase();
        database.Context.Companies.Add(Company(1));
        var period = new FiscalPeriod
        {
            CompanyId = 1,
            Name = "FY Open",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            Status = "Open",
            CreatedDate = DateTime.Now
        };
        database.Context.FiscalPeriods.Add(period);
        await database.Context.SaveChangesAsync();
        var service = new ErpAccountingService(database.Context);

        var entry = await service.CreateBalancedEntryAsync(
            1,
            "Test",
            "OPEN-1",
            "Open period entry",
            [
                new JournalPostingLine("1000", "Cash", "Asset", "Open", 10, 0),
                new JournalPostingLine("3000", "Equity", "Equity", "Open", 0, 10)
            ],
            entryDate: new DateTime(2026, 5, 1));

        Assert.Equal(period.Id, entry.FiscalPeriodId);
        Assert.Equal(new DateTime(2026, 5, 1), entry.EntryDate);
    }

    [Fact]
    public async Task CreateBalancedEntryAsync_RejectsClosedFiscalPeriod()
    {
        using var database = new TestDatabase();
        database.Context.Companies.Add(Company(1));
        database.Context.FiscalPeriods.Add(new FiscalPeriod
        {
            CompanyId = 1,
            Name = "FY Closed",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            Status = "Closed",
            CreatedDate = DateTime.Now
        });
        await database.Context.SaveChangesAsync();
        var service = new ErpAccountingService(database.Context);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateBalancedEntryAsync(
                1,
                "Test",
                "CLOSED-1",
                "Closed period entry",
                [
                    new JournalPostingLine("1000", "Cash", "Asset", "Closed", 10, 0),
                    new JournalPostingLine("3000", "Equity", "Equity", "Closed", 0, 10)
                ],
                entryDate: new DateTime(2026, 5, 1)));

        Assert.Contains("closed", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(database.Context.JournalEntries);
    }

    [Fact]
    public async Task PostSupplierInvoiceAsync_PostsBalancedApJournalEntryOnceAndMarksPosted()
    {
        using var database = new TestDatabase();
        database.Context.Companies.Add(Company(1));
        var invoice = new SupplierInvoice
        {
            CompanyId = 1,
            InvoiceNumber = "SUP-POST-1",
            SupplierName = "Supplier",
            InvoiceDate = DateTime.Today,
            Status = "Draft",
            MatchStatus = "Unmatched",
            SubTotal = 100,
            TaxAmount = 18,
            TotalAmount = 118,
            CreatedDate = DateTime.Now,
            Items =
            [
                new SupplierInvoiceItem
                {
                    Description = "Inventory purchase",
                    Quantity = 1,
                    UnitCost = 100,
                    TaxRate = 18,
                    TaxAmount = 18,
                    LineTotal = 118
                }
            ]
        };
        database.Context.SupplierInvoices.Add(invoice);
        await database.Context.SaveChangesAsync();

        var service = new ErpAccountingService(database.Context);

        var first = await service.PostSupplierInvoiceAsync(invoice.Id, "tester");
        var second = await service.PostSupplierInvoiceAsync(invoice.Id, "tester");

        Assert.NotNull(first);
        Assert.Same(first, second);
        Assert.Equal("Posted", database.Context.SupplierInvoices.Single(i => i.Id == invoice.Id).Status);
        var entry = await database.Context.JournalEntries
            .Include(e => e.Lines)
            .SingleAsync(e => e.SourceType == nameof(SupplierInvoice) && e.SourceId == invoice.Id.ToString());
        Assert.Equal(118, entry.TotalDebit);
        Assert.Equal(118, entry.TotalCredit);
        Assert.Equal(3, entry.Lines.Count);
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
