using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Services;

public class CompanySubscriptionService(ApplicationDbContext context) : ICompanySubscriptionService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<int> DisableExpiredCompaniesAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var companies = await _context.Companies
            .Where(c => c.IsActive && c.PlatformAccessEndDate.HasValue)
            .ToListAsync(cancellationToken);

        var disabledCount = 0;
        foreach (var company in companies)
        {
            var graceDays = Math.Max(company.AutoDisableGraceDays, 0);
            if (today <= company.PlatformAccessEndDate!.Value.Date.AddDays(graceDays))
            {
                continue;
            }

            company.IsActive = false;
            company.PlatformDisabledDate = DateTime.Now;
            company.PlatformDisabledReason = $"Platform access expired on {company.PlatformAccessEndDate.Value:yyyy-MM-dd}; auto-disabled after {graceDays} grace day(s).";
            company.UpdatedDate = DateTime.Now;
            disabledCount++;
        }

        if (disabledCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return disabledCount;
    }

    public async Task<List<CompanySubscriptionAlertViewModel>> GetSubscriptionAlertsAsync(int daysAhead = 7, CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var alertEnd = today.AddDays(daysAhead);

        var companies = await _context.Companies
            .Where(c => c.PlatformAccessEndDate.HasValue &&
                        (c.IsActive || c.PlatformDisabledDate.HasValue) &&
                        c.PlatformAccessEndDate.Value.Date <= alertEnd)
            .OrderBy(c => c.PlatformAccessEndDate)
            .ToListAsync(cancellationToken);

        return companies
            .Select(c =>
            {
                var endDate = c.PlatformAccessEndDate!.Value.Date;
                var graceDays = Math.Max(c.AutoDisableGraceDays, 0);
                return new CompanySubscriptionAlertViewModel
                {
                    CompanyId = c.Id,
                    CompanyName = c.DisplayName,
                    AccessEndDate = endDate,
                    DaysUntilExpiry = (endDate - today).Days,
                    GraceDays = graceDays,
                    IsExpired = today > endDate,
                    IsInGracePeriod = today > endDate && today <= endDate.AddDays(graceDays),
                    ShouldDisable = c.IsActive && today > endDate.AddDays(graceDays)
                };
            })
            .OrderBy(a => a.SeverityOrder)
            .ThenBy(a => a.AccessEndDate)
            .ToList();
    }
}
