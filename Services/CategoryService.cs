using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;

        public CategoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await _context.Categories
                .Include(c => c.Products)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<List<Category>> GetActiveCategoriesAsync()
        {
            return await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Category> CreateCategoryAsync(Category category)
        {
            category.CreatedDate = DateTime.Now;
            category.UpdatedDate = DateTime.Now;
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<Category> UpdateCategoryAsync(Category category)
        {
            category.UpdatedDate = DateTime.Now;
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<bool> DeleteCategoryAsync(int id, string? username = null)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return false;
            
            // Soft Delete
            category.IsDeleted = true;
            category.DeletedDate = DateTime.Now;
            category.DeletedBy = username ?? "System";
            category.IsActive = false;

            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetProductCountAsync(int categoryId)
        {
            return await _context.Products.CountAsync(p => p.CategoryId == categoryId);
        }
    }
}
