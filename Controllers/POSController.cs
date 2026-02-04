using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{
    [Authorize]
    public class POSController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICustomerService _customerService;
        private readonly ApplicationDbContext _context;

        public POSController(IProductService productService, ICustomerService customerService, ApplicationDbContext context)
        {
            _productService = productService;
            _customerService = customerService;
            _context = context;
        }

        // GET: POS
        public async Task<IActionResult> Index()
        {
            var products = await _productService.GetAllProductsAsync();
            return View(products);
        }

        // POST: POS/ProcessSale (CASH ONLY / DIRECT)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessSale([FromBody] SaleRequest request)
        {
            if (request?.Items == null || !request.Items.Any())
            {
                return Json(new { success = false, message = "No items in cart" });
            }

            try
            {
                var sale = await CreateSale(request.Items, "Cash", request.CustomerId);
                return Json(new { success = true, message = "Sale completed successfully!", saleNumber = sale.SaleNumber, saleId = sale.Id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: POS/CardTerminal (Step 1: Show Terminal)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CardTerminal(string cartJson)
        {
            if (string.IsNullOrEmpty(cartJson))
            {
                return RedirectToAction("Index");
            }

            var items = System.Text.Json.JsonSerializer.Deserialize<List<SaleItemRequest>>(cartJson);
            if (items == null || !items.Any())
            {
                return RedirectToAction("Index");
            }

            // Calculate total for display
            decimal total = 0;
            foreach (var item in items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    total += product.Price * item.Quantity;
                }
            }

            var model = new CardTerminalViewModel
            {
                CartJson = cartJson,
                TotalAmount = total,
                ItemCount = items.Sum(i => i.Quantity)
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
                if (items == null || !items.Any())
                {
                    ModelState.AddModelError("", "Cart is empty.");
                    return View("CardTerminal", model);
                }

                var sale = await CreateSale(items, "Card");
                
                // Redirect to receipt with clearCart flag
                return RedirectToAction("Receipt", "Sales", new { id = sale.Id, clearCart = 1 });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Transaction Failed: {ex.Message}");
                return View("CardTerminal", model);
            }
        }

        private async Task<Sale> CreateSale(List<SaleItemRequest> items, string paymentMethod, int? customerId = null)
        {
            var sale = new Sale
            {
                SaleNumber = $"SALE-{DateTime.Now:yyyyMMddHHmmss}",
                SaleDate = DateTime.Now,
                Status = "Completed",
                PaymentMethod = paymentMethod,
                CustomerId = customerId,
                SaleItems = new List<SaleItem>()
            };

            decimal total = 0;
            var currentUser = User.Identity?.Name ?? "Unknown";
            
            // Track stock movements to set ReferenceId after sale is saved
            var stockMovements = new List<StockMovement>();

            foreach (var item in items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null)
                {
                    throw new Exception($"Product not found: {item.ProductId}");
                }

                if (product.Stock < item.Quantity)
                {
                    throw new Exception($"Insufficient stock for {product.Name}");
                }

                var saleItem = new SaleItem
                {
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price,
                    TotalPrice = product.Price * item.Quantity
                };

                sale.SaleItems.Add(saleItem);
                total += saleItem.TotalPrice;

                // Capture previous stock
                var previousStock = product.Stock;

                // Update stock
                product.Stock -= item.Quantity;
                product.UpdatedDate = DateTime.Now;

                // Create Stock Movement (ReferenceId will be set after sale is saved)
                var movement = new StockMovement
                {
                    ProductId = product.Id,
                    MovementType = "Sale", 
                    Quantity = item.Quantity,
                    PreviousStock = previousStock,
                    NewStock = product.Stock,
                    ReferenceType = "Sale",
                    Notes = $"Sold in Sale #{sale.SaleNumber}",
                    CreatedBy = currentUser,
                    CreatedDate = DateTime.Now
                };
                
                stockMovements.Add(movement);
            }

            sale.TotalAmount = total;

            // Save sale first to get the generated Id
            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();

            // Now add stock movements with the correct ReferenceId
            foreach (var movement in stockMovements)
            {
                movement.ReferenceId = sale.Id;
                _context.StockMovements.Add(movement);
            }
            await _context.SaveChangesAsync();

            // Update Customer Stats
            if (customerId.HasValue)
            {
                await _customerService.UpdateCustomerStatsAsync(customerId.Value, total);
            }

            return sale;
        }
    }

    public class SaleRequest
    {
        public List<SaleItemRequest>? Items { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public int? CustomerId { get; set; }
    }

    public class SaleItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
