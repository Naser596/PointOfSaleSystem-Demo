using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers;

[Authorize(Roles = "Admin,Warehouse")]
public class WarehousesController(
    ApplicationDbContext context,
    ICurrentCompanyService currentCompany,
    IAuditLogService auditLog,
    IWarehouseWorkflowService warehouseWorkflow) : Controller
{
    private readonly ApplicationDbContext _context = context;
    private readonly ICurrentCompanyService _currentCompany = currentCompany;
    private readonly IAuditLogService _auditLog = auditLog;
    private readonly IWarehouseWorkflowService _warehouseWorkflow = warehouseWorkflow;

    public async Task<IActionResult> Index()
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var model = new WarehouseDashboardViewModel
        {
            Warehouses = await _context.Warehouses
                .Where(w => w.CompanyId == companyId)
                .OrderBy(w => w.Name)
                .ToListAsync(),
            Locations = await _context.StockLocations
                .Include(l => l.Warehouse)
                .Where(l => l.CompanyId == companyId)
                .OrderBy(l => l.Warehouse.Name)
                .ThenBy(l => l.Name)
                .ToListAsync(),
            Stocks = await _context.WarehouseStocks
                .Include(s => s.Warehouse)
                .Include(s => s.StockLocation)
                .Include(s => s.Product)
                .Where(s => s.CompanyId == companyId)
                .OrderBy(s => s.Warehouse.Name)
                .ThenBy(s => s.Product.Name)
                .ToListAsync(),
            Products = await _context.Products
                .Where(p => p.CompanyId == companyId && p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync(),
            PendingTransfers = await _context.StockTransfers
                .Include(t => t.Lines)
                    .ThenInclude(l => l.Product)
                .Where(t => t.CompanyId == companyId && t.Status == "PendingApproval")
                .OrderByDescending(t => t.CreatedDate)
                .Take(20)
                .ToListAsync(),
            TraceLots = await _context.ProductTraceLots
                .Include(l => l.Product)
                .Include(l => l.Warehouse)
                .Where(l => l.CompanyId == companyId)
                .OrderByDescending(l => l.CreatedDate)
                .Take(20)
                .ToListAsync()
        };

        return View(model);
    }

    public async Task<IActionResult> StockLedger(int? productId, int? warehouseId)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var products = await _context.Products
            .Where(p => p.CompanyId == companyId && p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
        var warehouses = await _context.Warehouses
            .Where(w => w.CompanyId == companyId && w.IsActive)
            .OrderBy(w => w.Name)
            .ToListAsync();

        if (productId.HasValue && products.All(p => p.Id != productId.Value))
        {
            return NotFound();
        }

        if (warehouseId.HasValue && warehouses.All(w => w.Id != warehouseId.Value))
        {
            return NotFound();
        }

        var stockQuery = _context.WarehouseStocks
            .Include(s => s.Warehouse)
            .Include(s => s.StockLocation)
            .Include(s => s.Product)
            .Where(s => s.CompanyId == companyId);
        if (productId.HasValue)
        {
            stockQuery = stockQuery.Where(s => s.ProductId == productId.Value);
        }
        if (warehouseId.HasValue)
        {
            stockQuery = stockQuery.Where(s => s.WarehouseId == warehouseId.Value);
        }

        var movementQuery = _context.StockMovements
            .Include(m => m.Product)
            .Where(m => m.CompanyId == companyId);
        if (productId.HasValue)
        {
            movementQuery = movementQuery.Where(m => m.ProductId == productId.Value);
        }

        var reorderSuggestions = products
            .Where(p => p.Stock <= p.MinStock)
            .Select(p =>
            {
                var targetStock = Math.Max(p.MinStock * 2, p.MinStock + 1);
                var suggestedQuantity = Math.Max(targetStock - p.Stock, 1);
                return new ReorderSuggestionRow
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    Sku = p.SKU,
                    CurrentStock = p.Stock,
                    MinStock = p.MinStock,
                    SuggestedQuantity = suggestedQuantity,
                    EstimatedCost = suggestedQuantity * p.CostPrice
                };
            })
            .OrderByDescending(r => r.MinStock - r.CurrentStock)
            .ThenBy(r => r.ProductName)
            .ToList();

        var model = new InventoryControlViewModel
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            Products = products,
            Warehouses = warehouses,
            Stocks = await stockQuery
                .OrderBy(s => s.Product.Name)
                .ThenBy(s => s.Warehouse.Name)
                .ThenBy(s => s.StockLocation == null ? string.Empty : s.StockLocation.Name)
                .ToListAsync(),
            StockMovements = await movementQuery
                .OrderByDescending(m => m.CreatedDate)
                .Take(200)
                .ToListAsync(),
            ReorderSuggestions = reorderSuggestions,
            TraceLots = await _context.ProductTraceLots
                .Include(l => l.Product)
                .Include(l => l.Warehouse)
                .Where(l => l.CompanyId == companyId &&
                    (!productId.HasValue || l.ProductId == productId.Value) &&
                    (!warehouseId.HasValue || l.WarehouseId == warehouseId.Value))
                .OrderBy(l => l.Product.Name)
                .ThenBy(l => l.TraceNumber)
                .ToListAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateWarehouse(WarehouseInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            TempData["Error"] = "Warehouse name is required.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var code = string.IsNullOrWhiteSpace(input.Code) ? null : input.Code.Trim().ToUpperInvariant();
        if (code != null)
        {
            var exists = await _context.Warehouses.AnyAsync(w => w.CompanyId == companyId && w.Code == code);
            if (exists)
            {
                TempData["Error"] = "A warehouse with this code already exists.";
                return RedirectToAction(nameof(Index));
            }
        }

        var warehouse = new Warehouse
        {
            CompanyId = companyId,
            Name = input.Name.Trim(),
            Code = code,
            Address = string.IsNullOrWhiteSpace(input.Address) ? null : input.Address.Trim(),
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync("Create", nameof(Warehouse), warehouse.Id.ToString(), $"Created warehouse {warehouse.Name}", companyId);

        TempData["Success"] = "Warehouse created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLocation(StockLocationInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name) || input.WarehouseId <= 0)
        {
            TempData["Error"] = "Warehouse and location name are required.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var warehouseExists = await _context.Warehouses
            .AnyAsync(w => w.CompanyId == companyId && w.Id == input.WarehouseId);
        if (!warehouseExists) return NotFound();

        var code = string.IsNullOrWhiteSpace(input.Code) ? null : input.Code.Trim().ToUpperInvariant();
        if (code != null)
        {
            var exists = await _context.StockLocations.AnyAsync(l =>
                l.CompanyId == companyId &&
                l.WarehouseId == input.WarehouseId &&
                l.Code == code);
            if (exists)
            {
                TempData["Error"] = "A location with this code already exists in this warehouse.";
                return RedirectToAction(nameof(Index));
            }
        }

        var location = new StockLocation
        {
            CompanyId = companyId,
            WarehouseId = input.WarehouseId,
            Name = input.Name.Trim(),
            Code = code,
            IsActive = true
        };

        _context.StockLocations.Add(location);
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync("Create", nameof(StockLocation), location.Id.ToString(), $"Created stock location {location.Name}", companyId);

        TempData["Success"] = "Stock location created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TransferStock(StockTransferInput input)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        try
        {
            var approvalRule = await _context.ApprovalRules
                .Where(r => r.CompanyId == companyId &&
                    r.IsActive &&
                    r.EntityName == nameof(StockTransfer) &&
                    r.ActionName == "Transfer" &&
                    input.Quantity >= r.AmountThreshold)
                .OrderByDescending(r => r.AmountThreshold)
                .FirstOrDefaultAsync();
            var transfer = approvalRule == null
                ? await _warehouseWorkflow.TransferStockAsync(input, User.Identity?.Name)
                : await _warehouseWorkflow.RequestTransferApprovalAsync(input, User.Identity?.Name);

            await _auditLog.LogAsync("Transfer", nameof(StockTransfer), transfer.Id.ToString(), $"Stock transfer {transfer.TransferNumber} status {transfer.Status}", companyId);
            TempData["Success"] = transfer.Status == "PendingApproval"
                ? "Stock transfer sent for approval."
                : "Stock transfer posted.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostApprovedTransfer(int id)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        try
        {
            var transfer = await _warehouseWorkflow.PostApprovedTransferAsync(id, User.Identity?.Name);
            await _auditLog.LogAsync("PostApprovedTransfer", nameof(StockTransfer), transfer.Id.ToString(), $"Posted approved transfer {transfer.TransferNumber}", companyId);
            TempData["Success"] = "Approved transfer posted.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustStock(StockAdjustmentInput input)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        try
        {
            var stock = await _warehouseWorkflow.AdjustStockAsync(input, User.Identity?.Name);
            await _auditLog.LogAsync("Adjust", nameof(WarehouseStock), stock.Id.ToString(), $"Adjusted warehouse stock for product {input.ProductId}", companyId);
            TempData["Success"] = "Stock adjustment posted.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignExistingStock(StockAssignmentInput input)
    {
        if (input.ProductId <= 0 || input.WarehouseId <= 0 || input.Quantity <= 0)
        {
            TempData["Error"] = "Product, warehouse, and positive quantity are required.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var product = await _context.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == input.ProductId);
        if (product == null) return NotFound();

        var warehouseExists = await _context.Warehouses
            .AnyAsync(w => w.CompanyId == companyId && w.Id == input.WarehouseId);
        if (!warehouseExists) return NotFound();

        if (input.StockLocationId.HasValue)
        {
            var locationExists = await _context.StockLocations.AnyAsync(l =>
                l.CompanyId == companyId &&
                l.WarehouseId == input.WarehouseId &&
                l.Id == input.StockLocationId.Value);
            if (!locationExists)
            {
                TempData["Error"] = "Stock location belongs to another warehouse.";
                return RedirectToAction(nameof(Index));
            }
        }

        var assigned = await _context.WarehouseStocks
            .Where(s => s.CompanyId == companyId && s.ProductId == input.ProductId)
            .SumAsync(s => s.QuantityOnHand);
        var unassigned = Math.Max(product.Stock - assigned, 0);
        if (input.Quantity > unassigned)
        {
            TempData["Error"] = $"Only {unassigned:N2} unassigned units are available for {product.Name}.";
            return RedirectToAction(nameof(Index));
        }

        var stock = await _context.WarehouseStocks.FirstOrDefaultAsync(s =>
            s.CompanyId == companyId &&
            s.ProductId == input.ProductId &&
            s.WarehouseId == input.WarehouseId &&
            s.StockLocationId == input.StockLocationId);

        if (stock == null)
        {
            stock = new WarehouseStock
            {
                CompanyId = companyId,
                ProductId = input.ProductId,
                WarehouseId = input.WarehouseId,
                StockLocationId = input.StockLocationId,
                QuantityOnHand = 0,
                QuantityReserved = 0,
                UpdatedDate = DateTime.Now
            };
            _context.WarehouseStocks.Add(stock);
        }

        stock.QuantityOnHand += input.Quantity;
        stock.UpdatedDate = DateTime.Now;

        _context.StockMovements.Add(new StockMovement
        {
            CompanyId = companyId,
            ProductId = input.ProductId,
            MovementType = "Assignment",
            Quantity = (int)Math.Round(input.Quantity, MidpointRounding.AwayFromZero),
            PreviousStock = product.Stock,
            NewStock = product.Stock,
            ReferenceType = "WarehouseAssignment",
            Notes = string.IsNullOrWhiteSpace(input.Reason)
                ? "Assigned existing product stock to warehouse"
                : input.Reason.Trim(),
            CreatedBy = User.Identity?.Name,
            CreatedDate = DateTime.Now
        });

        await _context.SaveChangesAsync();
        await _auditLog.LogAsync("AssignExistingStock", nameof(WarehouseStock), stock.Id.ToString(), $"Assigned existing stock for product {product.Name}", companyId);
        TempData["Success"] = "Existing stock assigned to warehouse.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTraceLot(ProductTraceLotInput input)
    {
        if (input.ProductId <= 0 || string.IsNullOrWhiteSpace(input.TraceNumber))
        {
            TempData["Error"] = "Product and batch/serial number are required.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var productExists = await _context.Products
            .IgnoreQueryFilters()
            .AnyAsync(p => p.CompanyId == companyId && p.Id == input.ProductId);
        if (!productExists) return NotFound();

        if (input.WarehouseId.HasValue)
        {
            var warehouseExists = await _context.Warehouses.AnyAsync(w => w.CompanyId == companyId && w.Id == input.WarehouseId.Value);
            if (!warehouseExists) return NotFound();
        }

        var traceNumber = input.TraceNumber.Trim().ToUpperInvariant();
        var exists = await _context.ProductTraceLots.AnyAsync(l =>
            l.CompanyId == companyId &&
            l.ProductId == input.ProductId &&
            l.TraceNumber == traceNumber);
        if (exists)
        {
            TempData["Error"] = "This batch/serial number already exists for the selected product.";
            return RedirectToAction(nameof(Index));
        }

        var traceLot = new ProductTraceLot
        {
            CompanyId = companyId,
            ProductId = input.ProductId,
            WarehouseId = input.WarehouseId,
            TraceType = string.IsNullOrWhiteSpace(input.TraceType) ? "Batch" : input.TraceType.Trim(),
            TraceNumber = traceNumber,
            Quantity = input.Quantity,
            ExpiryDate = input.ExpiryDate?.Date,
            Status = "Active",
            Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
            CreatedBy = User.Identity?.Name,
            CreatedDate = DateTime.Now
        };
        _context.ProductTraceLots.Add(traceLot);
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync("Create", nameof(ProductTraceLot), traceLot.Id.ToString(), $"Created trace lot {traceLot.TraceNumber}", companyId);

        TempData["Success"] = "Batch/serial record created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> StockCounts()
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        ViewBag.Warehouses = await _context.Warehouses
            .Where(w => w.CompanyId == companyId && w.IsActive)
            .OrderBy(w => w.Name)
            .ToListAsync();
        var counts = await _context.StockCounts
            .Include(c => c.Warehouse)
            .Include(c => c.Lines)
            .Where(c => c.CompanyId == companyId)
            .OrderByDescending(c => c.CountDate)
            .ThenByDescending(c => c.Id)
            .Take(100)
            .ToListAsync();

        return View(counts);
    }

    public async Task<IActionResult> StockCountDetails(int id)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var count = await _context.StockCounts
            .Include(c => c.Warehouse)
            .Include(c => c.Lines)
                .ThenInclude(l => l.Product)
            .Include(c => c.Lines)
                .ThenInclude(l => l.StockLocation)
            .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);
        if (count == null) return NotFound();

        return View(count);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStockCount(StockCountCreateInput input)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        try
        {
            var count = await _warehouseWorkflow.CreateStockCountAsync(input, User.Identity?.Name);
            await _auditLog.LogAsync("Create", nameof(StockCount), count.Id.ToString(), $"Created stock count {count.CountNumber}", companyId);
            TempData["Success"] = "Stock count created.";
            return RedirectToAction(nameof(StockCountDetails), new { id = count.Id });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(StockCounts));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStockCountLine(StockCountLineUpdateInput input)
    {
        try
        {
            await _warehouseWorkflow.UpdateStockCountLineAsync(input);
            TempData["Success"] = "Count line updated.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(StockCountDetails), new { id = input.StockCountId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostStockCount(int id)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        try
        {
            var count = await _warehouseWorkflow.PostStockCountAsync(id, User.Identity?.Name);
            await _auditLog.LogAsync("Post", nameof(StockCount), count.Id.ToString(), $"Posted stock count {count.CountNumber}", companyId);
            TempData["Success"] = "Stock count posted and variances adjusted.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(StockCountDetails), new { id });
    }
}
