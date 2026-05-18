using WebApplication3.Services;

namespace WebApplication3.Tests.Support;

public sealed class FixedCurrentCompanyService(int? companyId) : ICurrentCompanyService
{
    public Task<int?> GetCompanyIdAsync()
    {
        return Task.FromResult(companyId);
    }

    public Task<int> GetRequiredCompanyIdAsync()
    {
        if (!companyId.HasValue)
        {
            throw new UnauthorizedAccessException("A company context is required for this operation.");
        }

        return Task.FromResult(companyId.Value);
    }
}
