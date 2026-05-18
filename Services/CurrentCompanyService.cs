using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using WebApplication3.Models;

namespace WebApplication3.Services
{
    public class CurrentCompanyService : ICurrentCompanyService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public CurrentCompanyService(IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> userManager)
        {
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        public async Task<int?> GetCompanyIdAsync()
        {
            var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return null;

            var user = await _userManager.FindByIdAsync(userId);
            return user?.CompanyId;
        }

        public async Task<int> GetRequiredCompanyIdAsync()
        {
            var companyId = await GetCompanyIdAsync();
            if (!companyId.HasValue)
            {
                throw new UnauthorizedAccessException("A company context is required for this operation.");
            }

            return companyId.Value;
        }
    }
}
