using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CheckoutController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: Checkout/Payment
        // Receives cart data from POS Terminal and displays the payment form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Payment(string cartJson)
        {
            if (string.IsNullOrEmpty(cartJson))
            {
                TempData["Error"] = "No cart data provided.";
                return RedirectToAction("Index", "POS");
            }

            try
            {
                var cartItems = JsonSerializer.Deserialize<List<CartItemViewModel>>(cartJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (cartItems == null || !cartItems.Any())
                {
                    TempData["Error"] = "Cart is empty.";
                    return RedirectToAction("Index", "POS");
                }

                var model = new CheckoutViewModel
                {
                    CartItems = cartItems,
                    TotalAmount = cartItems.Sum(i => i.Total),
                    CartJson = cartJson // Keep for form submission
                };

                return View(model);
            }
            catch (JsonException)
            {
                TempData["Error"] = "Invalid cart data.";
                return RedirectToAction("Index", "POS");
            }
        }

        // POST: Checkout/Pay
        // Validates payment and creates the sale
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(CheckoutViewModel model)
        {
            // Re-parse cart items from the hidden field
            List<CartItemViewModel>? cartItems = null;
            if (!string.IsNullOrEmpty(model.CartJson))
            {
                try
                {
                    cartItems = JsonSerializer.Deserialize<List<CartItemViewModel>>(model.CartJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (JsonException)
                {
                    TempData["Error"] = "Invalid cart data.";
                    return RedirectToAction("Index", "POS");
                }
            }

            if (cartItems == null || !cartItems.Any())
            {
                TempData["Error"] = "Cart is empty.";
                return RedirectToAction("Index", "POS");
            }

            model.CartItems = cartItems;
            model.TotalAmount = cartItems.Sum(i => i.Total);

            // Validate card fields if DebitCard is selected
            if (model.PaymentMethod == "DebitCard")
            {
                if (string.IsNullOrWhiteSpace(model.CardholderName))
                    ModelState.AddModelError("CardholderName", "Cardholder name is required.");
                if (string.IsNullOrWhiteSpace(model.CardNumber) || model.CardNumber.Length < 13)
                    ModelState.AddModelError("CardNumber", "Valid card number is required.");
                if (string.IsNullOrWhiteSpace(model.CardExpiry))
                    ModelState.AddModelError("CardExpiry", "Card expiry is required.");
                if (string.IsNullOrWhiteSpace(model.CardCVV) || model.CardCVV.Length < 3)
                    ModelState.AddModelError("CardCVV", "Valid CVV is required.");
            }

            // Check FullName validation
            if (string.IsNullOrWhiteSpace(model.FullName))
            {
                ModelState.AddModelError("FullName", "Full name is required.");
            }

            if (!ModelState.IsValid)
            {
                return View("Payment", model);
            }

            try
            {
                // Create Sale (reusing logic from POSController.ProcessSale)
                var sale = new Sale
                {
                    SaleNumber = $"SALE-{DateTime.Now:yyyyMMddHHmmss}",
                    SaleDate = DateTime.Now,
                    Status = "Completed",
                    PaymentMethod = model.PaymentMethod,
                    SaleItems = new List<SaleItem>()
                };

                decimal total = 0;

                foreach (var item in cartItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                    {
                        TempData["Error"] = $"Product not found: {item.Name}";
                        return View("Payment", model);
                    }

                    if (product.Stock < item.Quantity)
                    {
                        TempData["Error"] = $"Insufficient stock for {product.Name}";
                        return View("Payment", model);
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

                    // Update stock
                    product.Stock -= item.Quantity;
                    product.UpdatedDate = DateTime.Now;
                }

                sale.TotalAmount = total;

                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();

                // Store success info for the Success page
                TempData["SaleNumber"] = sale.SaleNumber;
                TempData["TotalAmount"] = sale.TotalAmount.ToString("N2");
                TempData["PaymentMethod"] = sale.PaymentMethod;

                return RedirectToAction("Success");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error processing payment: {ex.Message}";
                return View("Payment", model);
            }
        }

        // GET: Checkout/Success
        // Displays the payment success confirmation
        public IActionResult Success()
        {
            var model = new PaymentSuccessViewModel
            {
                SaleNumber = TempData["SaleNumber"]?.ToString() ?? "N/A",
                TotalAmount = decimal.TryParse(TempData["TotalAmount"]?.ToString(), out var total) ? total : 0,
                PaymentMethod = TempData["PaymentMethod"]?.ToString() ?? "Unknown",
                SaleDate = DateTime.Now
            };

            return View(model);
        }
    }
}
