using Microsoft.AspNetCore.Mvc;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductService productService, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            try
            {
                var products = await _productService.GetAllProductsAsync();
                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting products: {ex.Message}");
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    return NotFound();
                }
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting product details: {ex.Message}");
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Price,Stock")] Product product)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await _productService.AddProductAsync(product);
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating product: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while creating the product.");
            }
            return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    return NotFound();
                }
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting product for edit: {ex.Message}");
                return RedirectToAction("Error", "Home");
            }
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,Stock,CreatedDate")] Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    await _productService.UpdateProductAsync(product);
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating product: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while updating the product.");
            }
            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _productService.DeleteProductAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting product: {ex.Message}");
                return RedirectToAction("Error", "Home");
            }
        }
    }
}
