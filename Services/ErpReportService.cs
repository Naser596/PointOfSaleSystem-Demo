using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Services;

public class ErpReportService(ApplicationDbContext context) : IErpReportService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<ReportBuilderViewModel> BuildReportAsync(int companyId, DateTime? dateFrom, DateTime? dateTo)
    {
        var from = dateFrom?.Date ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var to = dateTo?.Date ?? DateTime.Today;
        var exclusiveTo = to.AddDays(1);

        var salesDocuments = await _context.SalesDocuments
            .Include(d => d.Customer)
            .Where(d => d.CompanyId == companyId && d.DocumentDate >= from && d.DocumentDate < exclusiveTo)
            .OrderByDescending(d => d.DocumentDate)
            .ThenByDescending(d => d.Id)
            .ToListAsync();
        var allOpenInvoices = await _context.SalesDocuments
            .Include(d => d.Customer)
            .Where(d => d.CompanyId == companyId &&
                d.DocumentType == "Invoice" &&
                d.PaymentStatus != "Paid" &&
                d.Status != "Cancelled")
            .ToListAsync();
        var purchaseOrders = await _context.PurchaseOrders
            .Include(p => p.Lines)
            .Where(p => p.CompanyId == companyId && p.OrderDate >= from && p.OrderDate < exclusiveTo)
            .OrderByDescending(p => p.OrderDate)
            .ThenByDescending(p => p.Id)
            .ToListAsync();
        var supplierInvoices = await _context.SupplierInvoices
            .Include(i => i.PurchaseOrder)
            .Include(i => i.GoodsReceipt)
            .Where(i => i.CompanyId == companyId && i.InvoiceDate >= from && i.InvoiceDate < exclusiveTo)
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .ToListAsync();
        var openSupplierInvoices = await _context.SupplierInvoices
            .Where(i => i.CompanyId == companyId && i.Status != "Paid" && i.Status != "Cancelled")
            .ToListAsync();
        var allOpenObligations = await _context.PayrollObligations
            .Where(o => o.CompanyId == companyId && o.Status != "Paid")
            .ToListAsync();
        var paidObligations = await _context.PayrollObligations
            .Where(o => o.CompanyId == companyId && o.Status == "Paid" && o.PaidDate >= from && o.PaidDate < exclusiveTo)
            .ToListAsync();
        var payrollRuns = await _context.PayrollRuns
            .Where(r => r.CompanyId == companyId && r.PeriodStart < exclusiveTo && r.PeriodEnd >= from)
            .ToListAsync();
        var saleItems = await _context.SaleItems
            .Include(i => i.Sale)
            .Where(i => i.Sale.CompanyId == companyId && i.Sale.SaleDate >= from && i.Sale.SaleDate < exclusiveTo)
            .ToListAsync();
        var payments = await _context.PaymentRecords
            .Where(p => p.CompanyId == companyId && p.PaymentDate >= from && p.PaymentDate < exclusiveTo && p.Status == "Completed")
            .ToListAsync();
        var inventoryRows = await _context.WarehouseStocks
            .Include(s => s.Warehouse)
            .Include(s => s.Product)
            .Where(s => s.CompanyId == companyId)
            .OrderBy(s => s.Product.Name)
            .ThenBy(s => s.Warehouse.Name)
            .Select(s => new InventoryValuationRow
            {
                ProductId = s.ProductId,
                ProductName = s.Product.Name,
                Sku = s.Product.SKU,
                WarehouseName = s.Warehouse.Name,
                QuantityOnHand = s.QuantityOnHand,
                QuantityReserved = s.QuantityReserved,
                UnitCost = s.Product.CostPrice,
                Value = s.QuantityOnHand * s.Product.CostPrice,
                MinStock = s.Product.MinStock,
                IsLowStock = s.QuantityOnHand <= s.Product.MinStock
            })
            .ToListAsync();
        var purchaseVariance = await BuildPurchaseVarianceAsync(companyId);
        var customerStatements = await BuildCustomerStatementsAsync(companyId, from, exclusiveTo);

        var invoices = salesDocuments.Where(d => d.DocumentType == "Invoice").ToList();
        var creditNotes = salesDocuments.Where(d => d.DocumentType == "CreditNote").ToList();
        var quotes = salesDocuments.Where(d => d.DocumentType == "Quote").ToList();
        var orders = salesDocuments.Where(d => d.DocumentType == "Order").ToList();
        var invoiceRevenueTotal = invoices.Sum(d => d.TotalAmount);
        var creditNotesTotal = creditNotes.Sum(d => Math.Abs(d.TotalAmount));
        var paidInvoicesTotal = invoices.Sum(d => d.PaidAmount);
        var costOfGoodsSold = saleItems.Sum(i => i.UnitCost * Math.Max(i.Quantity - i.ReturnedQuantity, 0));
        var payrollTotal = payrollRuns.Sum(r => r.NetAmount);
        var operatingExpenses = payrollTotal + paidObligations.Where(o => o.ObligationType != "Payroll").Sum(o => o.Amount);
        var grossProfit = invoiceRevenueTotal - creditNotesTotal - costOfGoodsSold;

        return new ReportBuilderViewModel
        {
            DateFrom = from,
            DateTo = to,
            InvoiceCount = invoices.Count,
            QuoteCount = quotes.Count,
            OrderCount = orders.Count,
            CreditNoteCount = creditNotes.Count,
            InvoiceRevenueTotal = invoiceRevenueTotal,
            CreditNotesTotal = creditNotesTotal,
            NetSalesTotal = invoiceRevenueTotal - creditNotesTotal,
            PaidInvoicesTotal = paidInvoicesTotal,
            QuotesPipelineTotal = quotes.Where(d => d.Status != "Closed" && d.Status != "Cancelled").Sum(d => d.TotalAmount),
            OrdersPipelineTotal = orders.Where(d => d.Status != "Closed" && d.Status != "Cancelled").Sum(d => d.TotalAmount),
            PurchaseOrderCount = purchaseOrders.Count,
            PurchaseOrdersTotal = purchaseOrders.Sum(p => p.TotalAmount),
            SupplierInvoiceCount = supplierInvoices.Count,
            SupplierInvoicesTotal = supplierInvoices.Sum(i => i.TotalAmount),
            OpenObligationsTotal = allOpenObligations.Sum(o => o.Amount),
            PayrollTotal = payrollTotal,
            InventoryValue = inventoryRows.Sum(r => r.Value),
            CostOfGoodsSoldTotal = costOfGoodsSold,
            GrossProfitTotal = grossProfit,
            OperatingExpenseTotal = operatingExpenses,
            NetProfitTotal = grossProfit - operatingExpenses,
            CashInTotal = payments.Where(p => p.Direction == "In").Sum(p => p.Amount),
            CashOutTotal = payments.Where(p => p.Direction == "Out").Sum(p => p.Amount),
            CashNetTotal = payments.Where(p => p.Direction == "In").Sum(p => p.Amount) - payments.Where(p => p.Direction == "Out").Sum(p => p.Amount),
            AccountsReceivableTotal = allOpenInvoices.Sum(d => Math.Max(d.TotalAmount - d.PaidAmount, 0)),
            AccountsPayableTotal = openSupplierInvoices.Sum(i => i.TotalAmount) + allOpenObligations.Sum(o => o.Amount),
            PurchaseVarianceTotal = purchaseVariance.Sum(v => v.Variance),
            EmployeeCount = await _context.Employees.CountAsync(e => e.CompanyId == companyId && e.IsActive),
            StockAlertCount = inventoryRows.Count(r => r.IsLowStock),
            RecentSalesDocuments = salesDocuments.Take(10).ToList(),
            RecentPurchaseOrders = purchaseOrders.Take(10).ToList(),
            RecentSupplierInvoices = supplierInvoices.Take(10).ToList(),
            RecentJournalEntries = await _context.JournalEntries
                .Where(j => j.CompanyId == companyId && j.EntryDate >= from && j.EntryDate < exclusiveTo)
                .OrderByDescending(j => j.EntryDate)
                .ThenByDescending(j => j.Id)
                .Take(10)
                .ToListAsync(),
            ReceivablesAging = BuildReceivableAging(allOpenInvoices),
            PayablesAging = BuildPayableAging(openSupplierInvoices, allOpenObligations),
            InventoryValuation = inventoryRows.Take(20).ToList(),
            PurchaseVariance = purchaseVariance.Take(20).ToList(),
            CustomerStatements = customerStatements.Take(20).ToList()
        };
    }

    private async Task<List<PurchaseVarianceRow>> BuildPurchaseVarianceAsync(int companyId)
    {
        var orders = await _context.PurchaseOrders
            .Include(o => o.Lines)
            .Where(o => o.CompanyId == companyId && o.Status != "Cancelled")
            .OrderByDescending(o => o.OrderDate)
            .Take(50)
            .ToListAsync();
        var orderIds = orders.Select(o => o.Id).ToList();
        var receipts = await _context.GoodsReceipts
            .Include(r => r.Lines)
            .Where(r => r.CompanyId == companyId && r.PurchaseOrderId.HasValue && orderIds.Contains(r.PurchaseOrderId.Value))
            .ToListAsync();
        var invoices = await _context.SupplierInvoices
            .Where(i => i.CompanyId == companyId && i.PurchaseOrderId.HasValue && orderIds.Contains(i.PurchaseOrderId.Value))
            .ToListAsync();

        return orders.Select(order =>
        {
            var receivedValue = receipts
                .Where(r => r.PurchaseOrderId == order.Id)
                .SelectMany(r => r.Lines)
                .Sum(l => l.Quantity * l.UnitCost);
            var invoicedValue = invoices
                .Where(i => i.PurchaseOrderId == order.Id)
                .Sum(i => i.TotalAmount);
            return new PurchaseVarianceRow
            {
                PurchaseOrderId = order.Id,
                OrderNumber = order.OrderNumber,
                SupplierName = order.SupplierName,
                OrderedValue = order.TotalAmount,
                ReceivedValue = receivedValue,
                InvoicedValue = invoicedValue,
                Status = order.Status
            };
        }).ToList();
    }

    private async Task<List<CustomerStatementRow>> BuildCustomerStatementsAsync(int companyId, DateTime from, DateTime exclusiveTo)
    {
        var sales = await _context.Sales
            .Include(s => s.Customer)
            .Where(s => s.CompanyId == companyId && s.SaleDate >= from && s.SaleDate < exclusiveTo)
            .ToListAsync();
        var docs = await _context.SalesDocuments
            .Include(d => d.Customer)
            .Where(d => d.CompanyId == companyId && d.DocumentDate >= from && d.DocumentDate < exclusiveTo && d.DocumentType == "Invoice")
            .ToListAsync();

        var keys = sales.Select(s => s.CustomerId)
            .Concat(docs.Select(d => d.CustomerId))
            .Distinct()
            .ToList();

        return keys.Select(customerId =>
        {
            var customerName = sales.FirstOrDefault(s => s.CustomerId == customerId)?.Customer?.Name
                ?? docs.FirstOrDefault(d => d.CustomerId == customerId)?.Customer?.Name
                ?? "Walk-in / no customer";
            var customerSales = sales.Where(s => s.CustomerId == customerId).ToList();
            var customerDocs = docs.Where(d => d.CustomerId == customerId).ToList();
            var lastSaleDate = customerSales.Select(s => (DateTime?)s.SaleDate).Max();
            var lastDocDate = customerDocs.Select(d => (DateTime?)d.DocumentDate).Max();
            return new CustomerStatementRow
            {
                CustomerId = customerId,
                CustomerName = customerName,
                PosSalesTotal = customerSales.Sum(s => s.TotalAmount - s.RefundedAmount),
                InvoiceTotal = customerDocs.Sum(d => d.TotalAmount),
                PaidInvoiceTotal = customerDocs.Sum(d => d.PaidAmount),
                BalanceDue = customerDocs.Sum(d => Math.Max(d.TotalAmount - d.PaidAmount, 0)),
                LastActivity = lastSaleDate > lastDocDate ? lastSaleDate : lastDocDate
            };
        })
        .OrderByDescending(r => r.BalanceDue)
        .ThenBy(r => r.CustomerName)
        .ToList();
    }

    private static List<ReceivableAgingRow> BuildReceivableAging(IEnumerable<SalesDocument> invoices)
    {
        return invoices
            .GroupBy(i => i.Customer?.Name ?? "Walk-in / no customer")
            .Select(group =>
            {
                var row = new ReceivableAgingRow { CustomerName = group.Key };
                foreach (var invoice in group)
                {
                    AddAgingAmount(row, invoice.DueDate ?? invoice.DocumentDate, Math.Max(invoice.TotalAmount - invoice.PaidAmount, 0));
                }
                return row;
            })
            .Where(r => r.Total > 0)
            .OrderByDescending(r => r.Total)
            .ToList();
    }

    private static List<PayableAgingRow> BuildPayableAging(IEnumerable<SupplierInvoice> supplierInvoices, IEnumerable<PayrollObligation> obligations)
    {
        var rows = new List<PayableAgingRow>();
        foreach (var group in supplierInvoices.GroupBy(i => i.SupplierName))
        {
            var row = new PayableAgingRow { PayeeName = group.Key, Source = "Supplier Invoices" };
            foreach (var invoice in group)
            {
                AddAgingAmount(row, invoice.DueDate ?? invoice.InvoiceDate, invoice.TotalAmount);
            }
            rows.Add(row);
        }

        foreach (var group in obligations.GroupBy(o => o.PayeeName ?? o.ObligationType))
        {
            var row = new PayableAgingRow { PayeeName = group.Key, Source = "Obligations" };
            foreach (var obligation in group)
            {
                AddAgingAmount(row, obligation.DueDate ?? obligation.PeriodEnd, obligation.Amount);
            }
            rows.Add(row);
        }

        return rows
            .Where(r => r.Total > 0)
            .OrderByDescending(r => r.Total)
            .ToList();
    }

    private static void AddAgingAmount(ReceivableAgingRow row, DateTime dueDate, decimal amount)
    {
        var days = (DateTime.Today - dueDate.Date).Days;
        if (days <= 0) row.Current += amount;
        else if (days <= 30) row.Days1To30 += amount;
        else if (days <= 60) row.Days31To60 += amount;
        else row.Days61Plus += amount;
    }

    private static void AddAgingAmount(PayableAgingRow row, DateTime dueDate, decimal amount)
    {
        var days = (DateTime.Today - dueDate.Date).Days;
        if (days <= 0) row.Current += amount;
        else if (days <= 30) row.Days1To30 += amount;
        else if (days <= 60) row.Days31To60 += amount;
        else row.Days61Plus += amount;
    }
}
