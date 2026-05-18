using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoriesController(ICategoryService categoryService, IAuditLogService auditLog) : Controller
    {
        private readonly ICategoryService _categoryService = categoryService;
        private readonly IAuditLogService _auditLog = auditLog;

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return View(categories);
        }

        // GET: Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                await _categoryService.CreateCategoryAsync(category);
                await _auditLog.LogAsync("Create", nameof(Category), category.Id.ToString(), $"Created category {category.Name}");
                TempData["Success"] = "Category created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await _categoryService.UpdateCategoryAsync(category);
                await _auditLog.LogAsync("Update", nameof(Category), category.Id.ToString(), $"Updated category {category.Name}");
                TempData["Success"] = "Category updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var productCount = await _categoryService.GetProductCountAsync(id);
            if (productCount > 0)
            {
                TempData["Error"] = $"Cannot delete category. It has {productCount} product(s) assigned.";
                return RedirectToAction(nameof(Index));
            }

            var category = await _categoryService.GetCategoryByIdAsync(id);
            await _categoryService.DeleteCategoryAsync(id, User.Identity?.Name);
            if (category != null)
            {
                await _auditLog.LogAsync("Delete", nameof(Category), category.Id.ToString(), $"Deleted category {category.Name}");
            }
            TempData["Success"] = "Category deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
