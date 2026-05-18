using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Services;

public class ErpAccountingService(ApplicationDbContext context) : IErpAccountingService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<JournalEntry?> PostSalesDocumentAsync(int salesDocumentId, string? createdBy = null)
    {
        var document = await _context.SalesDocuments
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.Id == salesDocumentId);
        if (document == null || document.TotalAmount <= 0 || !document.Lines.Any())
        {
            return null;
        }

        if (!string.Equals(document.DocumentType, "Invoice", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(document.DocumentType, "Order", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!string.Equals(document.Status, "Issued", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(document.Status, "Closed", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var revenue = document.SubTotal - document.DiscountAmount;
        var lines = new List<JournalPostingLine>
        {
            new("1100", "Accounts Receivable", "Asset", document.DocumentNumber, document.TotalAmount, 0),
            new("4000", "Sales Revenue", "Revenue", document.DocumentNumber, 0, revenue)
        };

        if (document.TaxAmount > 0)
        {
            lines.Add(new("2100", "Sales Tax Payable", "Liability", document.DocumentNumber, 0, document.TaxAmount));
        }

        return await CreateBalancedEntryAsync(
            document.CompanyId,
            nameof(SalesDocument),
            document.Id.ToString(),
            $"Posted sales document {document.DocumentNumber}",
            lines,
            createdBy,
            document.DocumentDate);
    }

    public async Task<JournalEntry?> PostGoodsReceiptAsync(int goodsReceiptId, string? createdBy = null)
    {
        var receipt = await _context.GoodsReceipts
            .Include(r => r.Lines)
            .Include(r => r.PurchaseOrder)
            .FirstOrDefaultAsync(r => r.Id == goodsReceiptId);
        if (receipt == null || !receipt.Lines.Any())
        {
            return null;
        }

        var inventoryValue = receipt.Lines.Sum(l => decimal.Round(l.Quantity * l.UnitCost, 2));
        if (inventoryValue <= 0)
        {
            return null;
        }

        var reference = receipt.PurchaseOrder?.OrderNumber ?? receipt.ReceiptNumber;
        return await CreateBalancedEntryAsync(
            receipt.CompanyId,
            nameof(GoodsReceipt),
            receipt.Id.ToString(),
            $"Posted goods receipt {receipt.ReceiptNumber}",
            [
                new("1200", "Inventory", "Asset", reference, inventoryValue, 0),
                new("2000", "Accounts Payable", "Liability", reference, 0, inventoryValue)
            ],
            createdBy,
            receipt.ReceiptDate);
    }

    public async Task<JournalEntry?> PostSupplierInvoiceAsync(int supplierInvoiceId, string? createdBy = null)
    {
        var invoice = await _context.SupplierInvoices
            .Include(i => i.Items)
            .Include(i => i.PurchaseOrder)
            .Include(i => i.GoodsReceipt)
            .FirstOrDefaultAsync(i => i.Id == supplierInvoiceId);
        if (invoice == null || invoice.TotalAmount <= 0 || !invoice.Items.Any())
        {
            return null;
        }

        var reference = invoice.SupplierInvoiceNumber ?? invoice.InvoiceNumber;
        var lines = new List<JournalPostingLine>();

        if (invoice.GoodsReceiptId.HasValue)
        {
            var clearingMemo = invoice.GoodsReceipt?.ReceiptNumber ?? invoice.PurchaseOrder?.OrderNumber ?? reference;
            lines.Add(new("2000", "Accounts Payable", "Liability", $"Clear provisional AP {clearingMemo}", invoice.SubTotal, 0));
        }
        else
        {
            lines.Add(new("1200", "Inventory", "Asset", reference, invoice.SubTotal, 0));
        }

        if (invoice.TaxAmount > 0)
        {
            lines.Add(new("1300", "Input Tax Receivable", "Asset", reference, invoice.TaxAmount, 0));
        }

        lines.Add(new("2000", "Accounts Payable", "Liability", reference, 0, invoice.TotalAmount));

        var entry = await CreateBalancedEntryAsync(
            invoice.CompanyId,
            nameof(SupplierInvoice),
            invoice.Id.ToString(),
            $"Posted supplier invoice {invoice.InvoiceNumber}",
            lines,
            createdBy,
            invoice.InvoiceDate);

        if (!string.Equals(invoice.Status, "Posted", StringComparison.OrdinalIgnoreCase))
        {
            invoice.Status = "Posted";
            await _context.SaveChangesAsync();
        }

        return entry;
    }

    public async Task<JournalEntry> CreateBalancedEntryAsync(
        int companyId,
        string sourceType,
        string sourceId,
        string description,
        IEnumerable<JournalPostingLine> lines,
        string? createdBy = null,
        DateTime? entryDate = null)
    {
        var normalizedLines = lines
            .Where(l => l.Debit > 0 || l.Credit > 0)
            .ToList();

        if (!normalizedLines.Any())
        {
            throw new InvalidOperationException("Journal entry requires at least one posting line.");
        }

        var totalDebit = normalizedLines.Sum(l => l.Debit);
        var totalCredit = normalizedLines.Sum(l => l.Credit);
        if (totalDebit != totalCredit)
        {
            throw new InvalidOperationException("Journal entry must balance debit and credit.");
        }

        var existing = await _context.JournalEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e =>
                e.CompanyId == companyId &&
                e.SourceType == sourceType &&
                e.SourceId == sourceId);
        if (existing != null)
        {
            return existing;
        }

        var postingDate = (entryDate ?? DateTime.Today).Date;
        var fiscalPeriod = await ResolveOpenFiscalPeriodAsync(companyId, postingDate);

        var entry = new JournalEntry
        {
            CompanyId = companyId,
            EntryNumber = await GenerateEntryNumberAsync(companyId),
            EntryDate = postingDate,
            FiscalPeriodId = fiscalPeriod?.Id,
            Status = "Posted",
            Description = description,
            SourceType = sourceType,
            SourceId = sourceId,
            CreatedBy = createdBy,
            CreatedDate = DateTime.Now
        };

        foreach (var line in normalizedLines)
        {
            var account = await GetOrCreateAccountAsync(
                companyId,
                line.AccountCode,
                line.AccountName,
                line.AccountType);

            entry.Lines.Add(new JournalEntryLine
            {
                AccountId = account.Id,
                Memo = line.Memo,
                Debit = line.Debit,
                Credit = line.Credit
            });
        }

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync();
        return entry;
    }

    private async Task<FiscalPeriod?> ResolveOpenFiscalPeriodAsync(int companyId, DateTime postingDate)
    {
        var fiscalPeriod = await _context.FiscalPeriods
            .Where(p =>
                p.CompanyId == companyId &&
                p.StartDate.Date <= postingDate &&
                p.EndDate.Date >= postingDate)
            .OrderByDescending(p => p.StartDate)
            .FirstOrDefaultAsync();

        if (fiscalPeriod == null)
        {
            return null;
        }

        if (string.Equals(fiscalPeriod.Status, "Closed", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Fiscal period {fiscalPeriod.Name} is closed.");
        }

        return fiscalPeriod;
    }

    private async Task<ChartOfAccount> GetOrCreateAccountAsync(int companyId, string code, string name, string accountType)
    {
        var account = await _context.ChartOfAccounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == code);
        if (account != null)
        {
            return account;
        }

        account = new ChartOfAccount
        {
            CompanyId = companyId,
            Code = code,
            Name = name,
            AccountType = accountType,
            IsActive = true,
            CreatedDate = DateTime.Now
        };
        _context.ChartOfAccounts.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }

    private async Task<string> GenerateEntryNumberAsync(int companyId)
    {
        var datePart = DateTime.Today.ToString("yyyyMMdd");
        var startsWith = $"JE-{datePart}-";
        var count = await _context.JournalEntries
            .CountAsync(e => e.CompanyId == companyId && e.EntryNumber.StartsWith(startsWith));
        return $"{startsWith}{count + 1:000}";
    }
}
