using WebApplication3.Models;
using WebApplication3.Services;
using WebApplication3.Tests.Support;

namespace WebApplication3.Tests.Services;

public sealed class CompanyObligationFinanceServiceTests
{
    [Fact]
    public async Task MarkPaidAsync_PostsPaymentBankTransactionAndJournalEntry()
    {
        using var database = new TestDatabase();
        database.Context.Companies.Add(Company(1));
        database.Context.FinancialAccounts.Add(new FinancialAccount
        {
            Id = 10,
            CompanyId = 1,
            Name = "Main Bank",
            AccountType = "Bank",
            CurrencyCode = "USD",
            IsActive = true,
            CreatedDate = DateTime.Now
        });
        var obligation = new PayrollObligation
        {
            CompanyId = 1,
            Description = "May payroll",
            ObligationType = "Payroll",
            PayeeName = "Employees",
            PeriodStart = new DateTime(2026, 5, 1),
            PeriodEnd = new DateTime(2026, 5, 31),
            DueDate = new DateTime(2026, 6, 5),
            Amount = 1200,
            Status = "Open",
            CreatedDate = DateTime.Now
        };
        database.Context.PayrollObligations.Add(obligation);
        await database.Context.SaveChangesAsync();
        var service = new CompanyObligationFinanceService(database.Context, new ErpAccountingService(database.Context));

        var payment = await service.MarkPaidAsync(1, new ObligationPaymentInput
        {
            Id = obligation.Id,
            FinancialAccountId = 10,
            PaymentDate = new DateTime(2026, 6, 5),
            PaymentMethod = "Bank"
        }, "tester");

        Assert.Equal("Paid", obligation.Status);
        Assert.Equal(new DateTime(2026, 6, 5), obligation.PaidDate);
        Assert.Equal("Out", payment.Direction);
        Assert.Equal(nameof(PayrollObligation), payment.EntityName);
        Assert.Single(database.Context.BankTransactions);
        Assert.Contains(database.Context.JournalEntries, e => e.SourceType == nameof(PaymentRecord) && e.SourceId == payment.Id.ToString());
    }

    [Fact]
    public async Task MarkPaidAsync_RejectsAlreadyPaidObligation()
    {
        using var database = new TestDatabase();
        database.Context.Companies.Add(Company(1));
        database.Context.PayrollObligations.Add(new PayrollObligation
        {
            Id = 20,
            CompanyId = 1,
            Description = "Paid rent",
            ObligationType = "Rent",
            PeriodStart = DateTime.Today,
            PeriodEnd = DateTime.Today,
            Amount = 500,
            Status = "Paid",
            PaidDate = DateTime.Today,
            CreatedDate = DateTime.Now
        });
        await database.Context.SaveChangesAsync();
        var service = new CompanyObligationFinanceService(database.Context, new ErpAccountingService(database.Context));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.MarkPaidAsync(1, new ObligationPaymentInput { Id = 20 }, "tester"));

        Assert.Contains("already paid", error.Message);
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
