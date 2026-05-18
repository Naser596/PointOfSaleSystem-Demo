using WebApplication3.Models;

namespace WebApplication3.Services;

public interface IErpAccountingService
{
    Task<JournalEntry?> PostSalesDocumentAsync(int salesDocumentId, string? createdBy = null);
    Task<JournalEntry?> PostGoodsReceiptAsync(int goodsReceiptId, string? createdBy = null);
    Task<JournalEntry?> PostSupplierInvoiceAsync(int supplierInvoiceId, string? createdBy = null);
    Task<JournalEntry> CreateBalancedEntryAsync(
        int companyId,
        string sourceType,
        string sourceId,
        string description,
        IEnumerable<JournalPostingLine> lines,
        string? createdBy = null,
        DateTime? entryDate = null);
}

public sealed record JournalPostingLine(
    string AccountCode,
    string AccountName,
    string AccountType,
    string Memo,
    decimal Debit,
    decimal Credit);
