using Microsoft.Extensions.Logging;
using SubscriptionBillingAPI.Application.Common.Interfaces;
using SubscriptionBillingAPI.Domain.Enums;

namespace SubscriptionBillingAPI.Infrastructure.Services;

/// <summary>
/// Recurring Hangfire job: Daily at 00:05 UTC.
/// Finds subscriptions ending today and logs renewal processing.
/// Stripe handles actual renewal billing; this job ensures our records are updated.
/// </summary>
public class DailyRenewalCheckJob
{
    private readonly IAppDbContext _context;
    private readonly ILogger<DailyRenewalCheckJob> _logger;

    public DailyRenewalCheckJob(IAppDbContext context, ILogger<DailyRenewalCheckJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;

        var expiringSubscriptions = _context.Subscriptions
            .Where(s => s.EndDate.HasValue &&
                        s.EndDate.Value.Date == today &&
                        s.AutoRenew &&
                        s.Status == SubscriptionStatus.Active)
            .ToList();

        _logger.LogInformation("DailyRenewalCheck: Found {Count} subscriptions expiring today.", expiringSubscriptions.Count);

        foreach (var subscription in expiringSubscriptions)
        {
            // Stripe handles the actual billing via its subscription lifecycle.
            // This job is for reconciliation: extend EndDate for the next billing cycle.
            if (subscription.Plan is not null)
            {
                subscription.EndDate = subscription.Plan.BillingCycle == BillingCycle.Yearly
                    ? today.AddYears(1)
                    : today.AddMonths(1);
            }
            else
            {
                subscription.EndDate = today.AddMonths(1);
            }

            subscription.UpdatedAt = DateTime.UtcNow;
            _logger.LogInformation("DailyRenewalCheck: Extended subscription {SubId} for user {UserId}.",
                subscription.Id, subscription.UserId);
        }

        if (expiringSubscriptions.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("DailyRenewalCheck: Completed.");
    }
}
