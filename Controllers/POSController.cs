using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{
    [Authorize]
    public class POSController(
        IProductService productService,
        ICurrentCompanyService currentCompany,
        ApplicationDbContext context,
        IPOSOperationsService posOperations) : Controller
    {
        private readonly IProductService _productService = productService;
        private readonly ICurrentCompanyService _currentCompany = currentCompany;
        private readonly ApplicationDbContext _context = context;
        private readonly IPOSOperationsService _posOperations = posOperations;

        // GET: POS
        public IActionResult Index()
        {
            // Initial load - return empty list to show placeholder
            return View(new List<Product>());
        }

        // POST: POS/ProcessSale (CASH ONLY / DIRECT)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessSale([FromBody] SaleRequest request)
        {
            if (request?.Items == null || request.Items.Count == 0)
            {
                return Json(new { success = false, message = "No items in cart" });
            }

            try
            {
                var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
                var sale = await _posOperations.CreateSaleAsync(companyId, new PosSaleInput
                {
                    Items = request.Items.Select(i => new PosSaleItemInput
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity
                    }).ToList(),
                    PaymentMethod = "Cash",
                    CustomerId = request.CustomerId,
                    DiscountCode = request.DiscountCode
                }, User.Identity?.Name ?? "Unknown");
                return Json(new { success = true, message = "Sale completed successfully!", saleNumber = sale.SaleNumber, saleId = sale.Id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncPendingSales([FromBody] List<OfflineSaleRequest> requests)
        {
            if (requests == null || requests.Count == 0)
            {
                return Json(new { success = true, results = Array.Empty<object>() });
            }

            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
            var syncInputs = requests.Select(request => new OfflineSaleSyncInput
            {
                ClientId = request.ClientId,
                QueuedAt = request.QueuedAt,
                LastError = request.LastError,
                PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "Cash" : request.PaymentMethod,
                CustomerId = request.CustomerId,
                DiscountCode = request.DiscountCode,
                Items = request.Items?.Select(i => new PosSaleItemInput
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList() ?? []
            }).ToList();
            var results = await _posOperations.SyncOfflineSalesAsync(companyId, syncInputs, User.Identity?.Name ?? "Unknown");
            return Json(new { success = true, results });
        }

        // POST: POS/CardTerminal (Step 1: Show Terminal)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CardTerminal(string cartJson, string? discountCode = null)
        {
            if (string.IsNullOrEmpty(cartJson))
            {
                return RedirectToAction("Index");
            }

            var items = System.Text.Json.JsonSerializer.Deserialize<List<SaleItemRequest>>(cartJson);
            if (items == null || items.Count == 0)
            {
                return RedirectToAction("Index");
            }

            // Calculate total for display
            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
            decimal total = 0;
            foreach (var item in items)
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId && p.CompanyId == companyId);
                if (product != null)
                {
                    total += product.Price * item.Quantity;
                }
            }

            var model = new CardTerminalViewModel
            {
                CartJson = cartJson,
                TotalAmount = total,
                ItemCount = items.Sum(i => i.Quantity),
                DiscountCode = discountCode,
            };

            return View(model);
        }

        // POST: POS/ProcessCardSale (Step 2: Finalize Card Sale)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCardSale(CardTerminalViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("CardTerminal", model);
            }

            try
            {
                var items = System.Text.Json.JsonSerializer.Deserialize<List<SaleItemRequest>>(model.CartJson);
                if (items == null || items.Count == 0)
                {
                    ModelState.AddModelError("", "Cart is empty.");
                    return View("CardTerminal", model);
                }

                var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
                var sale = await _posOperations.CreateSaleAsync(companyId, new PosSaleInput
                {
                    Items = items.Select(i => new PosSaleItemInput
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity
                    }).ToList(),
                    PaymentMethod = "Card",
                    DiscountCode = model.DiscountCode
                }, User.Identity?.Name ?? "Unknown");

                // Redirect to receipt with clearCart flag
                return RedirectToAction("Receipt", "Sales", new { id = sale.Id, clearCart = 1 });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Transaction Failed: {ex.Message}");
                return View("CardTerminal", model);
            }
        }

        public async Task<IActionResult> Filter(string? searchTerm, int? categoryId)
        {
            var products = await _productService.GetProductsAsync(searchTerm, categoryId);
            return PartialView("_PosProductGrid", products);
        }

        // GET: POS/GetProductByBarcode
        public async Task<IActionResult> GetProductByBarcode(string barcode)
        {
            if (string.IsNullOrEmpty(barcode))
            {
                return Json(new { success = false, message = "Barcode is empty" });
            }

            var product = await _productService.GetProductByBarcodeAsync(barcode);
            if (product != null)
            {
                return Json(new
                {
                    success = true,
                    product = new
                    {
                        id = product.Id,
                        name = product.Name,
                        price = product.Price,
                        stock = product.Stock,
                        barcode = product.Barcode
                    }
                });
            }

            return Json(new { success = false, message = "Product not found" });
        }
    }

    public class SaleRequest
    {
        public List<SaleItemRequest>? Items { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public int? CustomerId { get; set; }
        public string? DiscountCode { get; set; }
    }

    public class OfflineSaleRequest : SaleRequest
    {
        public string ClientId { get; set; } = string.Empty;
        public DateTime QueuedAt { get; set; }
        public string? LastError { get; set; }
    }

    public class SaleItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
