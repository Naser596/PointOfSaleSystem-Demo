using System.Globalization;
using System.Text;
using WebApplication3.Models;

namespace WebApplication3.Services;

public static class SimplePdfService
{
    public static byte[] BuildSalesDocument(SalesDocument document, Company company)
    {
        var pdf = new SimplePdfBuilder();
        AddCompanyHeader(pdf, CompanyInfo.FromCompany(company), document.DocumentType, document.DocumentNumber);
        pdf.AddKeyValue("Customer", document.Customer?.Name ?? "-");
        if (!string.IsNullOrWhiteSpace(document.Customer?.Email)) pdf.AddKeyValue("Customer Email", document.Customer.Email);
        if (!string.IsNullOrWhiteSpace(document.Customer?.Phone)) pdf.AddKeyValue("Customer Phone", document.Customer.Phone);
        pdf.AddKeyValue("Date", FormatDate(document.DocumentDate));
        pdf.AddKeyValue("Due Date", FormatDate(document.DueDate));
        pdf.AddKeyValue("Status", document.Status);
        pdf.AddKeyValue("Payment", $"{document.PaymentStatus} / Paid {Money(document.PaidAmount, company.CurrencyCode)}");
        pdf.Space();
        pdf.AddTable(
            ["Description", "Qty", "Unit", "Tax", "Total"],
            document.Lines.Select(line => new[]
            {
                line.Description,
                line.Quantity.ToString("N2", CultureInfo.InvariantCulture),
                Money(line.UnitPrice, company.CurrencyCode),
                Money(line.TaxAmount, company.CurrencyCode),
                Money(line.LineTotal, company.CurrencyCode)
            }));
        AddTotals(pdf, company.CurrencyCode, document.SubTotal, document.TaxAmount, document.DiscountAmount, document.TotalAmount);
        AddNotes(pdf, document.Notes ?? company.InvoiceFooterNote);
        return pdf.Build();
    }

    public static byte[] BuildPurchaseOrder(PurchaseOrder order, Company? company)
    {
        var currency = company?.CurrencyCode ?? "USD";
        var pdf = new SimplePdfBuilder();
        AddCompanyHeader(pdf, CompanyInfo.FromCompany(company), "Purchase Order", order.OrderNumber);
        pdf.AddKeyValue("Supplier", order.SupplierName);
        if (!string.IsNullOrWhiteSpace(order.SupplierTaxNumber)) pdf.AddKeyValue("Supplier Tax No.", order.SupplierTaxNumber);
        pdf.AddKeyValue("Order Date", FormatDate(order.OrderDate));
        pdf.AddKeyValue("Expected Date", FormatDate(order.ExpectedDate));
        pdf.AddKeyValue("Status", order.Status);
        pdf.Space();
        pdf.AddTable(
            ["Description", "Qty", "Received", "Unit Cost", "Tax", "Total"],
            order.Lines.Select(line => new[]
            {
                line.Description,
                line.Quantity.ToString("N2", CultureInfo.InvariantCulture),
                line.ReceivedQuantity.ToString("N2", CultureInfo.InvariantCulture),
                Money(line.UnitCost, currency),
                Money(line.TaxAmount, currency),
                Money(line.LineTotal, currency)
            }));
        AddTotals(pdf, currency, order.SubTotal, order.TaxAmount, null, order.TotalAmount);
        AddNotes(pdf, order.Notes);
        return pdf.Build();
    }

    public static byte[] BuildSupplierInvoice(SupplierInvoice invoice, CompanySettings company)
    {
        var pdf = new SimplePdfBuilder();
        AddCompanyHeader(pdf, CompanyInfo.FromSettings(company), "Supplier Invoice", invoice.InvoiceNumber);
        pdf.AddKeyValue("Supplier", invoice.SupplierName);
        if (!string.IsNullOrWhiteSpace(invoice.SupplierInvoiceNumber)) pdf.AddKeyValue("Supplier Invoice No.", invoice.SupplierInvoiceNumber);
        if (!string.IsNullOrWhiteSpace(invoice.SupplierTaxNumber)) pdf.AddKeyValue("Supplier Tax No.", invoice.SupplierTaxNumber);
        if (!string.IsNullOrWhiteSpace(invoice.SupplierAddress)) pdf.AddKeyValue("Supplier Address", invoice.SupplierAddress);
        pdf.AddKeyValue("Invoice Date", FormatDate(invoice.InvoiceDate));
        pdf.AddKeyValue("Due Date", FormatDate(invoice.DueDate));
        pdf.AddKeyValue("Status", invoice.Status);
        pdf.AddKeyValue("Match Status", invoice.MatchStatus);
        pdf.Space();
        pdf.AddTable(
            ["Description", "Qty", "Unit Cost", "Tax", "Total"],
            invoice.Items.Select(item => new[]
            {
                item.Description,
                item.Quantity.ToString("N2", CultureInfo.InvariantCulture),
                Money(item.UnitCost, company.CurrencyCode),
                Money(item.TaxAmount, company.CurrencyCode),
                Money(item.LineTotal, company.CurrencyCode)
            }));
        AddTotals(pdf, company.CurrencyCode, invoice.SubTotal, invoice.TaxAmount, null, invoice.TotalAmount);
        AddNotes(pdf, invoice.Notes ?? company.SupplierInvoiceFooterNote);
        return pdf.Build();
    }

