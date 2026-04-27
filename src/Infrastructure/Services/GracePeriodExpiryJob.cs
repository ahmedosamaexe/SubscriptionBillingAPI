using Microsoft.Extensions.Logging;
using SubscriptionBillingAPI.Application.Common.Interfaces;
using SubscriptionBillingAPI.Domain.Enums;

namespace SubscriptionBillingAPI.Infrastructure.Services;

/// <summary>
/// Recurring Hangfire job: Daily at 01:00 UTC.
/// Finds subscriptions in GracePeriod where the grace window has expired
/// and transitions them to Suspended.
/// </summary>
public class GracePeriodExpiryJob
{
    private readonly IAppDbContext _context;
    private readonly ILogger<GracePeriodExpiryJob> _logger;

    public GracePeriodExpiryJob(IAppDbContext context, ILogger<GracePeriodExpiryJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var expiredGracePeriods = _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.GracePeriod &&
                        s.GracePeriodEndDate.HasValue &&
                        s.GracePeriodEndDate.Value < now)
            .ToList();

        _logger.LogInformation("GracePeriodExpiry: Found {Count} expired grace periods.", expiredGracePeriods.Count);

        foreach (var subscription in expiredGracePeriods)
        {
            subscription.Status = SubscriptionStatus.Suspended;
            subscription.GracePeriodEndDate = null;
            subscription.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("GracePeriodExpiry: Suspended subscription {SubId} for user {UserId}.",
                subscription.Id, subscription.UserId);
        }

        if (expiredGracePeriods.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("GracePeriodExpiry: Completed.");
    }
}
