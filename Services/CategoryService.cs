using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentCompanyService _currentCompany;

        public CategoryService(ApplicationDbContext context, ICurrentCompanyService currentCompany)
        {
            _context = context;
            _currentCompany = currentCompany;
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
            return await _context.Categories
                .Include(c => c.Products)
                .Where(c => c.CompanyId == companyId)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<List<Category>> GetActiveCategoriesAsync()
        {
            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
            return await _context.Categories
                .Where(c => c.CompanyId == companyId && c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
            return await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);
        }

        public async Task<Category> CreateCategoryAsync(Category category)
        {
            category.CreatedDate = DateTime.Now;
            category.UpdatedDate = DateTime.Now;
            category.CompanyId = await _currentCompany.GetRequiredCompanyIdAsync();
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<Category> UpdateCategoryAsync(Category category)
        {
            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
            var existing = await _context.Categories.FirstOrDefaultAsync(c => c.Id == category.Id && c.CompanyId == companyId);
            if (existing == null) throw new KeyNotFoundException("Category not found");

            existing.Name = category.Name;
            existing.Description = category.Description;
            existing.IconClass = category.IconClass;
            existing.DisplayOrder = category.DisplayOrder;
            existing.IsActive = category.IsActive;
            existing.UpdatedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteCategoryAsync(int id, string? username = null)
        {
            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);
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
            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
            return await _context.Products.CountAsync(p => p.CompanyId == companyId && p.CategoryId == categoryId);
        }
    }
}
