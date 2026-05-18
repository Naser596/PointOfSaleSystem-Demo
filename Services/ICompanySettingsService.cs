using WebApplication3.Models;

namespace WebApplication3.Services
{
    public interface ICompanySettingsService
    {
        Task<CompanySettings> GetSettingsAsync();
        Task<CompanySettings> SaveSettingsAsync(CompanySettings settings, string? updatedBy = null);
    }
}
