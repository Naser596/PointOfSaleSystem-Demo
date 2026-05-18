using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Services
{
    public class SupplierInvoiceService : ISupplierInvoiceService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentCompanyService _currentCompany;

        public SupplierInvoiceService(ApplicationDbContext context, ICurrentCompanyService currentCompany)
        {
            _context = context;
            _currentCompany = currentCompany;
        }

        public async Task<List<SupplierInvoice>> GetAllAsync()
        {
            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
            return await _context.SupplierInvoices
                .Where(i => i.CompanyId == companyId)
                .OrderByDescending(i => i.InvoiceDate)
                .ThenByDescending(i => i.Id)
                .ToListAsync();
        }

        public async Task<SupplierInvoice?> GetByIdAsync(int id)
        {
            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
            return await _context.SupplierInvoices
                .Include(i => i.PurchaseOrder)
                .Include(i => i.GoodsReceipt)
                .Include(i => i.Items)
                    .ThenInclude(item => item.Product)
                .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == companyId);
        }

        public async Task<SupplierInvoiceCreateViewModel> BuildCreateModelAsync()
        {
            var model = new SupplierInvoiceCreateViewModel();
            await PopulateMatchingOptionsAsync(model);
            return model;
        }

        public async Task PopulateMatchingOptionsAsync(SupplierInvoiceCreateViewModel model)
        {
            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
            model.AvailablePurchaseOrders = await _context.PurchaseOrders
                .Where(p => p.CompanyId == companyId && p.Status != "Cancelled")
                .OrderByDescending(p => p.OrderDate)
                .Take(100)
                .ToListAsync();

            model.AvailableGoodsReceipts = await _context.GoodsReceipts
                .Include(r => r.PurchaseOrder)
                .Where(r => r.CompanyId == companyId)
                .OrderByDescending(r => r.ReceiptDate)
                .Take(100)
                .ToListAsync();

            model.AvailableProducts = await _context.Products
                .Where(p => p.CompanyId == companyId && p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();

            model.AvailableWarehouses = await _context.Warehouses
                .Where(w => w.CompanyId == companyId && w.IsActive)
                .OrderBy(w => w.Name)
                .ToListAsync();

            model.AvailableStockLocations = await _context.StockLocations
                .Include(l => l.Warehouse)
                .Where(l => l.CompanyId == companyId && l.IsActive)
                .OrderBy(l => l.Warehouse.Name)
                .ThenBy(l => l.Name)
                .ToListAsync();
        }

        public async Task<SupplierInvoice> CreateAsync(SupplierInvoiceCreateViewModel model, string? createdBy = null)
        {
            var validItems = model.Items
                .Where(i =>
                    i.Quantity > 0 &&
                    (!string.IsNullOrWhiteSpace(i.Description) ||
                     i.ProductId.HasValue ||
                     i.CreateNewProduct ||
                     !string.IsNullOrWhiteSpace(i.NewProductName)))
                .ToList();

            if (validItems.Count == 0)
            {
                throw new InvalidOperationException("At least one invoice item is required.");
            }

            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
            var hasInventoryItems = validItems.Any(i => i.ProductId.HasValue || i.CreateNewProduct);
            var supplierName = string.IsNullOrWhiteSpace(model.SupplierName)
                ? "Direct Supplier"
                : model.SupplierName.Trim();

            PurchaseOrder? purchaseOrder = null;
            if (model.PurchaseOrderId.HasValue)
            {
                purchaseOrder = await _context.PurchaseOrders
                    .FirstOrDefaultAsync(p => p.Id == model.PurchaseOrderId.Value && p.CompanyId == companyId);
                if (purchaseOrder == null)
                {
                    throw new InvalidOperationException("Matched purchase order was not found.");
                }
            }

            GoodsReceipt? goodsReceipt = null;
            if (model.GoodsReceiptId.HasValue)
            {
                goodsReceipt = await _context.GoodsReceipts
                    .Include(r => r.PurchaseOrder)
                    .FirstOrDefaultAsync(r => r.Id == model.GoodsReceiptId.Value && r.CompanyId == companyId);
                if (goodsReceipt == null)
                {
                    throw new InvalidOperationException("Matched goods receipt was not found.");
                }

                if (purchaseOrder != null && goodsReceipt.PurchaseOrderId != purchaseOrder.Id)
                {
                    throw new InvalidOperationException("Goods receipt does not belong to the selected purchase order.");
                }
            }

            if (!model.GoodsReceiptId.HasValue && hasInventoryItems && !model.WarehouseId.HasValue)
            {
                throw new InvalidOperationException("Warehouse is required when invoice lines are linked to products.");
            }

            if (!model.GoodsReceiptId.HasValue && model.WarehouseId.HasValue)
            {
                var warehouseExists = await _context.Warehouses
                    .AnyAsync(w => w.CompanyId == companyId && w.Id == model.WarehouseId.Value);
                if (!warehouseExists)
                {
                    throw new InvalidOperationException("Selected warehouse was not found.");
                }

                if (model.StockLocationId.HasValue)
                {
                    var locationExists = await _context.StockLocations.AnyAsync(l =>
                        l.CompanyId == companyId &&
                        l.WarehouseId == model.WarehouseId.Value &&
                        l.Id == model.StockLocationId.Value);
                    if (!locationExists)
                    {
                        throw new InvalidOperationException("Selected location belongs to another warehouse.");
                    }
                }
            }

            var invoice = new SupplierInvoice
            {
                CompanyId = companyId,
                PurchaseOrderId = purchaseOrder?.Id,
                GoodsReceiptId = goodsReceipt?.Id,
                InvoiceNumber = await GenerateInvoiceNumberAsync(company?.InvoicePrefix ?? "INV", companyId),
                SupplierInvoiceNumber = model.SupplierInvoiceNumber,
                SupplierName = supplierName,
                SupplierTaxNumber = model.SupplierTaxNumber,
                SupplierAddress = model.SupplierAddress,
                SupplierPhone = model.SupplierPhone,
                SupplierEmail = model.SupplierEmail,
                InvoiceDate = model.InvoiceDate,
                DueDate = model.DueDate,
                Notes = model.Notes,
                Status = "Draft",
                MatchStatus = ResolveMatchStatus(purchaseOrder, goodsReceipt),
                CreatedDate = DateTime.Now,
                CreatedBy = createdBy
            };

            foreach (var input in validItems)
            {
                Product? product = null;
                if (input.CreateNewProduct)
                {
                    var productName = !string.IsNullOrWhiteSpace(input.NewProductName)
                        ? input.NewProductName.Trim()
                        : input.Description.Trim();
                    if (string.IsNullOrWhiteSpace(productName))
                    {
                        throw new InvalidOperationException("New product name is required.");
                    }

                    product = new Product
                    {
                        CompanyId = companyId,
                        Name = productName,
                        Description = string.IsNullOrWhiteSpace(input.Description) ? productName : input.Description.Trim(),
                        SKU = string.IsNullOrWhiteSpace(input.NewProductSku) ? null : input.NewProductSku.Trim(),
                        CostPrice = input.UnitCost,
                        Price = input.NewProductSalePrice.GetValueOrDefault(input.UnitCost),
                        TaxRate = input.TaxRate,
                        Stock = 0,
                        MinStock = input.NewProductMinStock,
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    };
                    _context.Products.Add(product);
                }
                else if (input.ProductId.HasValue)
                {
                    product = await _context.Products
                        .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == input.ProductId.Value && p.IsActive);
                    if (product == null)
                    {
                        throw new InvalidOperationException("One of the selected products was not found.");
                    }
                }

                var lineSubTotal = decimal.Round(input.Quantity * input.UnitCost, 2);
                var taxAmount = decimal.Round(lineSubTotal * (input.TaxRate / 100), 2);
                var lineTotal = lineSubTotal + taxAmount;

                invoice.Items.Add(new SupplierInvoiceItem
                {
                    ProductId = product?.Id,
                    Product = input.CreateNewProduct ? product : null,
                    Description = string.IsNullOrWhiteSpace(input.Description) && product != null
                        ? product.Name
                        : input.Description.Trim(),
                    Quantity = input.Quantity,
                    UnitCost = input.UnitCost,
                    TaxRate = input.TaxRate,
                    TaxAmount = taxAmount,
                    LineTotal = lineTotal
                });

                invoice.SubTotal += lineSubTotal;
                invoice.TaxAmount += taxAmount;
                invoice.TotalAmount += lineTotal;
            }

            _context.SupplierInvoices.Add(invoice);
            if (purchaseOrder != null && invoice.TotalAmount == purchaseOrder.TotalAmount)
            {
                purchaseOrder.Status = purchaseOrder.Status == "Received" ? "Invoiced" : purchaseOrder.Status;
            }
            await _context.SaveChangesAsync();

            if (!model.GoodsReceiptId.HasValue && model.WarehouseId.HasValue)
            {
                await ReceiveDirectInvoiceStockAsync(invoice, model.WarehouseId.Value, model.StockLocationId, createdBy);
            }

            return invoice;
        }

        private async Task ReceiveDirectInvoiceStockAsync(SupplierInvoice invoice, int warehouseId, int? stockLocationId, string? createdBy)
        {
            var stockedItems = invoice.Items.Where(i => i.ProductId.HasValue && i.Quantity > 0).ToList();
            if (!stockedItems.Any())
            {
                return;
            }

            foreach (var item in stockedItems)
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.CompanyId == invoice.CompanyId && p.Id == item.ProductId!.Value);
                if (product == null) continue;

                var stock = await _context.WarehouseStocks.FirstOrDefaultAsync(s =>
                    s.CompanyId == invoice.CompanyId &&
                    s.WarehouseId == warehouseId &&
                    s.StockLocationId == stockLocationId &&
                    s.ProductId == product.Id);

                if (stock == null)
                {
                    stock = new WarehouseStock
                    {
                        CompanyId = invoice.CompanyId,
                        WarehouseId = warehouseId,
                        StockLocationId = stockLocationId,
                        ProductId = product.Id,
                        QuantityOnHand = 0,
                        QuantityReserved = 0,
                        UpdatedDate = DateTime.Now
                    };
                    _context.WarehouseStocks.Add(stock);
                }

                var previousStock = product.Stock;
                var quantity = (int)Math.Round(item.Quantity, MidpointRounding.AwayFromZero);
                stock.QuantityOnHand += item.Quantity;
                stock.UpdatedDate = DateTime.Now;
                product.Stock += quantity;
                product.UpdatedDate = DateTime.Now;

                _context.StockMovements.Add(new StockMovement
                {
                    CompanyId = invoice.CompanyId,
                    ProductId = product.Id,
                    MovementType = "SupplierInvoice",
                    Quantity = quantity,
                    PreviousStock = previousStock,
                    NewStock = product.Stock,
                    ReferenceId = invoice.Id,
                    ReferenceType = nameof(SupplierInvoice),
                    Notes = $"Received from supplier invoice {invoice.SupplierInvoiceNumber ?? invoice.InvoiceNumber} ({invoice.SupplierName})",
                    CreatedBy = createdBy,
                    CreatedDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
        }

        private static string ResolveMatchStatus(PurchaseOrder? purchaseOrder, GoodsReceipt? goodsReceipt)
        {
            if (purchaseOrder != null && goodsReceipt != null) return "Matched";
            if (purchaseOrder != null || goodsReceipt != null) return "PartiallyMatched";
            return "Unmatched";
        }

        private async Task<string> GenerateInvoiceNumberAsync(string prefix, int? companyId)
        {
            prefix = string.IsNullOrWhiteSpace(prefix) ? "INV" : prefix.Trim().ToUpperInvariant();
            var datePart = DateTime.Today.ToString("yyyyMMdd");
            var startsWith = $"{prefix}-{datePart}-";

            var count = await _context.SupplierInvoices
                .CountAsync(i => i.CompanyId == companyId && i.InvoiceNumber.StartsWith(startsWith));

            return $"{startsWith}{count + 1:000}";
        }
    }
}
