using WebApplication3.Models;

namespace WebApplication3.Services
{
    public interface ICategoryService
    {
        Task<List<Category>> GetAllCategoriesAsync();
        Task<List<Category>> GetActiveCategoriesAsync();
        Task<Category?> GetCategoryByIdAsync(int id);
        Task<Category> CreateCategoryAsync(Category category);
        Task<Category> UpdateCategoryAsync(Category category);
        Task<bool> DeleteCategoryAsync(int id, string? username = null);
        Task<int> GetProductCountAsync(int categoryId);
    }
}
