using Microsoft.EntityFrameworkCore;
using WebApplication3.Models;
using WebApplication3.Services;
using WebApplication3.Tests.Support;

namespace WebApplication3.Tests.Services;

public sealed class SalesWorkflowServiceTests
{
    [Fact]
    public async Task ConvertDocumentAsync_ConvertsQuoteToInvoiceWithCopiedLines()
    {
        using var database = new TestDatabase();
        database.Context.Companies.Add(Company(1));
        var quote = new SalesDocument
        {
            CompanyId = 1,
            DocumentType = "Quote",
            DocumentNumber = "Q-1",
            DocumentDate = DateTime.Today,
            Status = "Draft",
            PaymentStatus = "NotApplicable",
            SubTotal = 100,
            TaxAmount = 10,
            TotalAmount = 110,
            CreatedDate = DateTime.Now,
            Lines =
            [
                new SalesDocumentLine
                {
                    Description = "Line",
                    Quantity = 1,
                    UnitPrice = 100,
                    TaxRate = 10,
                    TaxAmount = 10,
                    LineTotal = 110
                }
            ]
        };
        database.Context.SalesDocuments.Add(quote);
        await database.Context.SaveChangesAsync();
        var service = new SalesWorkflowService(database.Context, new ErpAccountingService(database.Context));

        var invoice = await service.ConvertDocumentAsync(quote.Id, "Invoice", "tester");

        Assert.Equal("Invoice", invoice.DocumentType);
        Assert.Equal("Unpaid", invoice.PaymentStatus);
        Assert.Equal(quote.Id, invoice.ConvertedFromDocumentId);
        Assert.Single(invoice.Lines);
        Assert.Equal("Closed", quote.Status);
    }

    [Fact]
    public async Task RecordPaymentAsync_UpdatesInvoiceAndCreatesPaymentJournal()
    {
        using var database = new TestDatabase();
        database.Context.Companies.Add(Company(1));
        database.Context.FinancialAccounts.Add(new FinancialAccount
        {
            Id = 10,
            CompanyId = 1,
            Name = "Bank",
            AccountType = "Bank",
            CurrencyCode = "USD",
            IsActive = true,
            CreatedDate = DateTime.Now
        });
        var invoice = new SalesDocument
        {
            CompanyId = 1,
            DocumentType = "Invoice",
            DocumentNumber = "INV-1",
            DocumentDate = DateTime.Today,
            Status = "Issued",
            PaymentStatus = "Unpaid",
            TotalAmount = 50,
            CreatedDate = DateTime.Now
        };
        database.Context.SalesDocuments.Add(invoice);
        await database.Context.SaveChangesAsync();
        var service = new SalesWorkflowService(database.Context, new ErpAccountingService(database.Context));

        var payment = await service.RecordPaymentAsync(new SalesDocumentPaymentInput
        {
            SalesDocumentId = invoice.Id,
            FinancialAccountId = 10,
            Amount = 50,
            PaymentMethod = "Bank",
            PaymentDate = DateTime.Today
        }, "tester");

        Assert.Equal("Paid", invoice.PaymentStatus);
        Assert.Equal("Closed", invoice.Status);
        Assert.Equal(50, invoice.PaidAmount);
        Assert.Equal("Manual", payment.ProviderName);
        Assert.Contains(database.Context.JournalEntries, e => e.SourceType == nameof(PaymentRecord) && e.SourceId == payment.Id.ToString());
        Assert.Single(database.Context.BankTransactions);
    }

    [Fact]
    public async Task ConvertDocumentAsync_CreatesNegativeCreditNoteAndRejectsDuplicate()
    {
        using var database = new TestDatabase();
        database.Context.Companies.Add(Company(1));
        var invoice = new SalesDocument
        {
            CompanyId = 1,
            DocumentType = "Invoice",
            DocumentNumber = "INV-1",
            DocumentDate = DateTime.Today,
            Status = "Closed",
            PaymentStatus = "Paid",
            SubTotal = 100,
            TaxAmount = 10,
            TotalAmount = 110,
            CreatedDate = DateTime.Now,
            Lines =
            [
                new SalesDocumentLine
                {
                    Description = "Line",
                    Quantity = 1,
                    UnitPrice = 100,
                    TaxRate = 10,
                    TaxAmount = 10,
                    LineTotal = 110
                }
            ]
        };
        database.Context.SalesDocuments.Add(invoice);
        await database.Context.SaveChangesAsync();
        var service = new SalesWorkflowService(database.Context, new ErpAccountingService(database.Context));

        var creditNote = await service.ConvertDocumentAsync(invoice.Id, "CreditNote", "tester");
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ConvertDocumentAsync(invoice.Id, "CreditNote", "tester"));

        Assert.Equal("CreditNote", creditNote.DocumentType);
        Assert.Equal(-110, creditNote.TotalAmount);
        Assert.Equal(-10, creditNote.TaxAmount);
        Assert.Single(creditNote.Lines);
        Assert.Equal(-110, creditNote.Lines[0].LineTotal);
        Assert.Contains("already been converted", error.Message);
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
