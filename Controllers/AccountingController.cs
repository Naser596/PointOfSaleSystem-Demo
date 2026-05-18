using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers;

[Authorize(Roles = "Admin,Accountant")]
public class AccountingController(
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
        var model = new AccountingDashboardViewModel
        {
            Accounts = await _context.ChartOfAccounts
                .Where(a => a.CompanyId == companyId)
                .OrderBy(a => a.Code)
                .ToListAsync(),
            FiscalPeriods = await _context.FiscalPeriods
                .Where(p => p.CompanyId == companyId)
                .OrderByDescending(p => p.StartDate)
                .ToListAsync(),
            RecentJournalEntries = await _context.JournalEntries
                .Include(j => j.Lines)
                    .ThenInclude(l => l.Account)
                .Where(j => j.CompanyId == companyId)
                .OrderByDescending(j => j.EntryDate)
                .ThenByDescending(j => j.Id)
                .Take(20)
                .ToListAsync()
        };
        var reportRows = await _context.JournalEntryLines
            .Include(l => l.Account)
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry.CompanyId == companyId && l.JournalEntry.Status == "Posted")
            .GroupBy(l => new { l.Account.Code, l.Account.Name, l.Account.AccountType })
            .Select(g => new AccountingReportRow
            {
                AccountCode = g.Key.Code,
                AccountName = g.Key.Name,
                AccountType = g.Key.AccountType,
                Debit = g.Sum(l => l.Debit),
                Credit = g.Sum(l => l.Credit)
            })
            .OrderBy(r => r.AccountCode)
            .ToListAsync();
        foreach (var row in reportRows)
        {
            row.Balance = row.AccountType is "Asset" or "Expense"
                ? row.Debit - row.Credit
                : row.Credit - row.Debit;
        }

        model.TrialBalance = reportRows;
        model.ProfitAndLoss = reportRows.Where(r => r.AccountType is "Revenue" or "Expense").ToList();
        model.BalanceSheet = reportRows.Where(r => r.AccountType is "Asset" or "Liability" or "Equity").ToList();
        model.TotalDebits = reportRows.Sum(r => r.Debit);
        model.TotalCredits = reportRows.Sum(r => r.Credit);
        var revenue = reportRows.Where(r => r.AccountType == "Revenue").Sum(r => r.Balance);
        var expenses = reportRows.Where(r => r.AccountType == "Expense").Sum(r => r.Balance);
        model.NetIncome = revenue - expenses;
        model.AssetsTotal = reportRows.Where(r => r.AccountType == "Asset").Sum(r => r.Balance);
        model.LiabilitiesTotal = reportRows.Where(r => r.AccountType == "Liability").Sum(r => r.Balance);
        model.EquityTotal = reportRows.Where(r => r.AccountType == "Equity").Sum(r => r.Balance);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAccount(ChartOfAccountInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Code) || string.IsNullOrWhiteSpace(input.Name))
        {
            TempData["Error"] = "Account code and name are required.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var code = input.Code.Trim().ToUpperInvariant();
        var exists = await _context.ChartOfAccounts.AnyAsync(a => a.CompanyId == companyId && a.Code == code);
        if (exists)
        {
            TempData["Error"] = "An account with this code already exists.";
            return RedirectToAction(nameof(Index));
        }

        if (input.ParentAccountId.HasValue)
        {
            var parentExists = await _context.ChartOfAccounts
                .AnyAsync(a => a.Id == input.ParentAccountId.Value && a.CompanyId == companyId);
            if (!parentExists) return NotFound();
        }

        var account = new ChartOfAccount
        {
            CompanyId = companyId,
            Code = code,
            Name = input.Name.Trim(),
            AccountType = string.IsNullOrWhiteSpace(input.AccountType) ? "Asset" : input.AccountType.Trim(),
            ParentAccountId = input.ParentAccountId,
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        _context.ChartOfAccounts.Add(account);
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync("Create", nameof(ChartOfAccount), account.Id.ToString(), $"Created account {account.Code} - {account.Name}", companyId);

        TempData["Success"] = "Account created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFiscalPeriod(FiscalPeriodInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            TempData["Error"] = "Fiscal period name is required.";
            return RedirectToAction(nameof(Index));
        }

        if (input.EndDate.Date < input.StartDate.Date)
        {
            TempData["Error"] = "Fiscal period end date must be after the start date.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var period = new FiscalPeriod
        {
            CompanyId = companyId,
            Name = input.Name.Trim(),
            StartDate = input.StartDate.Date,
            EndDate = input.EndDate.Date,
            Status = "Open",
            CreatedDate = DateTime.Now
        };

        _context.FiscalPeriods.Add(period);
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync("Create", nameof(FiscalPeriod), period.Id.ToString(), $"Created fiscal period {period.Name}", companyId);

        TempData["Success"] = "Fiscal period created.";
        return RedirectToAction(nameof(Index));
    }
}
