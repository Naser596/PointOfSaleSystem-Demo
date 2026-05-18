namespace WebApplication3.Services;

public class CompanySubscriptionMonitorService(
    IServiceScopeFactory scopeFactory,
    ILogger<CompanySubscriptionMonitorService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<CompanySubscriptionMonitorService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunCheckAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(6));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCheckAsync(stoppingToken);
        }
    }

    private async Task RunCheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ICompanySubscriptionService>();
            var disabled = await service.DisableExpiredCompaniesAsync(cancellationToken);
            if (disabled > 0)
            {
                _logger.LogWarning("Auto-disabled {CompanyCount} company account(s) because platform access expired.", disabled);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while checking company platform access expiration.");
        }
    }
}
