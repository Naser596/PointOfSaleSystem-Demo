using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Tests.Services;

public sealed class SimplePdfServiceTests
{
    [Fact]
    public void BuildSalesDocument_ReturnsPdfBytes()
    {
        var company = Company();
        var document = new SalesDocument
        {
            DocumentType = "Invoice",
            DocumentNumber = "INV-TEST",
            DocumentDate = new DateTime(2026, 5, 1),
            Status = "Issued",
            PaymentStatus = "Unpaid",
            SubTotal = 100,
            TaxAmount = 18,
            TotalAmount = 118,
            Lines =
            [
                new SalesDocumentLine
                {
                    Description = "Service",
                    Quantity = 1,
                    UnitPrice = 100,
                    TaxAmount = 18,
                    LineTotal = 118
                }
            ]
        };

        var pdf = SimplePdfService.BuildSalesDocument(document, company);

        Assert.StartsWith("%PDF-", System.Text.Encoding.ASCII.GetString(pdf, 0, 5));
        Assert.True(pdf.Length > 500);
    }

    [Fact]
    public void BuildPurchaseOrder_ReturnsPdfBytes()
    {
        var order = new PurchaseOrder
        {
            OrderNumber = "PO-TEST",
            SupplierName = "Supplier",
            OrderDate = new DateTime(2026, 5, 1),
            Status = "Draft",
            SubTotal = 50,
            TaxAmount = 5,
            TotalAmount = 55,
            Lines =
            [
                new PurchaseOrderLine
                {
                    Description = "Item",
                    Quantity = 2,
                    UnitCost = 25,
                    TaxAmount = 5,
                    LineTotal = 55
                }
            ]
        };

        var pdf = SimplePdfService.BuildPurchaseOrder(order, Company());

        Assert.StartsWith("%PDF-", System.Text.Encoding.ASCII.GetString(pdf, 0, 5));
        Assert.True(pdf.Length > 500);
    }

    [Fact]
    public void BuildSupplierInvoice_ReturnsPdfBytes()
    {
        var invoice = new SupplierInvoice
        {
            InvoiceNumber = "PINV-TEST",
            SupplierName = "Supplier",
            InvoiceDate = new DateTime(2026, 5, 1),
            Status = "Draft",
            MatchStatus = "Unmatched",
            SubTotal = 70,
            TaxAmount = 7,
            TotalAmount = 77,
            Items =
            [
                new SupplierInvoiceItem
                {
                    Description = "Item",
                    Quantity = 1,
                    UnitCost = 70,
                    TaxAmount = 7,
                    LineTotal = 77
                }
            ]
        };

        var pdf = SimplePdfService.BuildSupplierInvoice(invoice, CompanySettings());

        Assert.StartsWith("%PDF-", System.Text.Encoding.ASCII.GetString(pdf, 0, 5));
        Assert.True(pdf.Length > 500);
    }

    private static Company Company()
    {
        return new Company
        {
            DisplayName = "Test Company",
            CurrencyCode = "EUR",
            InvoicePrefix = "INV"
        };
    }

    private static CompanySettings CompanySettings()
    {
        return new CompanySettings
        {
            DisplayName = "Test Company",
            CurrencyCode = "EUR",
            InvoicePrefix = "INV"
        };
    }
}
