using SubscriptionBillingAPI.Application.Common.Interfaces;
using SubscriptionBillingAPI.Application.Common.Models;
using SubscriptionBillingAPI.Application.DTOs.Usage;
using SubscriptionBillingAPI.Domain.Entities;
using SubscriptionBillingAPI.Domain.Enums;

namespace SubscriptionBillingAPI.Application.Services;

public class UsageService : IUsageService
{
    private readonly IAppDbContext _context;

    public UsageService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result> IncrementUsageAsync(Guid userId, string action, int quantity = 1, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var month = now.Month;
        var year = now.Year;

        // Find the user's active subscription
        var subscription = _context.Subscriptions
            .FirstOrDefault(s => s.UserId == userId &&
                (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.GracePeriod));

        if (subscription is null)
            return Result.Failure("No active subscription found.");

        // Find existing usage log for this action in the current period
        var usageLog = _context.UsageLogs
            .FirstOrDefault(ul => ul.UserId == userId &&
                                  ul.Action == action &&
                                  ul.Month == month &&
                                  ul.Year == year);

        if (usageLog is not null)
        {
            usageLog.Count += quantity;
            usageLog.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            usageLog = new UsageLog
            {
                UserId = userId,
                SubscriptionId = subscription.Id,
                Action = action,
                Count = quantity,
                Month = month,
                Year = year,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Add(usageLog);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IEnumerable<UsageResponse>>> GetCurrentUsageAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var month = now.Month;
        var year = now.Year;

        // Find the user's active subscription and plan
        var subscription = _context.Subscriptions
            .FirstOrDefault(s => s.UserId == userId &&
                (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.GracePeriod));

        if (subscription is null)
            return Result<IEnumerable<UsageResponse>>.Failure("No active subscription found.");

        var plan = await _context.FindAsync<Plan>(subscription.PlanId, cancellationToken);
        if (plan is null)
            return Result<IEnumerable<UsageResponse>>.Failure("Plan not found.");

        var usageLogs = _context.UsageLogs
            .Where(ul => ul.UserId == userId && ul.Month == month && ul.Year == year)
            .ToList();

        // Build the aggregated "api_calls" total for MaxRequests comparison
        var totalApiCalls = usageLogs.Sum(ul => ul.Count);

        var responses = usageLogs.Select(ul => new UsageResponse
        {
            Action = ul.Action,
            CurrentCount = ul.Count,
            MaxAllowed = plan.MaxRequests,
            IsUnlimited = plan.MaxRequests == 0,
            Month = month,
            Year = year,
            UsagePercentage = plan.MaxRequests > 0
                ? Math.Round((double)ul.Count / plan.MaxRequests * 100, 2)
                : 0
        }).ToList();

        // If no usage logs exist, still return a summary entry
        if (responses.Count == 0)
        {
            responses.Add(new UsageResponse
            {
                Action = "total",
                CurrentCount = 0,
                MaxAllowed = plan.MaxRequests,
                IsUnlimited = plan.MaxRequests == 0,
                Month = month,
                Year = year,
                UsagePercentage = 0
            });
        }

        return await Task.FromResult(Result<IEnumerable<UsageResponse>>.Success(responses));
    }

    public async Task<bool> IsQuotaExceededAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var month = now.Month;
        var year = now.Year;

        // Find the user's active subscription
        var subscription = _context.Subscriptions
            .FirstOrDefault(s => s.UserId == userId &&
                (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.GracePeriod));

        if (subscription is null)
            return true; // No subscription = exceeded

        var plan = await _context.FindAsync<Plan>(subscription.PlanId, cancellationToken);
        if (plan is null)
            return true;

        // 0 = unlimited
        if (plan.MaxRequests == 0)
            return false;

        // Sum all usage logs for this user in the current month
        var totalUsage = _context.UsageLogs
            .Where(ul => ul.UserId == userId && ul.Month == month && ul.Year == year)
            .Sum(ul => ul.Count);

        return totalUsage >= plan.MaxRequests;
    }

    public async Task ResetMonthlyUsageAsync(int month, int year, CancellationToken cancellationToken = default)
    {
        var logsToReset = _context.UsageLogs
            .Where(ul => ul.Month == month && ul.Year == year)
            .ToList();

        foreach (var log in logsToReset)
        {
            log.Count = 0;
            log.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
