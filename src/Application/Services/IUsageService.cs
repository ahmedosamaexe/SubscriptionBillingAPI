using SubscriptionBillingAPI.Application.Common.Models;
using SubscriptionBillingAPI.Application.DTOs.Usage;

namespace SubscriptionBillingAPI.Application.Services;

public interface IUsageService
{
    /// <summary>Increments the usage counter for a specific action.</summary>
    Task<Result> IncrementUsageAsync(Guid userId, string action, int quantity = 1, CancellationToken cancellationToken = default);

    /// <summary>Returns usage stats for the current user for the current month.</summary>
    Task<Result<IEnumerable<UsageResponse>>> GetCurrentUsageAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Checks whether the user has exceeded MaxRequests for the current month.</summary>
    Task<bool> IsQuotaExceededAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Resets all usage counters for the specified month/year.</summary>
    Task ResetMonthlyUsageAsync(int month, int year, CancellationToken cancellationToken = default);
}
