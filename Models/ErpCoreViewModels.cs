namespace WebApplication3.Models;

public class ErpDashboardViewModel
{
    public int AccountCount { get; set; }
    public int JournalEntryCount { get; set; }
    public int SalesDocumentCount { get; set; }
    public int PurchaseOrderCount { get; set; }
    public int WarehouseCount { get; set; }
    public int FinancialAccountCount { get; set; }
    public int PendingApprovalCount { get; set; }
    public int AttachmentCount { get; set; }
    public List<ErpAlertViewModel> Alerts { get; set; } = [];
    public List<JournalEntry> RecentJournalEntries { get; set; } = [];
    public List<PurchaseOrder> RecentPurchaseOrders { get; set; } = [];
}

public class ErpAlertViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public string Icon { get; set; } = "fa-circle-info";
    public string Url { get; set; } = "/";
    public DateTime? DueDate { get; set; }
}

public class AccountingDashboardViewModel
{
    public List<ChartOfAccount> Accounts { get; set; } = [];
    public List<FiscalPeriod> FiscalPeriods { get; set; } = [];
    public List<JournalEntry> RecentJournalEntries { get; set; } = [];
    public List<AccountingReportRow> TrialBalance { get; set; } = [];
    public List<AccountingReportRow> ProfitAndLoss { get; set; } = [];
    public List<AccountingReportRow> BalanceSheet { get; set; } = [];
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal NetIncome { get; set; }
    public decimal AssetsTotal { get; set; }
    public decimal LiabilitiesTotal { get; set; }
    public decimal EquityTotal { get; set; }
    public ChartOfAccountInput AccountInput { get; set; } = new();
    public FiscalPeriodInput FiscalPeriodInput { get; set; } = new();
}

public class AccountingReportRow
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
}

public class ChartOfAccountInput
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AccountType { get; set; } = "Asset";
    public int? ParentAccountId { get; set; }
}

public class FiscalPeriodInput
{
    public string Name { get; set; } = $"{DateTime.Today:yyyy}";
    public DateTime StartDate { get; set; } = new(DateTime.Today.Year, 1, 1);
    public DateTime EndDate { get; set; } = new(DateTime.Today.Year, 12, 31);
}

public class SalesDocumentsDashboardViewModel
{
    public List<SalesDocument> Documents { get; set; } = [];
    public List<Customer> Customers { get; set; } = [];
}

public class ReceivablesDashboardViewModel
{
    public List<SalesReceivableRow> OpenInvoices { get; set; } = [];
    public List<ReceivableAgingRow> Aging { get; set; } = [];
    public decimal CurrentTotal { get; set; }
    public decimal Days1To30Total { get; set; }
    public decimal Days31To60Total { get; set; }
    public decimal Days61PlusTotal { get; set; }
    public decimal TotalOutstanding => CurrentTotal + Days1To30Total + Days31To60Total + Days61PlusTotal;
}

public class SalesReceivableRow
{
    public int DocumentId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceDue { get; set; }
    public int DaysOverdue { get; set; }
    public string AgingBucket { get; set; } = "Current";
}

public class SalesDocumentDetailsViewModel
{
    public SalesDocument Document { get; set; } = null!;
    public List<Product> Products { get; set; } = [];
    public List<FinancialAccount> FinancialAccounts { get; set; } = [];
    public Company Company { get; set; } = null!;
}

public class SalesDocumentInput
{
    public string DocumentType { get; set; } = "Quote";
    public string? DocumentNumber { get; set; }
    public int? CustomerId { get; set; }
    public DateTime DocumentDate { get; set; } = DateTime.Today;
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = "Draft";
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? Notes { get; set; }
}

