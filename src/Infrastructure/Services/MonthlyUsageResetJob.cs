using Microsoft.Extensions.Logging;
using SubscriptionBillingAPI.Application.Services;

namespace SubscriptionBillingAPI.Infrastructure.Services;

/// <summary>
/// Recurring Hangfire job: 1st of every month at 00:01 UTC.
/// Resets usage counters by calling the Application layer's IUsageService.
/// The previous month's data is reset (Count = 0).
/// </summary>
public class MonthlyUsageResetJob
{
    private readonly IUsageService _usageService;
    private readonly ILogger<MonthlyUsageResetJob> _logger;

    public MonthlyUsageResetJob(IUsageService usageService, ILogger<MonthlyUsageResetJob> logger)
    {
        _usageService = usageService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Reset the previous month's usage
        var now = DateTime.UtcNow;
        var previousMonth = now.AddMonths(-1);
        var month = previousMonth.Month;
        var year = previousMonth.Year;

        _logger.LogInformation("MonthlyUsageReset: Resetting usage for {Month}/{Year}.", month, year);

        await _usageService.ResetMonthlyUsageAsync(month, year, cancellationToken);

        _logger.LogInformation("MonthlyUsageReset: Completed.");
    }
}
