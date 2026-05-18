using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{
    [Authorize]
    public class SalesController : Controller
    {
        private readonly ISaleService _saleService;
        private readonly ApplicationDbContext _context;
        private readonly ICurrentCompanyService _currentCompany;
        private readonly IAuditLogService _auditLog;

        public SalesController(ISaleService saleService, ApplicationDbContext context, ICurrentCompanyService currentCompany, IAuditLogService auditLog)
        {
            _saleService = saleService;
            _context = context;
            _currentCompany = currentCompany;
            _auditLog = auditLog;
        }

        // GET: Sales
        public async Task<IActionResult> Index(string? searchTerm = null, int page = 1, int pageSize = 20)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 10, 100);
            var filter = new SalesFilterDto
            {
                SearchTerm = searchTerm,
                Page = page,
                PageSize = pageSize
            };

            if (!User.IsInRole("Admin"))
            {
                filter.CashierName = User.Identity?.Name ?? "";
            }

            var result = await _saleService.SearchSalesAsync(filter);
            ViewBag.SearchTerm = searchTerm;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = result.TotalCount;
            ViewBag.TotalPages = (int)Math.Ceiling(result.TotalCount / (double)pageSize);
            var sales = result.Sales;
            return View(sales);
        }

        // GET: Sales/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var sale = await _saleService.GetSaleByIdAsync(id);
            if (sale == null)
            {
                return NotFound();
            }
            return View(sale);
        }

        // GET: Sales/Receipt/5
        public async Task<IActionResult> Receipt(int id)
        {
            var sale = await _saleService.GetSaleByIdAsync(id);
            if (sale == null)
            {
                return NotFound();
            }
            return View(sale);
        }

        // GET: Sales/Return/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Return(int id)
        {
            var sale = await _saleService.GetSaleByIdAsync(id);
            if (sale == null)
            {
                return NotFound();
            }

            return View(BuildReturnViewModel(sale));
        }

        // POST: Sales/Return/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Return(int id, SalesReturnViewModel model)
        {
            if (id != model.SaleId)
            {
                return NotFound();
            }

            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
            var sale = await _context.Sales
                .Include(s => s.SaleItems)
                .ThenInclude(i => i.Product)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == id && s.CompanyId == companyId);

            if (sale == null)
            {
                return NotFound();
            }

            if (sale.Status == "Returned")
            {
                ModelState.AddModelError(string.Empty, "This sale is already fully returned.");
            }

            var requestedItems = model.Items.Where(i => i.ReturnQuantity > 0).ToList();
            if (requestedItems.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Select at least one product quantity to return.");
            }

            foreach (var requested in requestedItems)
            {
                var saleItem = sale.SaleItems.FirstOrDefault(i => i.Id == requested.SaleItemId);
                if (saleItem == null || requested.ReturnQuantity > saleItem.ReturnableQuantity)
                {
                    ModelState.AddModelError(string.Empty, $"Invalid return quantity for {requested.ProductName}.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(BuildReturnViewModel(sale, model));
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            var now = DateTime.Now;
            var returnedBy = User.Identity?.Name ?? "Unknown";
            decimal refundTotal = 0;

            foreach (var requested in requestedItems)
            {
                var saleItem = sale.SaleItems.First(i => i.Id == requested.SaleItemId);
                var product = saleItem.Product;
                var previousStock = product.Stock;
                var lineRefund = Math.Round(saleItem.UnitPrice * requested.ReturnQuantity, 2);

                saleItem.ReturnedQuantity += requested.ReturnQuantity;
                saleItem.RefundedAmount += lineRefund;
                product.Stock += requested.ReturnQuantity;
                product.UpdatedDate = now;
                refundTotal += lineRefund;

                _context.StockMovements.Add(new StockMovement
                {
                    CompanyId = companyId,
                    ProductId = product.Id,
                    MovementType = "Return",
                    Quantity = requested.ReturnQuantity,
                    PreviousStock = previousStock,
                    NewStock = product.Stock,
                    ReferenceId = sale.Id,
                    ReferenceType = "Sale",
                    Notes = $"Returned from sale #{sale.SaleNumber}. {model.Reason}",
                    CreatedBy = returnedBy,
                    CreatedDate = now
                });
            }

            sale.RefundedAmount += refundTotal;
            sale.ReturnedDate = now;
            sale.ReturnedBy = returnedBy;
            sale.ReturnReason = model.Reason;
            sale.Status = sale.SaleItems.All(i => i.ReturnableQuantity == 0)
                ? "Returned"
                : "Partially Returned";

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            await _auditLog.LogAsync("Return", nameof(Sale), sale.Id.ToString(), $"Returned {refundTotal:N2} from sale {sale.SaleNumber}", companyId);

            TempData["Success"] = $"Return processed. Refunded amount: {refundTotal:C}.";
            return RedirectToAction(nameof(Details), new { id = sale.Id });
        }

        // GET: Sales/ExportDailyReport
        public async Task<IActionResult> ExportDailyReport()
        {
            List<Sale> sales;
            if (User.IsInRole("Admin"))
            {
                sales = await _saleService.GetAllSalesAsync();
            }
            else
            {
                sales = await _saleService.GetSalesByCashierAsync(User.Identity?.Name ?? "");
            }

            var todaySales = sales.Where(s => s.SaleDate.Date == DateTime.Today).ToList();

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Today's Sales");

                // Headers
                worksheet.Cell(1, 1).Value = "Sale Number";
                worksheet.Cell(1, 2).Value = "Date";
                worksheet.Cell(1, 3).Value = "Payment Method";
                worksheet.Cell(1, 4).Value = "Total Amount";
                worksheet.Cell(1, 5).Value = "Status";

                // Data
                int row = 2;
                foreach (var sale in todaySales)
                {
                    worksheet.Cell(row, 1).Value = sale.SaleNumber;
                    worksheet.Cell(row, 2).Value = sale.SaleDate.ToString("yyyy-MM-dd HH:mm:ss");
                    worksheet.Cell(row, 3).Value = sale.PaymentMethod ?? "Cash";
                    worksheet.Cell(row, 4).Value = sale.TotalAmount;
                    worksheet.Cell(row, 5).Value = sale.Status;
                    row++;
                }

                // Styling
                var headerRange = worksheet.Range("A1:E1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightBlue;
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"MiniPOS_Report_{DateTime.Today:yyyyMMdd}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

        private static SalesReturnViewModel BuildReturnViewModel(Sale sale, SalesReturnViewModel? posted = null)
        {
            return new SalesReturnViewModel
            {
                SaleId = sale.Id,
                SaleNumber = sale.SaleNumber,
                SaleDate = sale.SaleDate,
                PaymentMethod = sale.PaymentMethod,
                Status = sale.Status,
                TotalAmount = sale.TotalAmount,
                RefundedAmount = sale.RefundedAmount,
                Reason = posted?.Reason,
                Items = sale.SaleItems.Select(item =>
                {
                    var postedItem = posted?.Items.FirstOrDefault(i => i.SaleItemId == item.Id);
                    return new SalesReturnItemViewModel
                    {
                        SaleItemId = item.Id,
                        ProductName = !string.IsNullOrWhiteSpace(item.ProductNameSnapshot)
                            ? item.ProductNameSnapshot
                            : item.Product?.Name ?? "Product",
                        Quantity = item.Quantity,
                        ReturnedQuantity = item.ReturnedQuantity,
                        ReturnableQuantity = item.ReturnableQuantity,
                        UnitPrice = item.UnitPrice,
                        ReturnQuantity = postedItem?.ReturnQuantity ?? 0
                    };
                }).ToList()
            };
        }
    }
}
