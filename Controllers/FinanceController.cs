using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers;

[Authorize(Roles = "Admin")]
public class FinanceController(ApplicationDbContext context, ICurrentCompanyService currentCompany, IAuditLogService auditLog) : Controller
{
    private readonly ApplicationDbContext _context = context;
    private readonly ICurrentCompanyService _currentCompany = currentCompany;
    private readonly IAuditLogService _auditLog = auditLog;

    public async Task<IActionResult> Index(DateTime? dateFrom, DateTime? dateTo)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var from = dateFrom?.Date ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var to = dateTo?.Date ?? DateTime.Today;
        var exclusiveTo = to.AddDays(1);

        var sales = await _context.Sales
            .Include(s => s.SaleItems)
            .Where(s => s.CompanyId == companyId && s.SaleDate >= from && s.SaleDate < exclusiveTo)
            .ToListAsync();

        var supplierSpend = await _context.SupplierInvoices
            .Where(i => i.CompanyId == companyId && i.InvoiceDate >= from && i.InvoiceDate < exclusiveTo)
            .SumAsync(i => i.TotalAmount);

        var payrollObligations = await _context.PayrollObligations
            .Where(p => p.CompanyId == companyId &&
                        p.ObligationType == "Payroll" &&
                        p.PeriodStart < exclusiveTo &&
                        p.PeriodEnd >= from)
            .OrderByDescending(p => p.PeriodStart)
            .ThenByDescending(p => p.Id)
            .ToListAsync();

        var payrollLiability = payrollObligations
            .Where(p => !string.Equals(p.Status, "Paid", StringComparison.OrdinalIgnoreCase))
            .Sum(p => p.Amount);

        var paidObligationExpense = await _context.PayrollObligations
            .Where(o => o.CompanyId == companyId &&
                        o.Status == "Paid" &&
                        o.PaidDate.HasValue &&
                        o.PaidDate.Value >= from &&
                        o.PaidDate.Value < exclusiveTo)
            .SumAsync(o => o.Amount);

        var openObligationLiability = await _context.PayrollObligations
            .Where(o => o.CompanyId == companyId && o.Status != "Paid")
            .SumAsync(o => o.Amount);

        var grossRevenue = sales.Sum(s => s.TotalAmount);
        var refundedAmount = sales.Sum(s => s.RefundedAmount);
        var netRevenue = grossRevenue - refundedAmount;
        var costOfGoodsSold = sales.SelectMany(s => s.SaleItems)
            .Sum(i => i.UnitCost * Math.Max(i.Quantity - i.ReturnedQuantity, 0));
        var taxCollected = sales.SelectMany(s => s.SaleItems)
            .Sum(i => i.Quantity == 0 ? 0 : i.TaxAmount / i.Quantity * Math.Max(i.Quantity - i.ReturnedQuantity, 0));
        var grossProfit = netRevenue - costOfGoodsSold;

        var dailyPoints = sales
            .GroupBy(s => s.SaleDate.Date)
            .Select(g =>
            {
                var dayItems = g.SelectMany(s => s.SaleItems).ToList();
                var dayRevenue = g.Sum(s => s.TotalAmount - s.RefundedAmount);
                var dayCost = dayItems.Sum(i => i.UnitCost * Math.Max(i.Quantity - i.ReturnedQuantity, 0));
                return new FinanceDailyPoint
                {
                    Label = g.Key.ToString("MMM dd"),
                    Revenue = dayRevenue,
                    Profit = dayRevenue - dayCost
                };
            })
            .OrderBy(p => p.Label)
            .ToList();

        var userPerformance = sales
            .GroupBy(s => string.IsNullOrWhiteSpace(s.CashierName) ? "Unknown" : s.CashierName)
            .Select(g =>
            {
                var userItems = g.SelectMany(s => s.SaleItems).ToList();
                var userRevenue = g.Sum(s => s.TotalAmount - s.RefundedAmount);
                var userCost = userItems.Sum(i => i.UnitCost * Math.Max(i.Quantity - i.ReturnedQuantity, 0));
                return new FinanceUserPerformance
                {
                    CashierName = g.Key!,
                    SaleCount = g.Count(),
                    Revenue = userRevenue,
                    Profit = userRevenue - userCost
                };
            })
            .OrderByDescending(u => u.Revenue)
            .ToList();

        var monthlyProfitReport = await BuildMonthlyProfitReportAsync(companyId, from, exclusiveTo);

