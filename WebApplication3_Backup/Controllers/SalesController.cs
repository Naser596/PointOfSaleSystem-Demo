using Microsoft.AspNetCore.Mvc;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{
    public class SalesController : Controller
    {
        private readonly ISaleService _saleService;
        private readonly IProductService _productService;
        private readonly ILogger<SalesController> _logger;

        public SalesController(ISaleService saleService, IProductService productService, ILogger<SalesController> logger)
        {
            _saleService = saleService;
            _productService = productService;
            _logger = logger;
        }

        // GET: Sales
        public async Task<IActionResult> Index()
        {
            try
            {
                var sales = await _saleService.GetAllSalesAsync();
                return View(sales);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting sales: {ex.Message}");
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Sales/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var sale = await _saleService.GetSaleByIdAsync(id);
                if (sale == null)
                {
                    return NotFound();
                }
                return View(sale);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting sale details: {ex.Message}");
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Sales/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var products = await _productService.GetAllProductsAsync();
                ViewBag.Products = products;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading create sale page: {ex.Message}");
                return RedirectToAction("Error", "Home");
            }
        }

        // POST: Sales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("SaleDate,Status")] Sale sale, List<int> productIds, List<int> quantities)
        {
            try
            {
                if (ModelState.IsValid && productIds.Count > 0)
                {
                    decimal totalAmount = 0;
                    var saleItems = new List<SaleItem>();

                    for (int i = 0; i < productIds.Count; i++)
                    {
                        var product = await _productService.GetProductByIdAsync(productIds[i]);
                        if (product != null && quantities[i] > 0)
                        {
                            decimal itemTotal = product.Price * quantities[i];
                            totalAmount += itemTotal;

                            saleItems.Add(new SaleItem
                            {
                                ProductId = product.Id,
                                Quantity = quantities[i],
                                UnitPrice = product.Price,
                                TotalPrice = itemTotal
                            });
                        }
                    }

                    if (saleItems.Count > 0)
                    {
                        sale.TotalAmount = totalAmount;
                        sale.SaleItems = saleItems;
                        await _saleService.CreateSaleAsync(sale);
                        return RedirectToAction(nameof(Index));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating sale: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while creating the sale.");
            }

            var products = await _productService.GetAllProductsAsync();
            ViewBag.Products = products;
            return View(sale);
        }

        // POST: Sales/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _saleService.DeleteSaleAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting sale: {ex.Message}");
                return RedirectToAction("Error", "Home");
            }
        }
    }
}