    private static void AddCompanyHeader(SimplePdfBuilder pdf, CompanyInfo company, string documentType, string documentNumber)
    {
        pdf.AddTitle(documentType);
        pdf.AddText(documentNumber, 14);
        pdf.Space();
        pdf.AddText(company.DisplayName, 12);
        if (!string.IsNullOrWhiteSpace(company.LegalName)) pdf.AddText(company.LegalName, 10);
        if (!string.IsNullOrWhiteSpace(company.TaxNumber)) pdf.AddText($"Tax No: {company.TaxNumber}", 10);
        var address = string.Join(", ", new[] { company.Address, company.City, company.Country }.Where(v => !string.IsNullOrWhiteSpace(v)));
        if (!string.IsNullOrWhiteSpace(address)) pdf.AddText(address, 10);
        var contact = string.Join(" | ", new[] { company.Phone, company.Email }.Where(v => !string.IsNullOrWhiteSpace(v)));
        if (!string.IsNullOrWhiteSpace(contact)) pdf.AddText(contact, 10);
        pdf.HorizontalRule();
    }

    private static void AddTotals(SimplePdfBuilder pdf, string currency, decimal subTotal, decimal taxAmount, decimal? discountAmount, decimal totalAmount)
    {
        pdf.Space();
        pdf.AddRightText($"Subtotal: {Money(subTotal, currency)}");
        pdf.AddRightText($"Tax: {Money(taxAmount, currency)}");
        if (discountAmount.HasValue) pdf.AddRightText($"Discount: {Money(discountAmount.Value, currency)}");
        pdf.AddRightText($"Total: {Money(totalAmount, currency)}", 12);
    }

    private static void AddNotes(SimplePdfBuilder pdf, string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes)) return;
        pdf.Space();
        pdf.AddText("Notes", 12);
        foreach (var line in SimplePdfBuilder.Wrap(notes, 92))
        {
            pdf.AddText(line, 10);
        }
    }

    private static string Money(decimal amount, string? currency)
    {
        return $"{(string.IsNullOrWhiteSpace(currency) ? "USD" : currency)} {amount:N2}";
    }

    private static string FormatDate(DateTime? date)
    {
        return date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-";
    }

    private sealed record CompanyInfo(
        string DisplayName,
        string? LegalName,
        string? TaxNumber,
        string? Address,
        string? City,
        string? Country,
        string? Phone,
        string? Email)
    {
        public static CompanyInfo FromCompany(Company? company)
        {
            return new CompanyInfo(
                company?.DisplayName ?? "Company",
                company?.LegalName,
                company?.TaxNumber,
                company?.Address,
                company?.City,
                company?.Country,
                company?.Phone,
                company?.Email);
        }

        public static CompanyInfo FromSettings(CompanySettings company)
        {
            return new CompanyInfo(
                company.DisplayName,
                company.LegalName,
                company.TaxNumber,
                company.Address,
                company.City,
                company.Country,
                company.Phone,
                company.Email);
        }
    }
}

public sealed class SimplePdfBuilder
{
    private const int PageWidth = 595;
    private const int PageHeight = 842;
    private const int Left = 50;
    private const int Right = 545;
    private readonly List<StringBuilder> _pages = [];
    private StringBuilder _current = new();
    private int _y = 790;

    public SimplePdfBuilder()
    {
        _pages.Add(_current);
    }

    public void AddTitle(string text)
    {
        AddText(text, 20);
    }

    public void AddText(string text, int fontSize = 10)
    {
        EnsureSpace(fontSize + 6);
        _current.AppendLine($"BT /F1 {fontSize} Tf {Left} {_y} Td {PdfText(text)} Tj ET");
        _y -= fontSize + 6;
    }

