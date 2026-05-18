using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductsController(
        IProductService productService,
        ICategoryService categoryService,
        IWebHostEnvironment environment,
        ICurrentCompanyService currentCompany,
        ApplicationDbContext context,
        IAuditLogService auditLog) : Controller
    {
        private readonly IProductService _productService = productService;
        private readonly ICategoryService _categoryService = categoryService;
        private readonly IWebHostEnvironment _environment = environment;
        private readonly ICurrentCompanyService _currentCompany = currentCompany;
        private readonly ApplicationDbContext _context = context;
        private readonly IAuditLogService _auditLog = auditLog;

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
            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
            var defaultTaxRate = await _context.Companies
                .Where(c => c.Id == companyId)
                .Select(c => c.DefaultTaxRate)
                .FirstOrDefaultAsync();

            var model = new ProductCreateViewModel
            {
                TaxRate = defaultTaxRate,
                Categories = await GetCategorySelectListAsync(),
                Warehouses = await GetWarehouseSelectListAsync(),
                StockLocations = await GetStockLocationSelectListAsync()
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
                    await PopulateProductCreateListsAsync(model);
                    return View(model);
                }

                if (model.Stock > 0 && !model.InitialWarehouseId.HasValue)
                {
                    ModelState.AddModelError(nameof(model.InitialWarehouseId), "Warehouse is required when initial stock is greater than zero.");
                    await PopulateProductCreateListsAsync(model);
                    return View(model);
                }

                var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
                if (model.InitialWarehouseId.HasValue)
                {
                    var warehouseExists = await _context.Warehouses
                        .AnyAsync(w => w.CompanyId == companyId && w.Id == model.InitialWarehouseId.Value);
                    if (!warehouseExists) return NotFound();

                    if (model.InitialStockLocationId.HasValue)
                    {
                        var locationExists = await _context.StockLocations.AnyAsync(l =>
                            l.CompanyId == companyId &&
                            l.WarehouseId == model.InitialWarehouseId.Value &&
                            l.Id == model.InitialStockLocationId.Value);
                        if (!locationExists)
                        {
                            ModelState.AddModelError(nameof(model.InitialStockLocationId), "Location belongs to another warehouse.");
                            await PopulateProductCreateListsAsync(model);
                            return View(model);
                        }
                    }
                }

                var product = new Product
                {
                    Name = model.Name,
                    Description = model.Description ?? string.Empty,
                    SKU = model.SKU,
                    Barcode = model.Barcode,
                    CostPrice = model.CostPrice,
                    Price = model.Price,
                    TaxRate = model.TaxRate,
                    Stock = model.Stock,
                    MinStock = model.MinStock,
                    CategoryId = model.CategoryId,
                    ImagePath = imagePath,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };

                await _productService.AddProductAsync(product);
                if (model.Stock > 0 && model.InitialWarehouseId.HasValue)
                {
                    _context.WarehouseStocks.Add(new WarehouseStock
                    {
                        CompanyId = companyId,
                        WarehouseId = model.InitialWarehouseId.Value,
                        StockLocationId = model.InitialStockLocationId,
                        ProductId = product.Id,
                        QuantityOnHand = model.Stock,
                        QuantityReserved = 0,
                        UpdatedDate = DateTime.Now
                    });

                    _context.StockMovements.Add(new StockMovement
                    {
                        CompanyId = companyId,
                        ProductId = product.Id,
                        MovementType = "InitialStock",
                        Quantity = model.Stock,
                        PreviousStock = 0,
                        NewStock = model.Stock,
                        ReferenceType = "ProductCreation",
                        Notes = string.IsNullOrWhiteSpace(model.StockOriginNote)
                            ? "Initial stock assigned during product creation"
                            : model.StockOriginNote.Trim(),
                        CreatedBy = User.Identity?.Name,
                        CreatedDate = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                }

                await _auditLog.LogAsync("Create", nameof(Product), product.Id.ToString(), $"Created product {product.Name}");
                TempData["Success"] = "Product created successfully!";
                return RedirectToAction(nameof(Index));
            }
            await PopulateProductCreateListsAsync(model);
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
                Barcode = product.Barcode,
                CostPrice = product.CostPrice,
                Price = product.Price,
                TaxRate = product.TaxRate,
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
                existingProduct.Barcode = model.Barcode;
                existingProduct.CostPrice = model.CostPrice;
                existingProduct.Price = model.Price;
                existingProduct.TaxRate = model.TaxRate;
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
                await _auditLog.LogAsync("Update", nameof(Product), existingProduct.Id.ToString(), $"Updated product {existingProduct.Name}");
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
            if (product != null)
            {
                await _auditLog.LogAsync("Delete", nameof(Product), product.Id.ToString(), $"Deleted product {product.Name}");
            }
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

        private async Task PopulateProductCreateListsAsync(ProductCreateViewModel model)
        {
            model.Categories = await GetCategorySelectListAsync();
            model.Warehouses = await GetWarehouseSelectListAsync();
            model.StockLocations = await GetStockLocationSelectListAsync();
        }

        private async Task<List<SelectListItem>> GetWarehouseSelectListAsync()
        {
            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
            var warehouses = await _context.Warehouses
                .Where(w => w.CompanyId == companyId && w.IsActive)
                .OrderBy(w => w.Name)
                .ToListAsync();

            var items = warehouses.Select(w => new SelectListItem
            {
                Value = w.Id.ToString(),
                Text = w.Name
            }).ToList();
            items.Insert(0, new SelectListItem { Value = "", Text = "-- Select Warehouse --" });
            return items;
        }

        private async Task<List<SelectListItem>> GetStockLocationSelectListAsync()
        {
            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
            var locations = await _context.StockLocations
                .Include(l => l.Warehouse)
                .Where(l => l.CompanyId == companyId && l.IsActive)
                .OrderBy(l => l.Warehouse.Name)
                .ThenBy(l => l.Name)
                .ToListAsync();

            var items = locations.Select(l => new SelectListItem
            {
                Value = l.Id.ToString(),
                Text = $"{l.Warehouse.Name} - {l.Name}"
            }).ToList();
            items.Insert(0, new SelectListItem { Value = "", Text = "-- No Location --" });
            return items;
        }

        // GET: Products/Filter
        public async Task<IActionResult> Filter(string? searchTerm, int? categoryId)
        {
            var products = await _productService.GetProductsAsync(searchTerm, categoryId);
            return PartialView("_ProductTable", products);
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
