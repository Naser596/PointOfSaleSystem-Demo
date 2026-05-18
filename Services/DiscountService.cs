using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Services
{
    public class DiscountService : IDiscountService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DiscountService> _logger;
        private readonly ICurrentCompanyService _currentCompany;

        public DiscountService(ApplicationDbContext context, ILogger<DiscountService> logger, ICurrentCompanyService currentCompany)
        {
            _context = context;
            _logger = logger;
            _currentCompany = currentCompany;
        }

        public async Task<List<Discount>> GetAllDiscountsAsync()
        {
            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
            return await _context.Discounts
                .Where(d => d.CompanyId == companyId)
                .OrderByDescending(d => d.CreatedDate)
                .ToListAsync();
        }

        public async Task<Discount?> GetDiscountByIdAsync(int id)
        {
            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
            return await _context.Discounts.FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId);
        }

        public async Task<Discount> CreateDiscountAsync(Discount discount)
        {
            discount.CreatedDate = DateTime.Now;
            discount.CompanyId = await _currentCompany.GetRequiredCompanyIdAsync();
            _context.Discounts.Add(discount);
            await _context.SaveChangesAsync();
            return discount;
        }

        public async Task<Discount> UpdateDiscountAsync(Discount discount)
        {
            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
            var existing = await _context.Discounts.FirstOrDefaultAsync(d => d.Id == discount.Id && d.CompanyId == companyId);
            if (existing == null) throw new KeyNotFoundException("Discount not found");

            existing.Code = discount.Code;
            existing.Name = discount.Name;
            existing.DiscountType = discount.DiscountType;
            existing.Value = discount.Value;
            existing.StartDate = discount.StartDate;
            existing.EndDate = discount.EndDate;
            existing.IsActive = discount.IsActive;
            existing.UsageLimit = discount.UsageLimit;
            existing.MinOrderAmount = discount.MinOrderAmount;
            existing.MaxDiscountAmount = discount.MaxDiscountAmount;

            _context.Discounts.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteDiscountAsync(int id)
        {
            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
            var discount = await _context.Discounts.FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId);
            if (discount == null) return false;

            _context.Discounts.Remove(discount);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Discount?> GetValidDiscountByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;

            var normalizedCode = code.Trim().ToUpper();
            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();

            var discount = await _context.Discounts
                .FirstOrDefaultAsync(d => d.CompanyId == companyId && d.Code.ToUpper() == normalizedCode);

            if (discount == null) return null;
            if (!discount.IsActive) return null;

            var now = DateTime.Now;
            if (discount.StartDate.HasValue && discount.StartDate.Value > now) return null;
            if (discount.EndDate.HasValue && discount.EndDate.Value < now) return null;

            if (discount.UsageLimit.HasValue && discount.UsedCount >= discount.UsageLimit.Value) return null;

            return discount;
        }
    }
}
