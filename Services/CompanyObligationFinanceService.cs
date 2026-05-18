using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Services;

public class CompanyObligationFinanceService(
    ApplicationDbContext context,
    IErpAccountingService accounting) : ICompanyObligationFinanceService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IErpAccountingService _accounting = accounting;

    public async Task<PaymentRecord> MarkPaidAsync(int companyId, ObligationPaymentInput input, string? userName = null)
    {
        if (input.Id <= 0)
        {
            throw new InvalidOperationException("Obligation is required.");
        }

        var obligation = await _context.PayrollObligations
            .FirstOrDefaultAsync(o => o.Id == input.Id && o.CompanyId == companyId);
        if (obligation == null)
        {
            throw new InvalidOperationException("Obligation was not found.");
        }

        if (obligation.IsPaid)
        {
            throw new InvalidOperationException("Obligation is already paid.");
        }

        if (obligation.Amount <= 0)
        {
            throw new InvalidOperationException("Obligation amount must be greater than zero.");
        }

        if (input.FinancialAccountId.HasValue)
        {
            var accountExists = await _context.FinancialAccounts.AnyAsync(a =>
                a.CompanyId == companyId &&
                a.Id == input.FinancialAccountId.Value &&
                a.IsActive);
            if (!accountExists)
            {
                throw new InvalidOperationException("Financial account belongs to another company or is inactive.");
            }
        }

        if (_context.Database.IsRelational())
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var payment = await MarkPaidCoreAsync(companyId, obligation, input, userName);
            await transaction.CommitAsync();
            return payment;
        }

        return await MarkPaidCoreAsync(companyId, obligation, input, userName);
    }

    private async Task<PaymentRecord> MarkPaidCoreAsync(
        int companyId,
        PayrollObligation obligation,
        ObligationPaymentInput input,
        string? userName)
    {
        var paymentDate = input.PaymentDate == default ? DateTime.Today : input.PaymentDate.Date;
        var paymentMethod = string.IsNullOrWhiteSpace(input.PaymentMethod) ? "Cash" : input.PaymentMethod.Trim();

        var payment = new PaymentRecord
        {
            CompanyId = companyId,
            FinancialAccountId = input.FinancialAccountId,
            Direction = "Out",
            PaymentMethod = paymentMethod,
            PaymentDate = paymentDate,
            Amount = obligation.Amount,
            Status = "Completed",
            EntityName = nameof(PayrollObligation),
            EntityId = obligation.Id.ToString(),
            ProviderName = "Manual",
            ProviderStatus = "ReadyForProvider",
            CreatedBy = userName,
            CreatedDate = DateTime.Now
        };
        _context.PaymentRecords.Add(payment);

        if (input.FinancialAccountId.HasValue)
        {
            _context.BankTransactions.Add(new BankTransaction
            {
                CompanyId = companyId,
                FinancialAccountId = input.FinancialAccountId.Value,
                TransactionDate = paymentDate,
                Amount = obligation.Amount,
                TransactionType = "Debit",
                Status = "Unreconciled",
                Description = $"Payment for obligation: {obligation.Description}",
                Reference = input.Reference,
                CreatedDate = DateTime.Now
            });
        }

        obligation.Status = "Paid";
        obligation.PaidDate = paymentDate;
        await _context.SaveChangesAsync();

        var debitAccount = GetDebitAccount(obligation.ObligationType);
        await _accounting.CreateBalancedEntryAsync(
            companyId,
            nameof(PaymentRecord),
            payment.Id.ToString(),
            $"Paid obligation {obligation.Description}",
            [
                new(debitAccount.Code, debitAccount.Name, debitAccount.Type, obligation.Description, obligation.Amount, 0),
                new("1000", "Cash and Bank", "Asset", obligation.Description, 0, obligation.Amount)
            ],
            userName);

        return payment;
    }

    private static (string Code, string Name, string Type) GetDebitAccount(string? obligationType)
    {
        return obligationType?.Trim() switch
        {
            "Payroll" => ("5000", "Payroll Expense", "Expense"),
            "Tax" => ("5200", "Tax Expense", "Expense"),
            "Rent" => ("5300", "Rent Expense", "Expense"),
            "Utilities" => ("5400", "Utilities Expense", "Expense"),
            "Debt" => ("2300", "Loans Payable", "Liability"),
            "Unpaid Invoice" => ("2000", "Accounts Payable", "Liability"),
            "Advance" => ("1150", "Employee Advances", "Asset"),
            _ => ("5900", "Other Business Expense", "Expense")
        };
    }
}
