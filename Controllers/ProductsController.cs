using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IWebHostEnvironment _environment;

        public ProductsController(IProductService productService, ICategoryService categoryService, IWebHostEnvironment environment)
        {
            _productService = productService;
            _categoryService = categoryService;
            _environment = environment;
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            var products = await _productService.GetAllProductsAsync();
            return View(products);
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // GET: Products/Create
        public async Task<IActionResult> Create()
        {
            var model = new ProductCreateViewModel
            {
                Categories = await GetCategorySelectListAsync()
            };
            return View(model);
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var (imagePath, imageError) = await ProcessFileUpload(model.ImageFile);
                if (imageError != null)
                {
                    ModelState.AddModelError("ImageFile", imageError);
                    model.Categories = await GetCategorySelectListAsync();
                    return View(model);
                }

                var product = new Product
                {
                    Name = model.Name,
                    Description = model.Description ?? string.Empty,
                    SKU = model.SKU,
                    Price = model.Price,
                    Stock = model.Stock,
                    MinStock = model.MinStock,
                    CategoryId = model.CategoryId,
                    ImagePath = imagePath,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };

                await _productService.AddProductAsync(product);
                TempData["Success"] = "Product created successfully!";
                return RedirectToAction(nameof(Index));
            }
            model.Categories = await GetCategorySelectListAsync();
            return View(model);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var model = new ProductEditViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                SKU = product.SKU,
                Price = product.Price,
                Stock = product.Stock,
                MinStock = product.MinStock,
                CategoryId = product.CategoryId,
                CurrentImagePath = product.ImagePath,
                Categories = await GetCategorySelectListAsync()
            };

            return View(model);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductEditViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existingProduct = await _productService.GetProductByIdAsync(id);
                if (existingProduct == null)
                {
                    return NotFound();
                }

                existingProduct.Name = model.Name;
                existingProduct.Description = model.Description ?? string.Empty;
                existingProduct.SKU = model.SKU;
                existingProduct.Price = model.Price;
                existingProduct.Stock = model.Stock;
                existingProduct.MinStock = model.MinStock;
                existingProduct.CategoryId = model.CategoryId;
                existingProduct.UpdatedDate = DateTime.Now;

                if (model.ImageFile != null)
                {
                    var (newImagePath, imageError) = await ProcessFileUpload(model.ImageFile);
                    if (imageError != null)
                    {
                        ModelState.AddModelError("ImageFile", imageError);
                        model.Categories = await GetCategorySelectListAsync();
                        return View(model);
                    }
                    if (newImagePath != null)
                    {
                        // Delete old image if it exists and is not the placeholder
                        if (!string.IsNullOrEmpty(existingProduct.ImagePath) && 
                            !existingProduct.ImagePath.Contains("placeholder"))
                        {
                            var oldFilePath = Path.Combine(_environment.WebRootPath, existingProduct.ImagePath.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }
                        existingProduct.ImagePath = newImagePath;
                    }
                }

                await _productService.UpdateProductAsync(existingProduct);
                TempData["Success"] = "Product updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            model.Categories = await GetCategorySelectListAsync();
            return View(model);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            // Optionally delete image when product is deleted
            if (product != null && !string.IsNullOrEmpty(product.ImagePath) && !product.ImagePath.Contains("placeholder"))
            {
                 var filePath = Path.Combine(_environment.WebRootPath, product.ImagePath.TrimStart('/'));
                 if (System.IO.File.Exists(filePath))
                 {
                     System.IO.File.Delete(filePath);
                 }
            }

            await _productService.DeleteProductAsync(id, User.Identity?.Name);
            TempData["Success"] = "Product deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
        
        private async Task<List<SelectListItem>> GetCategorySelectListAsync()
        {
            var categories = await _categoryService.GetActiveCategoriesAsync();
            var items = categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToList();
            items.Insert(0, new SelectListItem { Value = "", Text = "-- Select Category --" });
            return items;
        }

        private async Task<(string? Path, string? Error)> ProcessFileUpload(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return (null, null);
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return (null, "Only JPG, JPEG, PNG, and WEBP images are allowed.");
            }

            if (file.Length > 2 * 1024 * 1024) // 2MB
            {
                return (null, "Image size must be less than 2MB.");
            }

            var fileName = $"{Guid.NewGuid()}{extension}";
            var uploadDir = Path.Combine(_environment.WebRootPath, "images", "products");
            
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            var filePath = Path.Combine(uploadDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return ($"/images/products/{fileName}", null);
        }
    }
}
