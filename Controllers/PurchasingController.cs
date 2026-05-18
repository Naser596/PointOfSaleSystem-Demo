using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers;

[Authorize(Roles = "Admin,Manager,Warehouse")]
public class PurchasingController(
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
        var model = new PurchasingDashboardViewModel
        {
            PurchaseOrders = await _context.PurchaseOrders
                .Include(p => p.Lines)
                .Where(p => p.CompanyId == companyId)
                .OrderByDescending(p => p.OrderDate)
                .ThenByDescending(p => p.Id)
                .Take(100)
                .ToListAsync(),
            LowStockProducts = await _context.Products
                .Where(p => p.CompanyId == companyId && p.IsActive && p.Stock <= p.MinStock)
                .OrderBy(p => p.Name)
                .ToListAsync()
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var order = await FindOrderAsync(id, companyId);
        if (order == null) return NotFound();

        var model = new PurchaseOrderDetailsViewModel
        {
            Order = order,
            Products = await _context.Products
                .Where(p => p.CompanyId == companyId && p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync(),
            Warehouses = await _context.Warehouses
                .Where(w => w.CompanyId == companyId && w.IsActive)
                .OrderBy(w => w.Name)
                .ToListAsync(),
            Locations = await _context.StockLocations
                .Include(l => l.Warehouse)
                .Where(l => l.CompanyId == companyId && l.IsActive)
                .OrderBy(l => l.Warehouse.Name)
                .ThenBy(l => l.Name)
                .ToListAsync(),
            Receipts = await _context.GoodsReceipts
                .Include(r => r.Lines)
                    .ThenInclude(l => l.Product)
                .Where(r => r.CompanyId == companyId && r.PurchaseOrderId == order.Id)
                .OrderByDescending(r => r.ReceiptDate)
                .ThenByDescending(r => r.Id)
                .ToListAsync()
        };

        return View(model);
    }

    public async Task<IActionResult> Print(int id)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var order = await FindOrderAsync(id, companyId);
        if (order == null) return NotFound();

        ViewBag.Company = await _context.Companies.FindAsync(companyId);
        return View(new PurchaseOrderDetailsViewModel { Order = order });
    }

    public async Task<IActionResult> DownloadPdf(int id)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var order = await FindOrderAsync(id, companyId);
        if (order == null) return NotFound();

        var company = await _context.Companies.FindAsync(companyId);
        var pdf = SimplePdfService.BuildPurchaseOrder(order, company);
        await _auditLog.LogAsync("ExportPdf", nameof(PurchaseOrder), order.Id.ToString(), $"Downloaded PDF for {order.OrderNumber}", companyId);
        return File(pdf, "application/pdf", $"{order.OrderNumber}.pdf");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseOrderInput input)
    {
        if (string.IsNullOrWhiteSpace(input.SupplierName))
        {
            TempData["Error"] = "Supplier name is required.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var orderNumber = string.IsNullOrWhiteSpace(input.OrderNumber)
            ? $"PO-{DateTime.UtcNow:yyyyMMddHHmmss}"
            : input.OrderNumber.Trim().ToUpperInvariant();

        var exists = await _context.PurchaseOrders
            .AnyAsync(p => p.CompanyId == companyId && p.OrderNumber == orderNumber);
        if (exists)
        {
            TempData["Error"] = "A purchase order with this number already exists.";
            return RedirectToAction(nameof(Index));
        }

        var order = new PurchaseOrder
        {
            CompanyId = companyId,
            OrderNumber = orderNumber,
            SupplierName = input.SupplierName.Trim(),
            SupplierTaxNumber = string.IsNullOrWhiteSpace(input.SupplierTaxNumber) ? null : input.SupplierTaxNumber.Trim(),
            OrderDate = input.OrderDate.Date,
            ExpectedDate = input.ExpectedDate?.Date,
            Status = string.IsNullOrWhiteSpace(input.Status) ? "Draft" : input.Status.Trim(),
            SubTotal = input.SubTotal,
            TaxAmount = input.TaxAmount,
            TotalAmount = input.SubTotal + input.TaxAmount,
            Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
            CreatedBy = User.Identity?.Name,
            CreatedDate = DateTime.Now
        };

        _context.PurchaseOrders.Add(order);
        var approvalRule = await _context.ApprovalRules
            .Where(r => r.CompanyId == companyId &&
                r.IsActive &&
                r.EntityName == nameof(PurchaseOrder) &&
                r.ActionName == "Create" &&
                order.TotalAmount >= r.AmountThreshold)
            .OrderByDescending(r => r.AmountThreshold)
            .FirstOrDefaultAsync();
        if (approvalRule != null)
        {
            order.Status = "PendingApproval";
            _context.ApprovalRequests.Add(new ApprovalRequest
            {
                CompanyId = companyId,
                RequestType = approvalRule.RuleName,
                EntityName = nameof(PurchaseOrder),
                Status = "Pending",
                Notes = $"Purchase order {order.OrderNumber} requires approval because total {order.TotalAmount:N2} reached threshold {approvalRule.AmountThreshold:N2}.",
                RequestedBy = User.Identity?.Name,
                RequestedDate = DateTime.Now
            });
        }
        await _context.SaveChangesAsync();
        var pendingApproval = await _context.ApprovalRequests
            .Where(r => r.CompanyId == companyId && r.EntityName == nameof(PurchaseOrder) && r.EntityId == null)
            .OrderByDescending(r => r.Id)
            .FirstOrDefaultAsync();
        if (pendingApproval != null && order.Status == "PendingApproval")
        {
            pendingApproval.EntityId = order.Id.ToString();
            await _context.SaveChangesAsync();
        }
        await _auditLog.LogAsync("Create", nameof(PurchaseOrder), order.Id.ToString(), $"Created purchase order {order.OrderNumber}", companyId);

        TempData["Success"] = order.Status == "PendingApproval"
            ? "Purchase order created and sent for approval."
            : "Purchase order created.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddLine(PurchaseOrderLineInput input)
    {
        if (input.PurchaseOrderId <= 0 || input.Quantity <= 0)
        {
            TempData["Error"] = "Purchase order and positive quantity are required.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var order = await FindOrderAsync(input.PurchaseOrderId, companyId);
        if (order == null) return NotFound();

        Product? product = null;
        if (input.ProductId.HasValue)
        {
            product = await _context.Products
                .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == input.ProductId.Value);
            if (product == null) return NotFound();
        }

        var description = !string.IsNullOrWhiteSpace(input.Description)
            ? input.Description.Trim()
            : product?.Name;
        if (string.IsNullOrWhiteSpace(description))
        {
            TempData["Error"] = "Line description is required.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        var unitCost = input.UnitCost > 0 ? input.UnitCost : product?.CostPrice ?? 0;
        var taxRate = input.TaxRate > 0 ? input.TaxRate : product?.TaxRate ?? 0;
        var taxableAmount = input.Quantity * unitCost;
        var taxAmount = Math.Round(taxableAmount * taxRate / 100, 2);

        order.Lines.Add(new PurchaseOrderLine
        {
            ProductId = product?.Id,
            Description = description,
            Quantity = input.Quantity,
            ReceivedQuantity = 0,
            UnitCost = unitCost,
            TaxRate = taxRate,
            TaxAmount = taxAmount,
            LineTotal = taxableAmount + taxAmount
        });

        RecalculateTotals(order);
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync("AddLine", nameof(PurchaseOrder), order.Id.ToString(), $"Added line to purchase order {order.OrderNumber}", companyId);

        TempData["Success"] = "Purchase order line added.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Receive(GoodsReceiptInput input)
    {
        if (input.PurchaseOrderId <= 0 || input.WarehouseId <= 0)
        {
            TempData["Error"] = "Purchase order and warehouse are required.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var order = await FindOrderAsync(input.PurchaseOrderId, companyId);
        if (order == null) return NotFound();

        var warehouse = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.CompanyId == companyId && w.Id == input.WarehouseId);
        if (warehouse == null) return NotFound();

        if (input.StockLocationId.HasValue)
        {
            var locationExists = await _context.StockLocations.AnyAsync(l =>
                l.CompanyId == companyId &&
                l.WarehouseId == input.WarehouseId &&
                l.Id == input.StockLocationId.Value);
            if (!locationExists) return NotFound();
        }

        try
        {
            var receipt = await _warehouseWorkflow.ReceivePurchaseOrderAsync(input, User.Identity?.Name);
            await _auditLog.LogAsync("Receive", nameof(GoodsReceipt), receipt.Id.ToString(), $"Posted goods receipt {receipt.ReceiptNumber} for {order.OrderNumber}", companyId);
            TempData["Success"] = "Goods receipt posted, warehouse stock updated, and accounting entry created.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFromReorder(ReorderToPurchaseOrderInput input)
    {
        if (input.ProductId <= 0 || input.SuggestedQuantity <= 0 || string.IsNullOrWhiteSpace(input.SupplierName))
        {
            TempData["Error"] = "Product, quantity, and supplier are required.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == input.ProductId && p.IsActive);
        if (product == null) return NotFound();

        var order = new PurchaseOrder
        {
            CompanyId = companyId,
            OrderNumber = $"PO-{DateTime.UtcNow:yyyyMMddHHmmss}",
            SupplierName = input.SupplierName.Trim(),
            SupplierTaxNumber = string.IsNullOrWhiteSpace(input.SupplierTaxNumber) ? null : input.SupplierTaxNumber.Trim(),
            OrderDate = DateTime.Today,
            ExpectedDate = DateTime.Today.AddDays(7),
            Status = "Draft",
            Notes = $"Created from reorder suggestion for {product.Name}.",
            CreatedBy = User.Identity?.Name,
            CreatedDate = DateTime.Now,
            Lines =
            [
                new PurchaseOrderLine
                {
                    ProductId = product.Id,
                    Description = product.Name,
                    Quantity = input.SuggestedQuantity,
                    UnitCost = product.CostPrice,
                    TaxRate = product.TaxRate,
                    TaxAmount = Math.Round(input.SuggestedQuantity * product.CostPrice * product.TaxRate / 100, 2),
                    LineTotal = Math.Round(input.SuggestedQuantity * product.CostPrice * (1 + product.TaxRate / 100), 2)
                }
            ]
        };
        RecalculateTotals(order);
        _context.PurchaseOrders.Add(order);
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync("CreateFromReorder", nameof(PurchaseOrder), order.Id.ToString(), $"Created PO {order.OrderNumber} from reorder suggestion", companyId);

        TempData["Success"] = "Purchase order created from reorder suggestion.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    private async Task<PurchaseOrder?> FindOrderAsync(int id, int companyId)
    {
        return await _context.PurchaseOrders
            .Include(p => p.Lines)
                .ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);
    }

    private static void RecalculateTotals(PurchaseOrder order)
    {
        order.SubTotal = order.Lines.Sum(l => l.Quantity * l.UnitCost);
        order.TaxAmount = order.Lines.Sum(l => l.TaxAmount);
        order.TotalAmount = order.SubTotal + order.TaxAmount;
    }
}
