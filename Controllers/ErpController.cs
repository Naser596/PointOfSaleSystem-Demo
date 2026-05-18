using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers;

[Authorize(Roles = "Admin")]
public class ErpController(ApplicationDbContext context, ICurrentCompanyService currentCompany) : Controller
{
    private readonly ApplicationDbContext _context = context;
    private readonly ICurrentCompanyService _currentCompany = currentCompany;

    public async Task<IActionResult> Index()
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var today = DateTime.Today;
        var alerts = new List<ErpAlertViewModel>();

        alerts.AddRange(await _context.PurchaseOrders
            .Where(p => p.CompanyId == companyId &&
                p.ExpectedDate.HasValue &&
                p.ExpectedDate.Value.Date <= today &&
                p.Status != "Received" &&
                p.Status != "Cancelled")
            .OrderBy(p => p.ExpectedDate)
            .Take(5)
            .Select(p => new ErpAlertViewModel
            {
                Title = "Goods expected",
                Message = $"{p.OrderNumber} from {p.SupplierName} is due for receiving.",
                Severity = p.ExpectedDate.GetValueOrDefault().Date < today ? "Danger" : "Warning",
                Icon = "fa-truck-ramp-box",
                Url = $"/Purchasing/Details/{p.Id}",
                DueDate = p.ExpectedDate
            })
            .ToListAsync());

        alerts.AddRange(await _context.Products
            .Where(p => p.CompanyId == companyId && p.IsActive && p.Stock <= p.MinStock)
            .OrderBy(p => p.Stock)
            .Take(5)
            .Select(p => new ErpAlertViewModel
            {
                Title = "Low stock",
                Message = $"{p.Name} has {p.Stock} on hand. Minimum is {p.MinStock}.",
                Severity = "Warning",
                Icon = "fa-box",
                Url = "/Products"
            })
            .ToListAsync());

        alerts.AddRange(await _context.SalesDocuments
            .Where(d => d.CompanyId == companyId &&
                d.DueDate.HasValue &&
                d.DueDate.Value.Date < today &&
                d.Status != "Closed" &&
                d.Status != "Cancelled")
            .OrderBy(d => d.DueDate)
            .Take(5)
            .Select(d => new ErpAlertViewModel
            {
                Title = "Sales document overdue",
                Message = $"{d.DocumentNumber} is past due.",
                Severity = "Danger",
                Icon = "fa-file-invoice",
                Url = $"/SalesDocuments/Details/{d.Id}",
                DueDate = d.DueDate
            })
            .ToListAsync());

        alerts.AddRange(await _context.ApprovalRequests
            .Where(a => a.CompanyId == companyId && a.Status == "Pending")
            .OrderBy(a => a.RequestedDate)
            .Take(5)
            .Select(a => new ErpAlertViewModel
            {
                Title = "Approval pending",
                Message = $"{a.RequestType} for {a.EntityName} is waiting for review.",
                Severity = "Info",
                Icon = "fa-user-check",
                Url = "/Approvals",
                DueDate = a.RequestedDate
            })
            .ToListAsync());

        var openObligations = await _context.PayrollObligations
            .Where(o => o.CompanyId == companyId && o.Status != "Paid")
            .OrderBy(o => o.DueDate)
            .Take(5)
            .ToListAsync();
        alerts.AddRange(openObligations
            .Where(o => o.IsOverdue || (o.DueDate.HasValue && o.DueDate.Value.Date <= today.AddDays(7)))
            .Select(o => new ErpAlertViewModel
            {
                Title = o.IsOverdue ? "Obligation overdue" : "Obligation due soon",
                Message = $"{o.Description} needs payment.",
                Severity = o.IsOverdue ? "Danger" : "Warning",
                Icon = "fa-hand-holding-dollar",
                Url = "/Obligations",
                DueDate = o.DueDate
            }));

        var model = new ErpDashboardViewModel
        {
            AccountCount = await _context.ChartOfAccounts.CountAsync(a => a.CompanyId == companyId),
            JournalEntryCount = await _context.JournalEntries.CountAsync(j => j.CompanyId == companyId),
            SalesDocumentCount = await _context.SalesDocuments.CountAsync(d => d.CompanyId == companyId),
            PurchaseOrderCount = await _context.PurchaseOrders.CountAsync(p => p.CompanyId == companyId),
            WarehouseCount = await _context.Warehouses.CountAsync(w => w.CompanyId == companyId),
            FinancialAccountCount = await _context.FinancialAccounts.CountAsync(a => a.CompanyId == companyId),
            PendingApprovalCount = await _context.ApprovalRequests.CountAsync(a => a.CompanyId == companyId && a.Status == "Pending"),
            AttachmentCount = await _context.DocumentAttachments.CountAsync(a => a.CompanyId == companyId),
            Alerts = alerts
                .OrderBy(a => a.Severity == "Danger" ? 0 : a.Severity == "Warning" ? 1 : 2)
                .ThenBy(a => a.DueDate ?? DateTime.MaxValue)
                .Take(12)
                .ToList(),
            RecentJournalEntries = await _context.JournalEntries
                .Where(j => j.CompanyId == companyId)
                .OrderByDescending(j => j.EntryDate)
                .ThenByDescending(j => j.Id)
                .Take(5)
                .ToListAsync(),
            RecentPurchaseOrders = await _context.PurchaseOrders
                .Where(p => p.CompanyId == companyId)
                .OrderByDescending(p => p.OrderDate)
                .ThenByDescending(p => p.Id)
                .Take(5)
                .ToListAsync()
        };

        return View(model);
    }
}
