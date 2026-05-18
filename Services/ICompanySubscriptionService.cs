using WebApplication3.Models;

namespace WebApplication3.Services;

public interface ICompanySubscriptionService
{
    Task<int> DisableExpiredCompaniesAsync(CancellationToken cancellationToken = default);
    Task<List<CompanySubscriptionAlertViewModel>> GetSubscriptionAlertsAsync(int daysAhead = 7, CancellationToken cancellationToken = default);
}