public class SalesDocumentLineInput
{
    public int SalesDocumentId { get; set; }
    public int? ProductId { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
}

public class SalesDocumentPaymentInput
{
    public int SalesDocumentId { get; set; }
    public int? FinancialAccountId { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? Reference { get; set; }
}

public class SalesDocumentConvertInput
{
    public int SalesDocumentId { get; set; }
    public string TargetDocumentType { get; set; } = "Invoice";
}

public class PurchasingDashboardViewModel
{
    public List<PurchaseOrder> PurchaseOrders { get; set; } = [];
    public List<Product> LowStockProducts { get; set; } = [];
}

public class PurchaseOrderDetailsViewModel
{
    public PurchaseOrder Order { get; set; } = null!;
    public List<Product> Products { get; set; } = [];
    public List<Warehouse> Warehouses { get; set; } = [];
    public List<StockLocation> Locations { get; set; } = [];
    public List<GoodsReceipt> Receipts { get; set; } = [];
}

public class PurchaseOrderInput
{
    public string? OrderNumber { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? SupplierTaxNumber { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.Today;
    public DateTime? ExpectedDate { get; set; }
    public string Status { get; set; } = "Draft";
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public string? Notes { get; set; }
}

public class PurchaseOrderLineInput
{
    public int PurchaseOrderId { get; set; }
    public int? ProductId { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal UnitCost { get; set; }
    public decimal TaxRate { get; set; }
}

public class GoodsReceiptInput
{
    public int PurchaseOrderId { get; set; }
    public int WarehouseId { get; set; }
    public int? StockLocationId { get; set; }
    public DateTime ReceiptDate { get; set; } = DateTime.Today;
    public List<GoodsReceiptLineInput> Lines { get; set; } = [];
}

public class GoodsReceiptLineInput
{
    public int PurchaseOrderLineId { get; set; }
    public decimal Quantity { get; set; }
}

public class WarehouseDashboardViewModel
{
    public List<Warehouse> Warehouses { get; set; } = [];
    public List<StockLocation> Locations { get; set; } = [];
    public List<WarehouseStock> Stocks { get; set; } = [];
    public List<Product> Products { get; set; } = [];
    public List<StockTransfer> PendingTransfers { get; set; } = [];
    public List<ProductTraceLot> TraceLots { get; set; } = [];
}

public class InventoryControlViewModel
{
    public int? ProductId { get; set; }
    public int? WarehouseId { get; set; }
    public List<Product> Products { get; set; } = [];
    public List<Warehouse> Warehouses { get; set; } = [];
    public List<WarehouseStock> Stocks { get; set; } = [];
    public List<StockMovement> StockMovements { get; set; } = [];
    public List<ReorderSuggestionRow> ReorderSuggestions { get; set; } = [];
    public List<ProductTraceLot> TraceLots { get; set; } = [];
}

public class ReorderSuggestionRow
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public int CurrentStock { get; set; }
    public int MinStock { get; set; }
    public int SuggestedQuantity { get; set; }
    public decimal EstimatedCost { get; set; }
}

public class WarehouseInput
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Address { get; set; }
}

public class StockLocationInput
{
    public int WarehouseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
}

public class StockTransferInput
{
    public int ProductId { get; set; }
    public int FromWarehouseId { get; set; }
    public int? FromStockLocationId { get; set; }
    public int ToWarehouseId { get; set; }
    public int? ToStockLocationId { get; set; }
    public decimal Quantity { get; set; }
    public DateTime TransferDate { get; set; } = DateTime.Today;
}

public class StockAdjustmentInput
{
    public int ProductId { get; set; }
    public int WarehouseId { get; set; }
    public int? StockLocationId { get; set; }
    public decimal QuantityDelta { get; set; }
    public string? Reason { get; set; }
}

public class StockAssignmentInput
{
    public int ProductId { get; set; }
    public int WarehouseId { get; set; }
    public int? StockLocationId { get; set; }
    public decimal Quantity { get; set; }
    public string? Reason { get; set; }
}

public class ProductTraceLotInput
{
    public int ProductId { get; set; }
    public int? WarehouseId { get; set; }
    public string TraceType { get; set; } = "Batch";
    public string TraceNumber { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Notes { get; set; }
}

public class StockCountCreateInput
{
    public int WarehouseId { get; set; }
    public DateTime CountDate { get; set; } = DateTime.Today;
    public string? Notes { get; set; }
}

public class StockCountLineUpdateInput
{
    public int StockCountId { get; set; }
    public int LineId { get; set; }
    public decimal CountedQuantity { get; set; }
    public string? Reason { get; set; }
}

public class ReorderToPurchaseOrderInput
{
    public int ProductId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? SupplierTaxNumber { get; set; }
    public int SuggestedQuantity { get; set; }
}

public class FinancialAccountsDashboardViewModel
{
    public List<FinancialAccount> Accounts { get; set; } = [];
    public List<BankTransaction> BankTransactions { get; set; } = [];
    public List<PaymentRecord> PaymentRecords { get; set; } = [];
}

public class FinancialAccountInput
{
    public string Name { get; set; } = string.Empty;
    public string AccountType { get; set; } = "Cash";
    public string? AccountNumber { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal OpeningBalance { get; set; }
}

public class BankTransactionInput
{
    public int FinancialAccountId { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string TransactionType { get; set; } = "Debit";
    public string Status { get; set; } = "Unreconciled";
    public string? Description { get; set; }
    public string? Reference { get; set; }
}

public class ApprovalDashboardViewModel
{
    public List<ApprovalRequest> Requests { get; set; } = [];
    public List<ApprovalRule> Rules { get; set; } = [];
}

public class ApprovalRequestInput
{
    public string RequestType { get; set; } = "Approval";
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Notes { get; set; }
}

public class ApprovalRuleInput
{
    public string RuleName { get; set; } = string.Empty;
    public string EntityName { get; set; } = "PurchaseOrder";
    public string ActionName { get; set; } = "Create";
    public decimal AmountThreshold { get; set; }
    public string? Notes { get; set; }
}

public class HrDashboardViewModel
{
    public List<Employee> Employees { get; set; } = [];
    public List<PayrollObligation> EmployeeObligations { get; set; } = [];
    public List<PayrollRun> PayrollRuns { get; set; } = [];
    public List<FinancialAccount> FinancialAccounts { get; set; } = [];
    public PayrollRunInput PayrollInput { get; set; } = new();
}

public class EmployeeInput
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? EmployeeNumber { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public string? PersonalNumber { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? EmergencyContact { get; set; }
    public DateTime HireDate { get; set; } = DateTime.Today;
    public decimal MonthlySalary { get; set; }
    public int SalaryDueDay { get; set; } = 5;
    public string? Notes { get; set; }
}

public class PayrollRunInput
{
    public DateTime PeriodStart { get; set; } = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    public DateTime PeriodEnd { get; set; } = new(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));
    public DateTime? DueDate { get; set; } = DateTime.Today;
    public decimal BonusPercent { get; set; }
    public decimal DeductionPercent { get; set; }
    public decimal TaxPercent { get; set; }
    public bool AllowDuplicateOverride { get; set; }
    public string? OverrideReason { get; set; }
    public List<PayrollRunEmployeeLineInput> Lines { get; set; } = [];
}

public class PayrollRunEmployeeLineInput
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public bool Include { get; set; } = true;
    public decimal BaseSalary { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public string? Notes { get; set; }
}

public class PayrollRunPaymentBatchInput
{
    public int PayrollRunId { get; set; }
    public int? FinancialAccountId { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public string PaymentMethod { get; set; } = "Bank";
    public string? Reference { get; set; }
}

public class ReportBuilderViewModel
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public decimal InvoiceRevenueTotal { get; set; }
    public decimal CreditNotesTotal { get; set; }
    public decimal NetSalesTotal { get; set; }
    public decimal PaidInvoicesTotal { get; set; }
    public decimal QuotesPipelineTotal { get; set; }
    public decimal OrdersPipelineTotal { get; set; }
    public decimal PurchaseOrdersTotal { get; set; }
    public decimal SupplierInvoicesTotal { get; set; }
    public decimal OpenObligationsTotal { get; set; }
    public decimal PayrollTotal { get; set; }
    public decimal InventoryValue { get; set; }
    public decimal CostOfGoodsSoldTotal { get; set; }
    public decimal GrossProfitTotal { get; set; }
    public decimal OperatingExpenseTotal { get; set; }
    public decimal NetProfitTotal { get; set; }
    public decimal CashInTotal { get; set; }
    public decimal CashOutTotal { get; set; }
    public decimal CashNetTotal { get; set; }
    public decimal AccountsReceivableTotal { get; set; }
    public decimal AccountsPayableTotal { get; set; }
    public decimal PurchaseVarianceTotal { get; set; }
    public int InvoiceCount { get; set; }
    public int QuoteCount { get; set; }
    public int OrderCount { get; set; }
    public int CreditNoteCount { get; set; }
    public int PurchaseOrderCount { get; set; }
    public int SupplierInvoiceCount { get; set; }
    public int EmployeeCount { get; set; }
    public int StockAlertCount { get; set; }
    public List<SalesDocument> RecentSalesDocuments { get; set; } = [];
    public List<PurchaseOrder> RecentPurchaseOrders { get; set; } = [];
    public List<SupplierInvoice> RecentSupplierInvoices { get; set; } = [];
    public List<JournalEntry> RecentJournalEntries { get; set; } = [];
    public List<ReceivableAgingRow> ReceivablesAging { get; set; } = [];
    public List<PayableAgingRow> PayablesAging { get; set; } = [];
    public List<InventoryValuationRow> InventoryValuation { get; set; } = [];
    public List<PurchaseVarianceRow> PurchaseVariance { get; set; } = [];
    public List<CustomerStatementRow> CustomerStatements { get; set; } = [];
}

public class ReceivableAgingRow
{
    public string CustomerName { get; set; } = string.Empty;
    public decimal Current { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61Plus { get; set; }
    public decimal Total => Current + Days1To30 + Days31To60 + Days61Plus;
}

public class PayableAgingRow
{
    public string PayeeName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public decimal Current { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61Plus { get; set; }
    public decimal Total => Current + Days1To30 + Days31To60 + Days61Plus;
}

public class InventoryValuationRow
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Value { get; set; }
    public int MinStock { get; set; }
    public bool IsLowStock { get; set; }
}

public class PurchaseVarianceRow
{
    public int PurchaseOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal OrderedValue { get; set; }
    public decimal ReceivedValue { get; set; }
    public decimal InvoicedValue { get; set; }
    public decimal Variance => OrderedValue - InvoicedValue;
    public string Status { get; set; } = string.Empty;
}

public class CustomerStatementRow
{
    public int? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal PosSalesTotal { get; set; }
    public decimal InvoiceTotal { get; set; }
    public decimal PaidInvoiceTotal { get; set; }
    public decimal BalanceDue { get; set; }
    public DateTime? LastActivity { get; set; }
}

public class CustomerStatementDetailsViewModel
{
    public Customer Customer { get; set; } = null!;
    public List<Sale> Sales { get; set; } = [];
    public List<SalesDocument> Invoices { get; set; } = [];
    public decimal PosSalesTotal { get; set; }
    public decimal InvoiceTotal { get; set; }
    public decimal PaidInvoiceTotal { get; set; }
    public decimal BalanceDue { get; set; }
}