        var model = new FinanceDashboardViewModel
        {
            DateFrom = from,
            DateTo = to,
            PayrollLiability = payrollLiability,
            GrossRevenue = grossRevenue,
            RefundedAmount = refundedAmount,
            NetRevenue = netRevenue,
            CostOfGoodsSold = costOfGoodsSold,
            GrossProfit = grossProfit,
            TaxCollected = taxCollected,
            SupplierSpend = supplierSpend,
            PaidObligationExpense = paidObligationExpense,
            OpenObligationLiability = openObligationLiability,
            NetProfitAfterPayroll = grossProfit - payrollLiability,
            NetProfitAfterPaidObligations = grossProfit - paidObligationExpense,
            SaleCount = sales.Count,
            ReturnCount = sales.Count(s => s.RefundedAmount > 0),
            AverageTicket = sales.Count == 0 ? 0 : netRevenue / sales.Count,
            DailyPoints = dailyPoints,
            UserPerformance = userPerformance,
            MonthlyProfitReport = monthlyProfitReport,
            PayrollObligations = payrollObligations,
            PayrollInput = new PayrollObligationInput
            {
                PeriodStart = from,
                PeriodEnd = to
            }
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPayroll(PayrollObligationInput input, DateTime? dateFrom, DateTime? dateTo)
    {
        if (string.IsNullOrWhiteSpace(input.Description))
        {
            TempData["Error"] = "Payroll description is required.";
            return RedirectToAction(nameof(Index), new { dateFrom, dateTo });
        }

        if (input.Amount <= 0)
        {
            TempData["Error"] = "Payroll amount must be greater than zero.";
            return RedirectToAction(nameof(Index), new { dateFrom, dateTo });
        }

        if (input.PeriodEnd.Date < input.PeriodStart.Date)
        {
            TempData["Error"] = "Payroll period end must be after the start date.";
            return RedirectToAction(nameof(Index), new { dateFrom, dateTo });
        }

        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var payroll = new PayrollObligation
        {
            CompanyId = companyId,
            Description = input.Description.Trim(),
            ObligationType = "Payroll",
            PeriodStart = input.PeriodStart.Date,
            PeriodEnd = input.PeriodEnd.Date,
            DueDate = input.DueDate?.Date,
            Amount = input.Amount,
            Status = string.IsNullOrWhiteSpace(input.Status) ? "Open" : input.Status.Trim(),
            CreatedDate = DateTime.Now,
            CreatedBy = User.Identity?.Name
        };

        _context.PayrollObligations.Add(payroll);
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync("Create", nameof(PayrollObligation), payroll.Id.ToString(), $"Created payroll obligation {payroll.Description}", companyId);
        TempData["Success"] = "Payroll obligation added.";
        return RedirectToAction(nameof(Index), new { dateFrom, dateTo });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPayrollPaid(int id, DateTime? dateFrom, DateTime? dateTo)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var payroll = await _context.PayrollObligations
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);
        if (payroll == null) return NotFound();

        payroll.Status = "Paid";
        payroll.PaidDate = DateTime.Today;
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync("MarkPaid", nameof(PayrollObligation), payroll.Id.ToString(), $"Marked payroll obligation paid: {payroll.Description}", companyId);
        TempData["Success"] = "Payroll obligation marked as paid.";
        return RedirectToAction(nameof(Index), new { dateFrom, dateTo });
    }

    private async Task<List<FinanceMonthlyProfitPoint>> BuildMonthlyProfitReportAsync(int companyId, DateTime from, DateTime exclusiveTo)
    {
        var monthStart = new DateTime(from.Year, from.Month, 1);
        var monthEnd = new DateTime(exclusiveTo.AddDays(-1).Year, exclusiveTo.AddDays(-1).Month, 1);
        var result = new List<FinanceMonthlyProfitPoint>();

        for (var cursor = monthStart; cursor <= monthEnd; cursor = cursor.AddMonths(1))
        {
            var nextMonth = cursor.AddMonths(1);
            var sales = await _context.Sales
                .Include(s => s.SaleItems)
                .Where(s => s.CompanyId == companyId && s.SaleDate >= cursor && s.SaleDate < nextMonth)
                .ToListAsync();

            var revenue = sales.Sum(s => s.TotalAmount - s.RefundedAmount);
            var cogs = sales.SelectMany(s => s.SaleItems)
                .Sum(i => i.UnitCost * Math.Max(i.Quantity - i.ReturnedQuantity, 0));
            var supplierSpend = await _context.SupplierInvoices
                .Where(i => i.CompanyId == companyId && i.InvoiceDate >= cursor && i.InvoiceDate < nextMonth)
                .SumAsync(i => i.TotalAmount);
            var payroll = await _context.PayrollObligations
                .Where(p => p.CompanyId == companyId &&
                            p.ObligationType == "Payroll" &&
                            p.PeriodStart < nextMonth &&
                            p.PeriodEnd >= cursor)
                .SumAsync(p => p.Amount);
            var paidObligations = await _context.PayrollObligations
                .Where(o => o.CompanyId == companyId &&
                            o.Status == "Paid" &&
                            o.PaidDate.HasValue &&
                            o.PaidDate.Value >= cursor &&
                            o.PaidDate.Value < nextMonth)
                .SumAsync(o => o.Amount);

            result.Add(new FinanceMonthlyProfitPoint
            {
                Month = cursor.ToString("MMM yyyy"),
                Revenue = revenue,
                CostOfGoodsSold = cogs,
                SupplierSpend = supplierSpend,
                Payroll = payroll,
                PaidObligations = paidObligations,
                NetProfit = revenue - cogs - paidObligations
            });
        }

        return result;
    }
}
