namespace WebApplication3.Services
{
    public interface ICurrentCompanyService
    {
        Task<int?> GetCompanyIdAsync();
        Task<int> GetRequiredCompanyIdAsync();
    }
}
