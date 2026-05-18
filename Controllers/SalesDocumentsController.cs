using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers;

[Authorize(Roles = "Admin,Manager,Accountant")]
public class SalesDocumentsController(
    ApplicationDbContext context,
    ICurrentCompanyService currentCompany,
    IAuditLogService auditLog,
    IErpAccountingService accounting,
    ISalesWorkflowService salesWorkflow) : Controller
{
    private readonly ApplicationDbContext _context = context;
    private readonly ICurrentCompanyService _currentCompany = currentCompany;
    private readonly IAuditLogService _auditLog = auditLog;
    private readonly IErpAccountingService _accounting = accounting;
    private readonly ISalesWorkflowService _salesWorkflow = salesWorkflow;

    public async Task<IActionResult> Index()
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var model = new SalesDocumentsDashboardViewModel
        {
            Documents = await _context.SalesDocuments
                .Include(d => d.Customer)
                .Include(d => d.Lines)
                .Where(d => d.CompanyId == companyId)
                .OrderByDescending(d => d.DocumentDate)
                .ThenByDescending(d => d.Id)
                .Take(100)
                .ToListAsync(),
            Customers = await _context.Customers
                .Where(c => c.CompanyId == companyId)
                .OrderBy(c => c.Name)
                .ToListAsync()
        };

        return View(model);
    }

    public async Task<IActionResult> Receivables()
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var today = DateTime.Today;
        var invoices = await _context.SalesDocuments
            .Include(d => d.Customer)
            .Where(d =>
                d.CompanyId == companyId &&
                d.DocumentType == "Invoice" &&
                d.TotalAmount > d.PaidAmount &&
                d.Status != "Cancelled")
            .OrderBy(d => d.DueDate ?? d.DocumentDate)
            .ThenBy(d => d.DocumentNumber)
            .ToListAsync();

        var rows = invoices.Select(invoice =>
        {
            var dueDate = invoice.DueDate ?? invoice.DocumentDate;
            var daysOverdue = Math.Max((today - dueDate.Date).Days, 0);
            return new SalesReceivableRow
            {
                DocumentId = invoice.Id,
                DocumentNumber = invoice.DocumentNumber,
                CustomerName = invoice.Customer?.Name ?? "Walk-in / none",
                DocumentDate = invoice.DocumentDate,
                DueDate = invoice.DueDate,
                PaymentStatus = invoice.PaymentStatus,
                TotalAmount = invoice.TotalAmount,
                PaidAmount = invoice.PaidAmount,
                BalanceDue = invoice.TotalAmount - invoice.PaidAmount,
                DaysOverdue = daysOverdue,
                AgingBucket = daysOverdue == 0 ? "Current" :
                    daysOverdue <= 30 ? "1-30" :
                    daysOverdue <= 60 ? "31-60" : "61+"
            };
        }).ToList();

        var aging = rows
            .GroupBy(r => r.CustomerName)
            .Select(g => new ReceivableAgingRow
            {
                CustomerName = g.Key,
                Current = g.Where(r => r.AgingBucket == "Current").Sum(r => r.BalanceDue),
                Days1To30 = g.Where(r => r.AgingBucket == "1-30").Sum(r => r.BalanceDue),
                Days31To60 = g.Where(r => r.AgingBucket == "31-60").Sum(r => r.BalanceDue),
                Days61Plus = g.Where(r => r.AgingBucket == "61+").Sum(r => r.BalanceDue)
            })
            .OrderByDescending(r => r.Total)
            .ToList();

        return View(new ReceivablesDashboardViewModel
        {
            OpenInvoices = rows,
            Aging = aging,
            CurrentTotal = rows.Where(r => r.AgingBucket == "Current").Sum(r => r.BalanceDue),
            Days1To30Total = rows.Where(r => r.AgingBucket == "1-30").Sum(r => r.BalanceDue),
            Days31To60Total = rows.Where(r => r.AgingBucket == "31-60").Sum(r => r.BalanceDue),
            Days61PlusTotal = rows.Where(r => r.AgingBucket == "61+").Sum(r => r.BalanceDue)
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var document = await FindDocumentAsync(id, companyId);
        if (document == null) return NotFound();

        var company = await _context.Companies.FindAsync(companyId);
        if (company == null) return NotFound();

        var model = new SalesDocumentDetailsViewModel
        {
            Document = document,
            Company = company,
            Products = await _context.Products
                .Where(p => p.CompanyId == companyId && p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync(),
            FinancialAccounts = await _context.FinancialAccounts
                .Where(a => a.CompanyId == companyId && a.IsActive)
                .OrderBy(a => a.Name)
                .ToListAsync()
        };

        return View(model);
    }

    public async Task<IActionResult> Print(int id)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var document = await FindDocumentAsync(id, companyId);
        if (document == null) return NotFound();

        var company = await _context.Companies.FindAsync(companyId);
        if (company == null) return NotFound();

        return View(new SalesDocumentDetailsViewModel
        {
            Document = document,
            Company = company
        });
    }

    public async Task<IActionResult> DownloadPdf(int id)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var document = await FindDocumentAsync(id, companyId);
        if (document == null) return NotFound();

        var company = await _context.Companies.FindAsync(companyId);
        if (company == null) return NotFound();

        var pdf = SimplePdfService.BuildSalesDocument(document, company);
        var fileName = $"{document.DocumentNumber}.pdf";
        await _auditLog.LogAsync("ExportPdf", nameof(SalesDocument), document.Id.ToString(), $"Downloaded PDF for {document.DocumentNumber}", companyId);
        return File(pdf, "application/pdf", fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SalesDocumentInput input)
    {
        if (string.IsNullOrWhiteSpace(input.DocumentType))
        {
            TempData["Error"] = "Document type is required.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        if (input.CustomerId.HasValue)
        {
            var customerExists = await _context.Customers
                .AnyAsync(c => c.CompanyId == companyId && c.Id == input.CustomerId.Value);
            if (!customerExists) return NotFound();
        }

        var documentNumber = string.IsNullOrWhiteSpace(input.DocumentNumber)
            ? $"SD-{DateTime.UtcNow:yyyyMMddHHmmss}"
            : input.DocumentNumber.Trim().ToUpperInvariant();

        var exists = await _context.SalesDocuments
            .AnyAsync(d => d.CompanyId == companyId && d.DocumentNumber == documentNumber);
        if (exists)
        {
            TempData["Error"] = "A sales document with this number already exists.";
            return RedirectToAction(nameof(Index));
        }

        var document = new SalesDocument
        {
            CompanyId = companyId,
            DocumentType = input.DocumentType.Trim(),
            DocumentNumber = documentNumber,
            CustomerId = input.CustomerId,
            DocumentDate = input.DocumentDate.Date,
            DueDate = input.DueDate?.Date,
            Status = string.IsNullOrWhiteSpace(input.Status) ? "Draft" : input.Status.Trim(),
            SubTotal = input.SubTotal,
            TaxAmount = input.TaxAmount,
            DiscountAmount = input.DiscountAmount,
            TotalAmount = input.SubTotal + input.TaxAmount - input.DiscountAmount,
            Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
            CreatedBy = User.Identity?.Name,
            CreatedDate = DateTime.Now
        };

        _context.SalesDocuments.Add(document);
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync("Create", nameof(SalesDocument), document.Id.ToString(), $"Created sales document {document.DocumentNumber}", companyId);

        TempData["Success"] = "Sales document created.";
        return RedirectToAction(nameof(Details), new { id = document.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddLine(SalesDocumentLineInput input)
    {
        if (input.SalesDocumentId <= 0 || input.Quantity <= 0)
        {
            TempData["Error"] = "Document and positive quantity are required.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var document = await FindDocumentAsync(input.SalesDocumentId, companyId);
        if (document == null) return NotFound();

        Product? product = null;
        if (input.ProductId.HasValue)
        {
            product = await _context.Products
                .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == input.ProductId.Value);
            if (product == null) return NotFound();
        }

        var description = !string.IsNullOrWhiteSpace(input.Description)
            ? input.Description.Trim()
            : product?.Name;
        if (string.IsNullOrWhiteSpace(description))
        {
            TempData["Error"] = "Line description is required.";
            return RedirectToAction(nameof(Details), new { id = document.Id });
        }

        var unitPrice = input.UnitPrice > 0 ? input.UnitPrice : product?.Price ?? 0;
        var taxRate = input.TaxRate > 0 ? input.TaxRate : product?.TaxRate ?? 0;
        var taxableAmount = input.Quantity * unitPrice;
        var taxAmount = Math.Round(taxableAmount * taxRate / 100, 2);
        var line = new SalesDocumentLine
        {
            SalesDocumentId = document.Id,
            ProductId = product?.Id,
            Description = description,
            Quantity = input.Quantity,
            UnitPrice = unitPrice,
            TaxRate = taxRate,
            TaxAmount = taxAmount,
            LineTotal = taxableAmount + taxAmount
        };

        document.Lines.Add(line);
        RecalculateTotals(document);
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync("AddLine", nameof(SalesDocument), document.Id.ToString(), $"Added line to sales document {document.DocumentNumber}", companyId);

        TempData["Success"] = "Sales document line added.";
        return RedirectToAction(nameof(Details), new { id = document.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(SalesDocumentConvertInput input)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var source = await FindDocumentAsync(input.SalesDocumentId, companyId);
        if (source == null) return NotFound();

        try
        {
            var converted = await _salesWorkflow.ConvertDocumentAsync(source.Id, input.TargetDocumentType, User.Identity?.Name);
            await _auditLog.LogAsync("Convert", nameof(SalesDocument), converted.Id.ToString(), $"Converted {source.DocumentNumber} to {converted.DocumentNumber}", companyId);
            TempData["Success"] = $"Document converted to {converted.DocumentNumber}.";
            return RedirectToAction(nameof(Details), new { id = converted.Id });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id = source.Id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordPayment(SalesDocumentPaymentInput input)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var document = await FindDocumentAsync(input.SalesDocumentId, companyId);
        if (document == null) return NotFound();

        try
        {
            var payment = await _salesWorkflow.RecordPaymentAsync(input, User.Identity?.Name);
            await _auditLog.LogAsync("Payment", nameof(SalesDocument), document.Id.ToString(), $"Recorded payment {payment.Amount:N2} for {document.DocumentNumber}", companyId);
            TempData["Success"] = "Payment recorded and accounting entry posted.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id = document.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var document = await FindDocumentAsync(id, companyId);
        if (document == null) return NotFound();

        document.Status = string.IsNullOrWhiteSpace(status) ? document.Status : status.Trim();
        await _context.SaveChangesAsync();
        var journalEntry = await _accounting.PostSalesDocumentAsync(document.Id, User.Identity?.Name);
        await _auditLog.LogAsync("UpdateStatus", nameof(SalesDocument), document.Id.ToString(), $"Updated sales document {document.DocumentNumber} to {document.Status}", companyId);

        TempData["Success"] = journalEntry == null
            ? "Sales document status updated."
            : $"Sales document status updated and journal entry {journalEntry.EntryNumber} posted.";
        return RedirectToAction(nameof(Details), new { id = document.Id });
    }

    private async Task<SalesDocument?> FindDocumentAsync(int id, int companyId)
    {
        return await _context.SalesDocuments
            .Include(d => d.Company)
            .Include(d => d.Customer)
            .Include(d => d.Lines)
                .ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId);
    }

    private static void RecalculateTotals(SalesDocument document)
    {
        document.SubTotal = document.Lines.Sum(l => l.Quantity * l.UnitPrice);
        document.TaxAmount = document.Lines.Sum(l => l.TaxAmount);
        document.TotalAmount = document.SubTotal + document.TaxAmount - document.DiscountAmount;
    }
}
