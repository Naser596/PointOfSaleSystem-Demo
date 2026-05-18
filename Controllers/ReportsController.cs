using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.Text;
using WebApplication3.Data;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers;

[Authorize(Roles = "Admin,Manager,Accountant")]
public class ReportsController(
    ICurrentCompanyService currentCompany,
    IErpReportService reportService,
    ApplicationDbContext context,
    IAuditLogService auditLog) : Controller
{
    private readonly ICurrentCompanyService _currentCompany = currentCompany;
    private readonly IErpReportService _reportService = reportService;
    private readonly ApplicationDbContext _context = context;
    private readonly IAuditLogService _auditLog = auditLog;

    public async Task<IActionResult> Index(DateTime? dateFrom, DateTime? dateTo)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        return View(await _reportService.BuildReportAsync(companyId, dateFrom, dateTo));
    }

    public async Task<IActionResult> Export(DateTime? dateFrom, DateTime? dateTo)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var report = await _reportService.BuildReportAsync(companyId, dateFrom, dateTo);
        using var workbook = new XLWorkbook();

        var summary = workbook.Worksheets.Add("Summary");
        summary.Cell(1, 1).Value = "Metric";
        summary.Cell(1, 2).Value = "Value";
        var rows = new (string Label, decimal Value)[]
        {
            ("Net Sales", report.NetSalesTotal),
            ("Paid Invoices", report.PaidInvoicesTotal),
            ("Gross Profit", report.GrossProfitTotal),
            ("Operating Expenses", report.OperatingExpenseTotal),
            ("Net Profit", report.NetProfitTotal),
            ("Cash In", report.CashInTotal),
            ("Cash Out", report.CashOutTotal),
            ("Accounts Receivable", report.AccountsReceivableTotal),
            ("Accounts Payable", report.AccountsPayableTotal),
            ("Inventory Value", report.InventoryValue)
        };
        for (var i = 0; i < rows.Length; i++)
        {
            summary.Cell(i + 2, 1).Value = rows[i].Label;
            summary.Cell(i + 2, 2).Value = rows[i].Value;
        }

        var ar = workbook.Worksheets.Add("AR Aging");
        AddHeader(ar, "Customer", "Current", "1-30", "31-60", "61+", "Total");
        for (var i = 0; i < report.ReceivablesAging.Count; i++)
        {
            var row = report.ReceivablesAging[i];
            ar.Cell(i + 2, 1).Value = row.CustomerName;
            ar.Cell(i + 2, 2).Value = row.Current;
            ar.Cell(i + 2, 3).Value = row.Days1To30;
            ar.Cell(i + 2, 4).Value = row.Days31To60;
            ar.Cell(i + 2, 5).Value = row.Days61Plus;
            ar.Cell(i + 2, 6).Value = row.Total;
        }

        var inventory = workbook.Worksheets.Add("Inventory");
        AddHeader(inventory, "Product", "SKU", "Warehouse", "On Hand", "Reserved", "Cost", "Value", "Low Stock");
        for (var i = 0; i < report.InventoryValuation.Count; i++)
        {
            var row = report.InventoryValuation[i];
            inventory.Cell(i + 2, 1).Value = row.ProductName;
            inventory.Cell(i + 2, 2).Value = row.Sku;
            inventory.Cell(i + 2, 3).Value = row.WarehouseName;
            inventory.Cell(i + 2, 4).Value = row.QuantityOnHand;
            inventory.Cell(i + 2, 5).Value = row.QuantityReserved;
            inventory.Cell(i + 2, 6).Value = row.UnitCost;
            inventory.Cell(i + 2, 7).Value = row.Value;
            inventory.Cell(i + 2, 8).Value = row.IsLowStock ? "Yes" : "No";
        }

        var customers = workbook.Worksheets.Add("Customer Statements");
        AddHeader(customers, "Customer", "POS Sales", "Invoices", "Paid", "Balance Due", "Last Activity");
        for (var i = 0; i < report.CustomerStatements.Count; i++)
        {
            var row = report.CustomerStatements[i];
            customers.Cell(i + 2, 1).Value = row.CustomerName;
            customers.Cell(i + 2, 2).Value = row.PosSalesTotal;
            customers.Cell(i + 2, 3).Value = row.InvoiceTotal;
            customers.Cell(i + 2, 4).Value = row.PaidInvoiceTotal;
            customers.Cell(i + 2, 5).Value = row.BalanceDue;
            customers.Cell(i + 2, 6).Value = row.LastActivity;
        }

        foreach (var worksheet in workbook.Worksheets) worksheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        await _auditLog.LogAsync("Export", "Report", null, $"Exported ERP report {report.DateFrom:yyyy-MM-dd} to {report.DateTo:yyyy-MM-dd}", companyId);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"erp-report-{report.DateFrom:yyyyMMdd}-{report.DateTo:yyyyMMdd}.xlsx");
    }

    public async Task<IActionResult> AccountingExport(DateTime? dateFrom, DateTime? dateTo)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var from = (dateFrom ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
        var to = (dateTo ?? DateTime.Today).Date;
        var exclusiveTo = to.AddDays(1);

        var lines = await _context.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Include(l => l.Account)
            .Where(l =>
                l.JournalEntry.CompanyId == companyId &&
                l.JournalEntry.EntryDate >= from &&
                l.JournalEntry.EntryDate < exclusiveTo)
            .OrderBy(l => l.JournalEntry.EntryDate)
            .ThenBy(l => l.JournalEntry.EntryNumber)
            .ThenBy(l => l.Id)
            .ToListAsync();

        var csv = new StringBuilder();
        AppendCsvRow(csv, "Entry Date", "Entry Number", "Status", "Account Code", "Account Name", "Account Type", "Memo", "Debit", "Credit", "Source Type", "Source ID");
        foreach (var line in lines)
        {
            AppendCsvRow(
                csv,
                line.JournalEntry.EntryDate.ToString("yyyy-MM-dd"),
                line.JournalEntry.EntryNumber,
                line.JournalEntry.Status,
                line.Account.Code,
                line.Account.Name,
                line.Account.AccountType,
                line.Memo ?? string.Empty,
                line.Debit.ToString("0.00"),
                line.Credit.ToString("0.00"),
                line.JournalEntry.SourceType ?? string.Empty,
                line.JournalEntry.SourceId ?? string.Empty);
        }

        await _auditLog.LogAsync("Export", nameof(JournalEntry), null, $"Exported accounting CSV {from:yyyy-MM-dd} to {to:yyyy-MM-dd}", companyId);
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"accounting-export-{from:yyyyMMdd}-{to:yyyyMMdd}.csv");
    }

    private static void AddHeader(IXLWorksheet sheet, params string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }
        sheet.Range(1, 1, 1, headers.Length).Style.Font.Bold = true;
    }

    private static void AppendCsvRow(StringBuilder builder, params string[] values)
    {
        builder.AppendLine(string.Join(",", values.Select(EscapeCsv)));
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