    public void AddRightText(string text, int fontSize = 10)
    {
        EnsureSpace(fontSize + 6);
        _current.AppendLine($"BT /F1 {fontSize} Tf 350 {_y} Td {PdfText(text)} Tj ET");
        _y -= fontSize + 6;
    }

    public void AddKeyValue(string key, string value)
    {
        EnsureSpace(16);
        _current.AppendLine($"BT /F1 10 Tf {Left} {_y} Td {PdfText($"{key}:")} Tj ET");
        _current.AppendLine($"BT /F1 10 Tf 150 {_y} Td {PdfText(value)} Tj ET");
        _y -= 16;
    }

    public void AddTable(string[] headers, IEnumerable<string[]> rows)
    {
        AddTableRow(headers, true);
        HorizontalRule();
        foreach (var row in rows)
        {
            var wrappedFirst = Wrap(row.FirstOrDefault() ?? "-", 34).ToList();
            var rowHeight = Math.Max(1, wrappedFirst.Count) * 14;
            EnsureSpace(rowHeight + 4);
            for (var i = 0; i < wrappedFirst.Count; i++)
            {
                _current.AppendLine($"BT /F1 9 Tf {Left} {_y - (i * 14)} Td {PdfText(wrappedFirst[i])} Tj ET");
            }

            var x = 260;
            for (var i = 1; i < row.Length; i++)
            {
                _current.AppendLine($"BT /F1 9 Tf {x} {_y} Td {PdfText(row[i])} Tj ET");
                x += i == 1 ? 55 : 75;
            }
            _y -= rowHeight + 4;
        }
    }

    public void Space(int height = 10)
    {
        EnsureSpace(height);
        _y -= height;
    }

    public void HorizontalRule()
    {
        EnsureSpace(12);
        _current.AppendLine($"{Left} {_y} m {Right} {_y} l S");
        _y -= 12;
    }

    public byte[] Build()
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            BuildPagesObject(),
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };

        for (var i = 0; i < _pages.Count; i++)
        {
            var pageId = 4 + i * 2;
            var contentId = pageId + 1;
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth} {PageHeight}] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentId} 0 R >>");
            var stream = _pages[i].ToString();
            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}endstream");
        }

        return WritePdf(objects);
    }

    public static IEnumerable<string> Wrap(string text, int width)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield return "-";
            yield break;
        }

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var line = string.Empty;
        foreach (var word in words)
        {
            if ((line.Length + word.Length + 1) > width && line.Length > 0)
            {
                yield return line;
                line = word;
            }
            else
            {
                line = string.IsNullOrWhiteSpace(line) ? word : $"{line} {word}";
            }
        }
        if (!string.IsNullOrWhiteSpace(line)) yield return line;
    }

    private void AddTableRow(string[] values, bool header)
    {
        EnsureSpace(18);
        var x = Left;
        var fontSize = header ? 10 : 9;
        foreach (var value in values)
        {
            _current.AppendLine($"BT /F1 {fontSize} Tf {x} {_y} Td {PdfText(value)} Tj ET");
            x += x == Left ? 210 : 75;
        }
        _y -= 18;
    }

    private void EnsureSpace(int required)
    {
        if (_y - required > 45) return;
        _current = new StringBuilder();
        _pages.Add(_current);
        _y = 790;
    }

    private string BuildPagesObject()
    {
        var kids = string.Join(" ", Enumerable.Range(0, _pages.Count).Select(i => $"{4 + i * 2} 0 R"));
        return $"<< /Type /Pages /Kids [{kids}] /Count {_pages.Count} >>";
    }

    private static byte[] WritePdf(List<string> objects)
    {
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.Write("%PDF-1.4\n%\u00e2\u00e3\u00cf\u00d3\n");
        writer.Flush();

        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(stream.Position);
            writer.Write($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            writer.Flush();
        }

        var xref = stream.Position;
        writer.Write($"xref\n0 {objects.Count + 1}\n");
        writer.Write("0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            writer.Write($"{offset:0000000000} 00000 n \n");
        }
        writer.Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        writer.Flush();
        return stream.ToArray();
    }

    private static string PdfText(string? text)
    {
        var bytes = Encoding.BigEndianUnicode.GetBytes("\uFEFF" + (text ?? string.Empty));
        return $"<{Convert.ToHexString(bytes)}>";
    }
}
