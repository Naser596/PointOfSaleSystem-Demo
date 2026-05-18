using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Services;

public class POSOperationsService(
    ApplicationDbContext context,
    ICustomerService customerService,
    IAuditLogService auditLog) : IPOSOperationsService
{
    private readonly ApplicationDbContext _context = context;
    private readonly ICustomerService _customerService = customerService;
    private readonly IAuditLogService _auditLog = auditLog;

    public async Task<Sale> CreateSaleAsync(int companyId, PosSaleInput input, string userName)
    {
        if (input.Items.Count == 0)
        {
            throw new InvalidOperationException("No items in cart.");
        }

        if (_context.Database.IsRelational())
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var sale = await CreateSaleCoreAsync(companyId, input, userName);
            await transaction.CommitAsync();
            return sale;
        }

        return await CreateSaleCoreAsync(companyId, input, userName);
    }

    public async Task<List<OfflineSaleSyncResult>> SyncOfflineSalesAsync(
        int companyId,
        IEnumerable<OfflineSaleSyncInput> requests,
        string userName)
    {
        var results = new List<OfflineSaleSyncResult>();
        foreach (var request in requests)
        {
            results.Add(await SyncOneOfflineSaleAsync(companyId, request, userName));
        }

        return results;
    }

    public async Task<OfflineSaleSyncResult> RetryOfflineSyncRecordAsync(int companyId, int recordId, string userName)
    {
        var record = await _context.OfflineSyncRecords
            .FirstOrDefaultAsync(r => r.Id == recordId && r.CompanyId == companyId);
        if (record == null)
        {
            throw new InvalidOperationException("Offline sync record was not found.");
        }

        if (string.Equals(record.Status, "Synced", StringComparison.OrdinalIgnoreCase))
        {
            return new OfflineSaleSyncResult
            {
                ClientId = record.ClientId,
                Success = true,
                SaleId = record.SaleId,
                Message = "Already synced.",
                Status = record.Status
            };
        }

        if (string.Equals(record.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cancelled offline sync records cannot be retried.");
        }

        var request = JsonSerializer.Deserialize<OfflineSaleSyncInput>(record.PayloadJson)
            ?? throw new InvalidOperationException("Offline sync payload is invalid.");
        request.ClientId = record.ClientId;

        return (await SyncOfflineSalesAsync(companyId, [request], userName)).Single();
    }

    public async Task CancelOfflineSyncRecordAsync(int companyId, int recordId, string userName)
    {
        var record = await _context.OfflineSyncRecords
            .FirstOrDefaultAsync(r => r.Id == recordId && r.CompanyId == companyId);
        if (record == null)
        {
            throw new InvalidOperationException("Offline sync record was not found.");
        }

        if (string.Equals(record.Status, "Synced", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Synced offline records cannot be cancelled.");
        }

        record.Status = "Cancelled";
        record.ProcessedAt = DateTime.Now;
        record.ErrorMessage = AppendSyncNote(record.ErrorMessage, $"Cancelled by {userName}.");
        await _context.SaveChangesAsync();
    }

    private async Task<OfflineSaleSyncResult> SyncOneOfflineSaleAsync(
        int companyId,
        OfflineSaleSyncInput request,
        string userName)
    {
        var clientId = string.IsNullOrWhiteSpace(request.ClientId)
            ? Guid.NewGuid().ToString("N")
            : request.ClientId.Trim();

        var record = await _context.OfflineSyncRecords
            .FirstOrDefaultAsync(r => r.CompanyId == companyId && r.ClientId == clientId);

        if (record?.Status == "Synced" && record.SaleId.HasValue)
        {
            var existingSale = await _context.Sales
                .Where(s => s.Id == record.SaleId.Value && s.CompanyId == companyId)
                .Select(s => new { s.Id, s.SaleNumber })
                .FirstOrDefaultAsync();

            return new OfflineSaleSyncResult
            {
                ClientId = clientId,
                Success = true,
                SaleId = existingSale?.Id ?? record.SaleId.Value,
                SaleNumber = existingSale?.SaleNumber,
                Message = "Already synced.",
                Status = "Synced"
            };
        }

        var payload = JsonSerializer.Serialize(request);
        if (record == null)
        {
            record = new OfflineSyncRecord
            {
                CompanyId = companyId,
                ClientId = clientId,
                SyncType = "Sale",
                Status = "Pending",
                QueuedAt = request.QueuedAt == default ? DateTime.Now : request.QueuedAt,
                ReceivedAt = DateTime.Now,
                PayloadJson = payload
            };
            _context.OfflineSyncRecords.Add(record);
        }
        else
        {
            record.Status = "Pending";
            record.ReceivedAt = DateTime.Now;
            record.PayloadJson = payload;
        }
        await _context.SaveChangesAsync();

        try
        {
            if (request.Items.Count == 0)
            {
                throw new InvalidOperationException("Queued sale has no items.");
            }

            Sale sale;
            if (_context.Database.IsRelational())
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                sale = await CreateSaleCoreAsync(companyId, request, userName);
                MarkRecordSynced(record, sale);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            else
            {
                sale = await CreateSaleCoreAsync(companyId, request, userName);
                MarkRecordSynced(record, sale);
                await _context.SaveChangesAsync();
            }

            await _auditLog.LogAsync("OfflineSync", nameof(OfflineSyncRecord), record.Id.ToString(), $"Synced offline sale {sale.SaleNumber}", companyId);
            return new OfflineSaleSyncResult
            {
                ClientId = clientId,
                Success = true,
                SaleId = sale.Id,
                SaleNumber = sale.SaleNumber,
                Message = "Synced.",
                Status = "Synced"
            };
        }
        catch (Exception ex)
        {
            var isConflict = IsStockConflict(ex);
            record.Status = isConflict ? "Conflict" : "Failed";
            record.ErrorMessage = AppendSyncNote(record.ErrorMessage, ex.Message);
            record.ProcessedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            await _auditLog.LogAsync("OfflineSyncFailed", nameof(OfflineSyncRecord), record.Id.ToString(), ex.Message, companyId);
            return new OfflineSaleSyncResult
            {
                ClientId = clientId,
                Success = false,
                Message = ex.Message,
                Status = record.Status
            };
        }
    }

    private async Task<Sale> CreateSaleCoreAsync(int companyId, PosSaleInput input, string userName)
    {
        var sale = new Sale
        {
            CompanyId = companyId,
            SaleNumber = await GenerateSaleNumberAsync(companyId),
            SaleDate = DateTime.Now,
            Status = "Completed",
            PaymentMethod = string.IsNullOrWhiteSpace(input.PaymentMethod) ? "Cash" : input.PaymentMethod.Trim(),
            CustomerId = input.CustomerId,
            CashierName = userName,
            SaleItems = []
        };

        Discount? discount = null;
        if (!string.IsNullOrWhiteSpace(input.DiscountCode))
        {
            var normalizedCode = input.DiscountCode.Trim().ToUpperInvariant();
            discount = await _context.Discounts
                .FirstOrDefaultAsync(d => d.CompanyId == companyId && d.Code.ToUpper() == normalizedCode);

            if (discount == null || !IsDiscountUsable(discount))
            {
                throw new InvalidOperationException("Invalid or expired discount code.");
            }

            sale.DiscountCode = discount.Code;
            sale.DiscountId = discount.Id;
        }

        decimal total = 0;
        decimal taxTotal = 0;
        var stockMovements = new List<StockMovement>();

        foreach (var item in input.Items)
        {
            if (item.Quantity <= 0)
            {
                throw new InvalidOperationException("Sale item quantity must be greater than zero.");
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == item.ProductId && p.CompanyId == companyId)
                ?? throw new InvalidOperationException($"Product not found: {item.ProductId}");

            if (product.Stock < item.Quantity)
            {
                throw new InvalidOperationException($"Insufficient stock for {product.Name}.");
            }

            var saleItem = new SaleItem
            {
                ProductId = product.Id,
                ProductNameSnapshot = product.Name,
                ProductSkuSnapshot = product.SKU,
                Quantity = item.Quantity,
                UnitPrice = product.Price,
                UnitCost = product.CostPrice,
                TaxRate = product.TaxRate,
                TotalPrice = product.Price * item.Quantity
            };
            saleItem.TaxAmount = CalculateIncludedTax(saleItem.TotalPrice, product.TaxRate);

            sale.SaleItems.Add(saleItem);
            total += saleItem.TotalPrice;
            taxTotal += saleItem.TaxAmount;

            var previousStock = product.Stock;
            product.Stock -= item.Quantity;
            product.UpdatedDate = DateTime.Now;

            stockMovements.Add(new StockMovement
            {
                CompanyId = companyId,
                ProductId = product.Id,
                MovementType = "Sale",
                Quantity = item.Quantity,
                PreviousStock = previousStock,
                NewStock = product.Stock,
                ReferenceType = "Sale",
                Notes = $"Sold in sale {sale.SaleNumber}",
                CreatedBy = userName,
                CreatedDate = DateTime.Now
            });
        }

        sale.SubTotal = total - taxTotal;
        sale.TaxAmount = taxTotal;
        sale.TotalAmount = total;

        if (discount != null)
        {
            ApplyDiscount(sale, discount, total, taxTotal);
        }

        _context.Sales.Add(sale);
        await _context.SaveChangesAsync();

        foreach (var movement in stockMovements)
        {
            movement.ReferenceId = sale.Id;
            _context.StockMovements.Add(movement);
        }

        if (discount != null)
        {
            discount.UsedCount++;
        }

        await _context.SaveChangesAsync();

        if (input.CustomerId.HasValue)
        {
            await _customerService.UpdateCustomerStatsAsync(input.CustomerId.Value, sale.TotalAmount);
        }

        await _auditLog.LogAsync("Create", nameof(Sale), sale.Id.ToString(), $"Created sale {sale.SaleNumber} with {sale.SaleItems.Count} items", companyId);
        return sale;
    }

    private static void MarkRecordSynced(OfflineSyncRecord record, Sale sale)
    {
        record.Status = "Synced";
        record.SaleId = sale.Id;
        record.ProcessedAt = DateTime.Now;
        record.ErrorMessage = null;
    }

    private async Task<string> GenerateSaleNumberAsync(int companyId)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var datePart = today.ToString("yyyyMMdd");
        var prefix = $"SALE-{datePart}-";
        var count = await _context.Sales
            .CountAsync(s => s.CompanyId == companyId && s.SaleDate >= today && s.SaleDate < tomorrow);

        return $"{prefix}{count + 1:0000}";
    }

    private static bool IsDiscountUsable(Discount discount)
    {
        if (!discount.IsActive)
        {
            return false;
        }

        var now = DateTime.Now;
        if (discount.StartDate.HasValue && discount.StartDate.Value > now)
        {
            return false;
        }

        if (discount.EndDate.HasValue && discount.EndDate.Value < now)
        {
            return false;
        }

        return !discount.UsageLimit.HasValue || discount.UsedCount < discount.UsageLimit.Value;
    }

    private static void ApplyDiscount(Sale sale, Discount discount, decimal total, decimal taxTotal)
    {
        if (discount.MinOrderAmount.HasValue && total < discount.MinOrderAmount.Value)
        {
            throw new InvalidOperationException($"Minimum order amount of {discount.MinOrderAmount:N2} required for this discount.");
        }

        var discountAmount = discount.DiscountType == DiscountType.Percentage
            ? total * (discount.Value / 100)
            : discount.Value;

        if (discount.MaxDiscountAmount.HasValue && discountAmount > discount.MaxDiscountAmount.Value)
        {
            discountAmount = discount.MaxDiscountAmount.Value;
        }

        if (discountAmount > total)
        {
            discountAmount = total;
        }

        sale.DiscountAmount = Math.Round(discountAmount, 2);
        sale.TotalAmount = total - sale.DiscountAmount;
        sale.TaxAmount = sale.TotalAmount == 0 ? 0 : Math.Round(taxTotal * (sale.TotalAmount / total), 2);
        sale.SubTotal = sale.TotalAmount - sale.TaxAmount;
    }

    private static decimal CalculateIncludedTax(decimal lineTotal, decimal taxRate)
    {
        if (taxRate <= 0 || lineTotal <= 0)
        {
            return 0;
        }

        return Math.Round(lineTotal - (lineTotal / (1 + taxRate / 100)), 2);
    }

    private static bool IsStockConflict(Exception ex)
    {
        return ex.Message.Contains("Insufficient stock", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("Product not found", StringComparison.OrdinalIgnoreCase);
    }

    private static string AppendSyncNote(string? current, string note)
    {
        var entry = $"{DateTime.Now:yyyy-MM-dd HH:mm}: {note}";
        if (string.IsNullOrWhiteSpace(current))
        {
            return entry;
        }

        return $"{current}\n{entry}";
    }
}
