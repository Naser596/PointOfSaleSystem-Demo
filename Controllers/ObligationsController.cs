using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers;

[Authorize(Roles = "Admin,Accountant,Manager")]
public class ObligationsController(
    ApplicationDbContext context,
    ICurrentCompanyService currentCompany,
    IAuditLogService auditLog,
    ICompanyObligationFinanceService obligationFinance) : Controller
{
    private readonly ApplicationDbContext _context = context;
    private readonly ICurrentCompanyService _currentCompany = currentCompany;
    private readonly IAuditLogService _auditLog = auditLog;
    private readonly ICompanyObligationFinanceService _obligationFinance = obligationFinance;

    public async Task<IActionResult> Index(string? status = "Open", string? type = null)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var query = _context.PayrollObligations
            .Where(o => o.CompanyId == companyId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(o => o.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(type) && !string.Equals(type, "All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(o => o.ObligationType == type);
        }

        var obligations = await query
            .OrderBy(o => o.Status == "Paid")
            .ThenBy(o => o.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(o => o.Id)
            .ToListAsync();

        var allCompanyObligations = await _context.PayrollObligations
            .Where(o => o.CompanyId == companyId)
            .ToListAsync();

        var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var nextMonth = monthStart.AddMonths(1);
        var model = new CompanyObligationsViewModel
        {
            Status = status,
            Type = type,
            Obligations = obligations,
            FinancialAccounts = await _context.FinancialAccounts
                .Where(a => a.CompanyId == companyId && a.IsActive)
                .OrderBy(a => a.Name)
                .ToListAsync(),
            OpenTotal = allCompanyObligations.Where(o => !o.IsPaid).Sum(o => o.Amount),
            OverdueTotal = allCompanyObligations.Where(o => o.IsOverdue).Sum(o => o.Amount),
            DueThisMonthTotal = allCompanyObligations
                .Where(o => !o.IsPaid && o.DueDate >= monthStart && o.DueDate < nextMonth)
                .Sum(o => o.Amount),
            PaidThisMonthTotal = allCompanyObligations
                .Where(o => o.PaidDate >= monthStart && o.PaidDate < nextMonth)
                .Sum(o => o.Amount)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CompanyObligationInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Description))
        {
            TempData["Error"] = "Description is required.";
            return RedirectToAction(nameof(Index));
        }

        if (input.Amount <= 0)
        {
            TempData["Error"] = "Amount must be greater than zero.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var dueDate = input.DueDate?.Date ?? DateTime.Today;
        var isPaid = string.Equals(input.Status, "Paid", StringComparison.OrdinalIgnoreCase);

        var obligation = new PayrollObligation
        {
            CompanyId = companyId,
            Description = input.Description.Trim(),
            ObligationType = string.IsNullOrWhiteSpace(input.ObligationType) ? "Other" : input.ObligationType.Trim(),
            PayeeName = string.IsNullOrWhiteSpace(input.PayeeName) ? null : input.PayeeName.Trim(),
            PeriodStart = dueDate,
            PeriodEnd = dueDate,
            DueDate = dueDate,
            Amount = input.Amount,
            Status = string.IsNullOrWhiteSpace(input.Status) ? "Open" : input.Status.Trim(),
            PaidDate = isPaid ? DateTime.Today : null,
            Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
            CreatedDate = DateTime.Now,
            CreatedBy = User.Identity?.Name
        };

        _context.PayrollObligations.Add(obligation);
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync("Create", nameof(PayrollObligation), obligation.Id.ToString(), $"Created obligation {obligation.Description}", companyId);
        TempData["Success"] = "Company obligation added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPaid(ObligationPaymentInput input)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();

        try
        {
            var payment = await _obligationFinance.MarkPaidAsync(companyId, input, User.Identity?.Name);
            await _auditLog.LogAsync("MarkPaid", nameof(PayrollObligation), input.Id.ToString(), $"Marked obligation paid with payment {payment.Amount:N2}", companyId);
            TempData["Success"] = "Obligation marked as paid and posted to finance.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkOpen(int id)
    {
        var obligation = await FindCompanyObligationAsync(id);
        if (obligation == null) return NotFound();

        var hasFinancePayment = await _context.PaymentRecords.AnyAsync(p =>
            p.CompanyId == obligation.CompanyId &&
            p.EntityName == nameof(PayrollObligation) &&
            p.EntityId == obligation.Id.ToString() &&
            p.Status == "Completed");
        if (hasFinancePayment)
        {
            TempData["Error"] = "This obligation has a posted finance payment and cannot be reopened without a reversal entry.";
            return RedirectToAction(nameof(Index));
        }

        obligation.Status = "Open";
        obligation.PaidDate = null;
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync("Reopen", nameof(PayrollObligation), obligation.Id.ToString(), $"Reopened obligation: {obligation.Description}", obligation.CompanyId);

        TempData["Success"] = "Obligation reopened.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var obligation = await FindCompanyObligationAsync(id);
        if (obligation == null) return NotFound();

        _context.PayrollObligations.Remove(obligation);
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync("Delete", nameof(PayrollObligation), obligation.Id.ToString(), $"Deleted obligation: {obligation.Description}", obligation.CompanyId);

        TempData["Success"] = "Obligation deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<PayrollObligation?> FindCompanyObligationAsync(int id)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        return await _context.PayrollObligations
            .FirstOrDefaultAsync(o => o.Id == id && o.CompanyId == companyId);
    }
}
