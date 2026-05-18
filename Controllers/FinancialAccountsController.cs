using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers;

[Authorize(Roles = "Admin,Accountant")]
public class FinancialAccountsController(
    ApplicationDbContext context,
    ICurrentCompanyService currentCompany,
    IAuditLogService auditLog) : Controller
{
    private readonly ApplicationDbContext _context = context;
    private readonly ICurrentCompanyService _currentCompany = currentCompany;
    private readonly IAuditLogService _auditLog = auditLog;

    public async Task<IActionResult> Index()
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var model = new FinancialAccountsDashboardViewModel
        {
            Accounts = await _context.FinancialAccounts
                .Where(a => a.CompanyId == companyId)
                .OrderBy(a => a.Name)
                .ToListAsync(),
            BankTransactions = await _context.BankTransactions
                .Include(t => t.FinancialAccount)
                .Where(t => t.CompanyId == companyId)
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.Id)
                .Take(50)
                .ToListAsync(),
            PaymentRecords = await _context.PaymentRecords
                .Include(p => p.FinancialAccount)
                .Where(p => p.CompanyId == companyId)
                .OrderByDescending(p => p.PaymentDate)
                .ThenByDescending(p => p.Id)
                .Take(50)
                .ToListAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAccount(FinancialAccountInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            TempData["Error"] = "Account name is required.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var name = input.Name.Trim();
        var exists = await _context.FinancialAccounts.AnyAsync(a => a.CompanyId == companyId && a.Name == name);
        if (exists)
        {
            TempData["Error"] = "A financial account with this name already exists.";
            return RedirectToAction(nameof(Index));
        }

        var account = new FinancialAccount
        {
            CompanyId = companyId,
            Name = name,
            AccountType = string.IsNullOrWhiteSpace(input.AccountType) ? "Cash" : input.AccountType.Trim(),
            AccountNumber = string.IsNullOrWhiteSpace(input.AccountNumber) ? null : input.AccountNumber.Trim(),
            CurrencyCode = string.IsNullOrWhiteSpace(input.CurrencyCode) ? "USD" : input.CurrencyCode.Trim().ToUpperInvariant(),
            OpeningBalance = input.OpeningBalance,
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        _context.FinancialAccounts.Add(account);
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync("Create", nameof(FinancialAccount), account.Id.ToString(), $"Created financial account {account.Name}", companyId);

        TempData["Success"] = "Financial account created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTransaction(BankTransactionInput input)
    {
        if (input.FinancialAccountId <= 0 || input.Amount <= 0)
        {
            TempData["Error"] = "Financial account and positive amount are required.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var accountExists = await _context.FinancialAccounts
            .AnyAsync(a => a.CompanyId == companyId && a.Id == input.FinancialAccountId);
        if (!accountExists) return NotFound();

        var transaction = new BankTransaction
        {
            CompanyId = companyId,
            FinancialAccountId = input.FinancialAccountId,
            TransactionDate = input.TransactionDate.Date,
            Amount = input.Amount,
            TransactionType = string.IsNullOrWhiteSpace(input.TransactionType) ? "Debit" : input.TransactionType.Trim(),
            Status = string.IsNullOrWhiteSpace(input.Status) ? "Unreconciled" : input.Status.Trim(),
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            Reference = string.IsNullOrWhiteSpace(input.Reference) ? null : input.Reference.Trim(),
            CreatedDate = DateTime.Now
        };

        _context.BankTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync("Create", nameof(BankTransaction), transaction.Id.ToString(), $"Created bank transaction {transaction.Reference ?? transaction.Id.ToString()}", companyId);

        TempData["Success"] = "Bank transaction created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkReconciled(int id)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var transaction = await _context.BankTransactions
            .FirstOrDefaultAsync(t => t.CompanyId == companyId && t.Id == id);
        if (transaction == null) return NotFound();

        var before = new { transaction.Id, transaction.Status };
        transaction.Status = "Reconciled";
        await _context.SaveChangesAsync();
        await _auditLog.LogChangeAsync(
            "Reconcile",
            nameof(BankTransaction),
            transaction.Id.ToString(),
            before,
            new { transaction.Id, transaction.Status },
            $"Reconciled bank transaction {transaction.Reference ?? transaction.Id.ToString()}",
            companyId);

        TempData["Success"] = "Bank transaction reconciled.";
        return RedirectToAction(nameof(Index));
    }
}
