using Microsoft.AspNetCore.Mvc;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{
    public class POSController : Controller
    {
        private readonly IProductService _productService;
        private readonly ISaleService _saleService;
        private readonly ILogger<POSController> _logger;
        private const string CartSessionKey = "CartItems";

        public POSController(IProductService productService, ISaleService saleService, ILogger<POSController> logger)
        {
            _productService = productService;
            _saleService = saleService;
            _logger = logger;
        }

        // GET: POS
        public async Task<IActionResult> Index()
        {
            try
            {
                var products = await _productService.GetAllProductsAsync();
                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading POS: {ex.Message}");
                return RedirectToAction("Error", "Home");
            }
        }

        // POST: POS/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(productId);
                if (product == null || quantity <= 0)
                {
                    return BadRequest("Invalid product or quantity");
                }

                var cart = GetCart();
                var cartItem = cart.FirstOrDefault(c => c.ProductId == productId);

                if (cartItem != null)
                {
                    cartItem.Quantity += quantity;
                    cartItem.Total = cartItem.Price * cartItem.Quantity;
                }
                else
                {
                    cart.Add(new CartItem
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Price = product.Price,
                        Quantity = quantity,
                        Total = product.Price * quantity
                    });
                }

                SaveCart(cart);
                return Json(new { success = true, message = "Product added to cart" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding to cart: {ex.Message}");
                return BadRequest("Error adding to cart");
            }
        }

        // GET: POS/Cart
        public IActionResult Cart()
        {
            var cart = GetCart();
            return View(cart);
        }

        // POST: POS/RemoveFromCart
        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            try
            {
                var cart = GetCart();
                var cartItem = cart.FirstOrDefault(c => c.ProductId == productId);
                if (cartItem != null)
                {
                    cart.Remove(cartItem);
                    SaveCart(cart);
                }
                return RedirectToAction(nameof(Cart));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing from cart: {ex.Message}");
                return RedirectToAction(nameof(Cart));
            }
        }

        // POST: POS/Checkout
        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                var cart = GetCart();
                if (cart.Count == 0)
                {
                    return BadRequest("Cart is empty");
                }

                var sale = new Sale
                {
                    SaleDate = DateTime.Now,
                    Status = "Completed",
                    TotalAmount = cart.Sum(c => c.Total),
                    SaleItems = cart.Select(c => new SaleItem
                    {
                        ProductId = c.ProductId,
                        Quantity = c.Quantity,
                        UnitPrice = c.Price,
                        TotalPrice = c.Total
                    }).ToList()
                };

                await _saleService.CreateSaleAsync(sale);
                ClearCart();

                return Json(new { success = true, message = "Sale completed successfully", saleId = sale.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during checkout: {ex.Message}");
                return BadRequest("Error during checkout");
            }
        }

        // POST: POS/UpdateQuantity
        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            try
            {
                if (quantity <= 0)
                {
                    return RemoveFromCart(productId);
                }

                var cart = GetCart();
                var cartItem = cart.FirstOrDefault(c => c.ProductId == productId);
                if (cartItem != null)
                {
                    cartItem.Quantity = quantity;
                    cartItem.Total = cartItem.Price * quantity;
                    SaveCart(cart);
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating quantity: {ex.Message}");
                return BadRequest("Error updating quantity");
            }
        }

        private List<CartItem> GetCart()
        {
            var cart = HttpContext.Session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(cart))
            {
                return new List<CartItem>();
            }
            return System.Text.Json.JsonSerializer.Deserialize<List<CartItem>>(cart) ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString(CartSessionKey, System.Text.Json.JsonSerializer.Serialize(cart));
        }

        private void ClearCart()
        {
            HttpContext.Session.Remove(CartSessionKey);
        }
    }
}
