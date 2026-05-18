using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebApplication3.Models;

public class ApprovalRequest
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    [ValidateNever]
    public Company Company { get; set; } = null!;

    [Required, MaxLength(80)]
    public string RequestType { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string EntityName { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? EntityId { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = "Pending";

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [MaxLength(256)]
    public string? RequestedBy { get; set; }

    [MaxLength(256)]
    public string? ReviewedBy { get; set; }

    public DateTime RequestedDate { get; set; }
    public DateTime? ReviewedDate { get; set; }
}

public class ApprovalRule
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    [ValidateNever]
    public Company Company { get; set; } = null!;

    [Required, MaxLength(80)]
    public string RuleName { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string EntityName { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string ActionName { get; set; } = "Create";

    public decimal AmountThreshold { get; set; }
    public bool IsActive { get; set; } = true;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; }
}

public class OfflineSyncRecord
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    [ValidateNever]
    public Company Company { get; set; } = null!;

    [Required, MaxLength(120)]
    public string ClientId { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string SyncType { get; set; } = "Sale";

    [Required, MaxLength(30)]
    public string Status { get; set; } = "Pending";

    public DateTime QueuedAt { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public int? SaleId { get; set; }

    [ValidateNever]
    public Sale? Sale { get; set; }

    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    public string PayloadJson { get; set; } = string.Empty;
}

public class DocumentAttachment
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    [ValidateNever]
    public Company Company { get; set; } = null!;

    [Required, MaxLength(120)]
    public string EntityName { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? EntityId { get; set; }

    [Required, MaxLength(260)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? ContentType { get; set; }

    public long FileSize { get; set; }

    [MaxLength(256)]
    public string? UploadedBy { get; set; }

    public DateTime UploadedDate { get; set; }
}

public class FiscalPeriod
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    [ValidateNever]
    public Company Company { get; set; } = null!;

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = "Open";

    public DateTime CreatedDate { get; set; }
}

public class ChartOfAccount
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    [ValidateNever]
    public Company Company { get; set; } = null!;

    [Required, MaxLength(40)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(180)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string AccountType { get; set; } = "Asset";

    public int? ParentAccountId { get; set; }

    [ValidateNever]
    public ChartOfAccount? ParentAccount { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }

    public List<JournalEntryLine> JournalEntryLines { get; set; } = [];
}

public class JournalEntry
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    [ValidateNever]
    public Company Company { get; set; } = null!;

    [Required, MaxLength(60)]
    public string EntryNumber { get; set; } = string.Empty;

    public DateTime EntryDate { get; set; }
    public int? FiscalPeriodId { get; set; }

    [ValidateNever]
    public FiscalPeriod? FiscalPeriod { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = "Draft";

    [Required, MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? SourceType { get; set; }

    [MaxLength(80)]
    public string? SourceId { get; set; }

    [MaxLength(256)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public List<JournalEntryLine> Lines { get; set; } = [];

    public decimal TotalDebit => Lines.Sum(l => l.Debit);
    public decimal TotalCredit => Lines.Sum(l => l.Credit);
    public bool IsBalanced => TotalDebit == TotalCredit;
}

public class JournalEntryLine
{
    public int Id { get; set; }
    public int JournalEntryId { get; set; }

    [ValidateNever]
    public JournalEntry JournalEntry { get; set; } = null!;

    public int AccountId { get; set; }

    [ValidateNever]
    public ChartOfAccount Account { get; set; } = null!;

    [MaxLength(300)]
    public string? Memo { get; set; }

    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

public class SalesDocument
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    [ValidateNever]
    public Company Company { get; set; } = null!;

    [Required, MaxLength(40)]
    public string DocumentType { get; set; } = "Quote";

    [Required, MaxLength(60)]
    public string DocumentNumber { get; set; } = string.Empty;

    public int? CustomerId { get; set; }

    [ValidateNever]
    public Customer? Customer { get; set; }

    public DateTime DocumentDate { get; set; }
    public DateTime? DueDate { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = "Draft";

    [Required, MaxLength(30)]
    public string PaymentStatus { get; set; } = "Unpaid";

    public decimal PaidAmount { get; set; }

    public int? ConvertedFromDocumentId { get; set; }

    [ValidateNever]
    public SalesDocument? ConvertedFromDocument { get; set; }

    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [MaxLength(256)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }
    public List<SalesDocumentLine> Lines { get; set; } = [];
}

public class SalesDocumentLine
{
    public int Id { get; set; }
    public int SalesDocumentId { get; set; }

    [ValidateNever]
    public SalesDocument SalesDocument { get; set; } = null!;

    public int? ProductId { get; set; }

    [ValidateNever]
    public Product? Product { get; set; }

    [Required, MaxLength(300)]
    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
}

public class PurchaseOrder
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    [ValidateNever]
    public Company Company { get; set; } = null!;

    [Required, MaxLength(60)]
    public string OrderNumber { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string SupplierName { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? SupplierTaxNumber { get; set; }

    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDate { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = "Draft";

    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [MaxLength(256)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }
    public List<PurchaseOrderLine> Lines { get; set; } = [];
}

public class PurchaseOrderLine
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }

    [ValidateNever]
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public int? ProductId { get; set; }

    [ValidateNever]
    public Product? Product { get; set; }

    [Required, MaxLength(300)]
    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
}

public class GoodsReceipt
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    [ValidateNever]
    public Company Company { get; set; } = null!;

    public int? PurchaseOrderId { get; set; }

    [ValidateNever]
    public PurchaseOrder? PurchaseOrder { get; set; }

    [Required, MaxLength(60)]
    public string ReceiptNumber { get; set; } = string.Empty;

    public DateTime ReceiptDate { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = "Draft";

    [MaxLength(256)]
    public string? ReceivedBy { get; set; }

    public DateTime CreatedDate { get; set; }
    public List<GoodsReceiptLine> Lines { get; set; } = [];
}

public class GoodsReceiptLine
{
    public int Id { get; set; }
    public int GoodsReceiptId { get; set; }

    [ValidateNever]
    public GoodsReceipt GoodsReceipt { get; set; } = null!;

    public int? ProductId { get; set; }

    [ValidateNever]
    public Product? Product { get; set; }

    [Required, MaxLength(300)]
    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
}

public class Warehouse
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    [ValidateNever]
    public Company Company { get; set; } = null!;

    [Required, MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? Code { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }

    public List<StockLocation> Locations { get; set; } = [];
}

public class StockLocation
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    [ValidateNever]
    public Company Company { get; set; } = null!;

    public int WarehouseId { get; set; }

    [ValidateNever]
    public Warehouse Warehouse { get; set; } = null!;

    [Required, MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? Code { get; set; }

    public bool IsActive { get; set; } = true;
}

public class WarehouseStock
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    [ValidateNever]
    public Company Company { get; set; } = null!;

    public int WarehouseId { get; set; }

    [ValidateNever]
    public Warehouse Warehouse { get; set; } = null!;

    public int? StockLocationId { get; set; }

    [ValidateNever]
    public StockLocation? StockLocation { get; set; }

    public int ProductId { get; set; }

    [ValidateNever]
    public Product Product { get; set; } = null!;

    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public class StockTransfer
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    [ValidateNever]
    public Company Company { get; set; } = null!;

    [Required, MaxLength(60)]
    public string TransferNumber { get; set; } = string.Empty;

    public int FromWarehouseId { get; set; }
    public int ToWarehouseId { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = "Draft";

    public DateTime TransferDate { get; set; }

    [MaxLength(256)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }
    public List<StockTransferLine> Lines { get; set; } = [];
}

public class StockTransferLine
{
    public int Id { get; set; }
    public int StockTransferId { get; set; }

    [ValidateNever]
    public StockTransfer StockTransfer { get; set; } = null!;

    public int ProductId { get; set; }

    [ValidateNever]
    public Product Product { get; set; } = null!;

    public decimal Quantity { get; set; }
}

public class ProductTraceLot
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    [ValidateNever]
    public Company Company { get; set; } = null!;

    public int ProductId { get; set; }

    [ValidateNever]
    public Product Product { get; set; } = null!;

    public int? WarehouseId { get; set; }

    [ValidateNever]
    public Warehouse? Warehouse { get; set; }

    [Required, MaxLength(80)]
    public string TraceType { get; set; } = "Batch";

    [Required, MaxLength(120)]
    public string TraceNumber { get; set; } = string.Empty;

    public decimal Quantity { get; set; }
    public DateTime? ExpiryDate { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = "Active";

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [MaxLength(256)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }
}

public class StockCount
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    [ValidateNever]
    public Company Company { get; set; } = null!;

    [Required, MaxLength(60)]
    public string CountNumber { get; set; } = string.Empty;

    public int WarehouseId { get; set; }

    [ValidateNever]
    public Warehouse Warehouse { get; set; } = null!;

    public DateTime CountDate { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = "Draft";

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [MaxLength(256)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime? PostedDate { get; set; }

    public List<StockCountLine> Lines { get; set; } = [];
}

public class StockCountLine
{
    public int Id { get; set; }
    public int StockCountId { get; set; }

    [ValidateNever]
    public StockCount StockCount { get; set; } = null!;

    public int ProductId { get; set; }

    [ValidateNever]
    public Product Product { get; set; } = null!;

    public int? StockLocationId { get; set; }

    [ValidateNever]
    public StockLocation? StockLocation { get; set; }

    public decimal SystemQuantity { get; set; }
    public decimal? CountedQuantity { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    public decimal Variance => (CountedQuantity ?? SystemQuantity) - SystemQuantity;
}

public class NotificationMessage
{
    public int Id { get; set; }
    public int? CompanyId { get; set; }

    [ValidateNever]
    public Company? Company { get; set; }

    [Required, MaxLength(40)]
    public string Channel { get; set; } = "Email";

    [Required, MaxLength(80)]
    public string NotificationType { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? EntityName { get; set; }

    [MaxLength(80)]
    public string? EntityId { get; set; }

    [Required, MaxLength(300)]
    public string Recipient { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Subject { get; set; } = string.Empty;

    [Required, MaxLength(4000)]
    public string Body { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Status { get; set; } = "Queued";

    public DateTime CreatedDate { get; set; }
    public DateTime? SentDate { get; set; }

    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }
}

public class FinancialAccount
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    [ValidateNever]
    public Company Company { get; set; } = null!;

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string AccountType { get; set; } = "Cash";

    [MaxLength(80)]
    public string? AccountNumber { get; set; }

    [MaxLength(10)]
    public string CurrencyCode { get; set; } = "USD";

    public decimal OpeningBalance { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
}

public class BankTransaction
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    [ValidateNever]
    public Company Company { get; set; } = null!;

    public int FinancialAccountId { get; set; }

    [ValidateNever]
    public FinancialAccount FinancialAccount { get; set; } = null!;

    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; }

    [Required, MaxLength(30)]
    public string TransactionType { get; set; } = "Debit";

    [Required, MaxLength(30)]
    public string Status { get; set; } = "Unreconciled";

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(120)]
    public string? Reference { get; set; }

    public DateTime CreatedDate { get; set; }
}

public class PaymentRecord
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    [ValidateNever]
    public Company Company { get; set; } = null!;

    public int? FinancialAccountId { get; set; }

    [ValidateNever]
    public FinancialAccount? FinancialAccount { get; set; }

    [Required, MaxLength(30)]
    public string Direction { get; set; } = "In";

    [Required, MaxLength(40)]
    public string PaymentMethod { get; set; } = "Cash";

    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = "Draft";

    [MaxLength(120)]
    public string? EntityName { get; set; }

    [MaxLength(80)]
    public string? EntityId { get; set; }

    [MaxLength(80)]
    public string? ProviderName { get; set; }

    [MaxLength(160)]
    public string? ProviderTransactionId { get; set; }

    [MaxLength(80)]
    public string? ProviderStatus { get; set; }

    public string? ProviderPayloadJson { get; set; }

    [MaxLength(256)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }
}
