using WebApplication3.Models;

namespace WebApplication3.Services
{
    public interface IDiscountService
    {
        Task<List<Discount>> GetAllDiscountsAsync();
        Task<Discount?> GetDiscountByIdAsync(int id);
        Task<Discount> CreateDiscountAsync(Discount discount);
        Task<Discount> UpdateDiscountAsync(Discount discount);
        Task<bool> DeleteDiscountAsync(int id);
        Task<Discount?> GetValidDiscountByCodeAsync(string code);
    }
}
