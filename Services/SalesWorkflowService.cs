using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Services;

public class SalesWorkflowService(
    ApplicationDbContext context,
    IErpAccountingService accounting) : ISalesWorkflowService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IErpAccountingService _accounting = accounting;

    public async Task<SalesDocument> ConvertDocumentAsync(int sourceDocumentId, string targetDocumentType, string? userName = null)
    {
        var source = await _context.SalesDocuments
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.Id == sourceDocumentId);
        if (source == null)
        {
            throw new InvalidOperationException("Source sales document was not found.");
        }

        if (string.Equals(source.Status, "Closed", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(source.DocumentType, "Invoice", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Closed sales documents cannot be converted again.");
        }

        targetDocumentType = NormalizeTargetType(source.DocumentType, targetDocumentType);
        var alreadyConverted = await _context.SalesDocuments.AnyAsync(d =>
            d.CompanyId == source.CompanyId &&
            d.ConvertedFromDocumentId == source.Id &&
            d.DocumentType == targetDocumentType &&
            d.Status != "Cancelled");
        if (alreadyConverted)
        {
            throw new InvalidOperationException($"This document has already been converted to {targetDocumentType}.");
        }

        var sign = targetDocumentType == "CreditNote" ? -1 : 1;
        var converted = new SalesDocument
        {
            CompanyId = source.CompanyId,
            DocumentType = targetDocumentType,
            DocumentNumber = await GenerateDocumentNumberAsync(source.CompanyId, targetDocumentType),
            CustomerId = source.CustomerId,
            DocumentDate = DateTime.Today,
            DueDate = targetDocumentType == "Invoice" ? DateTime.Today.AddDays(14) : source.DueDate,
            Status = "Draft",
            PaymentStatus = targetDocumentType == "Invoice" ? "Unpaid" : "NotApplicable",
            PaidAmount = 0,
            ConvertedFromDocumentId = source.Id,
            SubTotal = source.SubTotal * sign,
            TaxAmount = source.TaxAmount * sign,
            DiscountAmount = source.DiscountAmount * sign,
            TotalAmount = source.TotalAmount * sign,
            Notes = $"Converted from {source.DocumentNumber}",
            CreatedBy = userName,
            CreatedDate = DateTime.Now,
            Lines = source.Lines.Select(line => new SalesDocumentLine
            {
                ProductId = line.ProductId,
                Description = line.Description,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                TaxRate = line.TaxRate,
                TaxAmount = line.TaxAmount * sign,
                LineTotal = line.LineTotal * sign
            }).ToList()
        };

        source.Status = targetDocumentType == "Order" ? "Accepted" : "Closed";
        _context.SalesDocuments.Add(converted);
        await _context.SaveChangesAsync();
        return converted;
    }

    public async Task<PaymentRecord> RecordPaymentAsync(SalesDocumentPaymentInput input, string? userName = null)
    {
        if (input.SalesDocumentId <= 0 || input.Amount <= 0)
        {
            throw new InvalidOperationException("Sales document and positive payment amount are required.");
        }

        var document = await _context.SalesDocuments
            .FirstOrDefaultAsync(d => d.Id == input.SalesDocumentId);
        if (document == null)
        {
            throw new InvalidOperationException("Sales document was not found.");
        }

        if (!string.Equals(document.DocumentType, "Invoice", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only invoices can receive payments.");
        }

        if (input.FinancialAccountId.HasValue)
        {
            var accountExists = await _context.FinancialAccounts.AnyAsync(a =>
                a.CompanyId == document.CompanyId &&
                a.Id == input.FinancialAccountId.Value);
            if (!accountExists)
            {
                throw new InvalidOperationException("Financial account belongs to another company or does not exist.");
            }
        }

        var remaining = document.TotalAmount - document.PaidAmount;
        var amount = Math.Min(input.Amount, remaining);
        if (amount <= 0)
        {
            throw new InvalidOperationException("Invoice is already paid.");
        }

        if (_context.Database.IsRelational())
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var payment = await RecordPaymentCoreAsync(document, input, amount, userName);
            await transaction.CommitAsync();
            return payment;
        }

        return await RecordPaymentCoreAsync(document, input, amount, userName);
    }

    private async Task<PaymentRecord> RecordPaymentCoreAsync(
        SalesDocument document,
        SalesDocumentPaymentInput input,
        decimal amount,
        string? userName)
    {
        var paymentDate = input.PaymentDate == default ? DateTime.Today : input.PaymentDate.Date;
        var payment = new PaymentRecord
        {
            CompanyId = document.CompanyId,
            FinancialAccountId = input.FinancialAccountId,
            Direction = "In",
            PaymentMethod = string.IsNullOrWhiteSpace(input.PaymentMethod) ? "Cash" : input.PaymentMethod.Trim(),
            PaymentDate = paymentDate,
            Amount = amount,
            Status = "Completed",
            EntityName = nameof(SalesDocument),
            EntityId = document.Id.ToString(),
            ProviderName = "Manual",
            ProviderStatus = "ReadyForProvider",
            CreatedBy = userName,
            CreatedDate = DateTime.Now
        };
        _context.PaymentRecords.Add(payment);

        if (input.FinancialAccountId.HasValue)
        {
            _context.BankTransactions.Add(new BankTransaction
            {
                CompanyId = document.CompanyId,
                FinancialAccountId = input.FinancialAccountId.Value,
                TransactionDate = paymentDate,
                Amount = amount,
                TransactionType = "Credit",
                Status = "Unreconciled",
                Description = $"Payment for {document.DocumentNumber}",
                Reference = input.Reference,
                CreatedDate = DateTime.Now
            });
        }

        document.PaidAmount += amount;
        document.PaymentStatus = document.PaidAmount >= document.TotalAmount ? "Paid" : "PartiallyPaid";
        if (document.PaymentStatus == "Paid")
        {
            document.Status = "Closed";
        }

        await _context.SaveChangesAsync();

        await _accounting.CreateBalancedEntryAsync(
            document.CompanyId,
            nameof(PaymentRecord),
            payment.Id.ToString(),
            $"Payment received for {document.DocumentNumber}",
            [
                new("1000", "Cash and Bank", "Asset", document.DocumentNumber, amount, 0),
                new("1100", "Accounts Receivable", "Asset", document.DocumentNumber, 0, amount)
            ],
            userName);

        return payment;
    }

    private async Task<string> GenerateDocumentNumberAsync(int companyId, string documentType)
    {
        var prefix = documentType switch
        {
            "Order" => "SO",
            "Invoice" => "INV",
            "CreditNote" => "CN",
            _ => "SD"
        };
        var datePart = DateTime.Today.ToString("yyyyMMdd");
        var startsWith = $"{prefix}-{datePart}-";
        var count = await _context.SalesDocuments.CountAsync(d =>
            d.CompanyId == companyId &&
            d.DocumentNumber.StartsWith(startsWith));
        return $"{startsWith}{count + 1:000}";
    }

    private static string NormalizeTargetType(string sourceType, string targetType)
    {
        targetType = string.IsNullOrWhiteSpace(targetType) ? "Invoice" : targetType.Trim();
        if (sourceType == "Quote" && targetType is "Order" or "Invoice") return targetType;
        if (sourceType == "Order" && targetType == "Invoice") return targetType;
        if (sourceType == "Invoice" && targetType == "CreditNote") return targetType;
        throw new InvalidOperationException($"Cannot convert {sourceType} to {targetType}.");
    }
}
