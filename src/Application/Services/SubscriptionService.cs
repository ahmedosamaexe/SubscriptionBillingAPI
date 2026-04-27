using SubscriptionBillingAPI.Application.Common.Interfaces;
using SubscriptionBillingAPI.Application.Common.Models;
using SubscriptionBillingAPI.Application.DTOs.Subscriptions;
using SubscriptionBillingAPI.Domain.Entities;
using SubscriptionBillingAPI.Domain.Enums;

namespace SubscriptionBillingAPI.Application.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly IAppDbContext _context;
    private readonly IStripePaymentService _stripePaymentService;

    public SubscriptionService(IAppDbContext context, IStripePaymentService stripePaymentService)
    {
        _context = context;
        _stripePaymentService = stripePaymentService;
    }

    public async Task<Result<string>> SubscribeAsync(Guid userId, SubscribeRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.FindAsync<User>(userId, cancellationToken);
        if (user is null)
            return Result<string>.Failure("User not found.");

        var plan = await _context.FindAsync<Plan>(request.PlanId, cancellationToken);
        if (plan is null)
            return Result<string>.Failure("Plan not found.");

        if (!plan.IsActive)
            return Result<string>.Failure("This plan is not currently available.");

        if (string.IsNullOrEmpty(plan.StripePriceId))
            return Result<string>.Failure("This plan is not configured for payment processing.");

        // Check if user already has an active subscription
        var existingActive = _context.Subscriptions
            .FirstOrDefault(s => s.UserId == userId &&
                (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.GracePeriod));

        if (existingActive is not null)
            return Result<string>.Failure("You already have an active subscription. Please cancel or upgrade instead.");

        // Create Stripe Checkout Session
        var checkoutUrl = await _stripePaymentService.CreateCheckoutSessionAsync(
            userId,
            user.Email,
            plan.Id,
            plan.StripePriceId,
            user.StripeCustomerId,
            cancellationToken);

        return Result<string>.Success(checkoutUrl);
    }

    public async Task<Result> CancelAsync(Guid userId, Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await _context.FindAsync<Subscription>(subscriptionId, cancellationToken);
        if (subscription is null)
            return Result.Failure("Subscription not found.");

        if (subscription.UserId != userId)
            return Result.Failure("You do not own this subscription.");

        if (subscription.Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Expired)
            return Result.Failure("This subscription is already cancelled or expired.");

        if (!string.IsNullOrEmpty(subscription.StripeSubscriptionId))
        {
            await _stripePaymentService.CancelSubscriptionAsync(
                subscription.StripeSubscriptionId, cancellationToken);
        }

        subscription.Cancel();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<string>> UpgradeAsync(Guid userId, Guid subscriptionId, UpgradeRequest request, CancellationToken cancellationToken = default)
    {
        var subscription = await _context.FindAsync<Subscription>(subscriptionId, cancellationToken);
        if (subscription is null)
            return Result<string>.Failure("Subscription not found.");

        if (subscription.UserId != userId)
            return Result<string>.Failure("You do not own this subscription.");

        if (!subscription.AllowsAccess())
            return Result<string>.Failure("Only active or grace-period subscriptions can be upgraded.");

        var newPlan = await _context.FindAsync<Plan>(request.NewPlanId, cancellationToken);
        if (newPlan is null)
            return Result<string>.Failure("Target plan not found.");

        if (!newPlan.IsActive)
            return Result<string>.Failure("The target plan is not currently available.");

        if (string.IsNullOrEmpty(newPlan.StripePriceId))
            return Result<string>.Failure("The target plan is not configured for payment processing.");

        if (subscription.PlanId == request.NewPlanId)
            return Result<string>.Failure("You are already subscribed to this plan.");

        var user = await _context.FindAsync<User>(userId, cancellationToken);
        if (user is null)
            return Result<string>.Failure("User not found.");

        var checkoutUrl = await _stripePaymentService.CreateUpgradeCheckoutSessionAsync(
            userId,
            user.Email,
            subscription.StripeSubscriptionId!,
            newPlan.StripePriceId,
            subscription.StripeCustomerId!,
            cancellationToken);

        return Result<string>.Success(checkoutUrl);
    }

    public async Task<Result> PauseAsync(Guid userId, Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await _context.FindAsync<Subscription>(subscriptionId, cancellationToken);
        if (subscription is null)
            return Result.Failure("Subscription not found.");

        if (subscription.UserId != userId)
            return Result.Failure("You do not own this subscription.");

        if (subscription.Status != SubscriptionStatus.Active)
            return Result.Failure("Only active subscriptions can be paused.");

        if (!string.IsNullOrEmpty(subscription.StripeSubscriptionId))
        {
            await _stripePaymentService.PauseSubscriptionAsync(
                subscription.StripeSubscriptionId, cancellationToken);
        }

        subscription.Pause();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<SubscriptionResponse>> GetByIdAsync(Guid userId, Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await _context.FindAsync<Subscription>(subscriptionId, cancellationToken);
        if (subscription is null)
            return Result<SubscriptionResponse>.Failure("Subscription not found.");

        if (subscription.UserId != userId)
            return Result<SubscriptionResponse>.Failure("You do not own this subscription.");

        var plan = await _context.FindAsync<Plan>(subscription.PlanId, cancellationToken);

        return Result<SubscriptionResponse>.Success(MapToResponse(subscription, plan?.Name ?? "Unknown"));
    }

    public async Task<Result<IEnumerable<SubscriptionResponse>>> GetUserSubscriptionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var subscriptions = _context.Subscriptions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToList();

        var responses = new List<SubscriptionResponse>();
        foreach (var sub in subscriptions)
        {
            var plan = await _context.FindAsync<Plan>(sub.PlanId, cancellationToken);
            responses.Add(MapToResponse(sub, plan?.Name ?? "Unknown"));
        }

        return await Task.FromResult(Result<IEnumerable<SubscriptionResponse>>.Success(responses));
    }

    private static SubscriptionResponse MapToResponse(Subscription sub, string planName) => new()
    {
        Id = sub.Id,
        UserId = sub.UserId,
        PlanId = sub.PlanId,
        PlanName = planName,
        Status = sub.Status,
        StartDate = sub.StartDate,
        EndDate = sub.EndDate,
        CancelledAt = sub.CancelledAt,
        PausedAt = sub.PausedAt,
        AutoRenew = sub.AutoRenew,
        PaymentRetryCount = sub.PaymentRetryCount,
        GracePeriodEndDate = sub.GracePeriodEndDate,
        CreatedAt = sub.CreatedAt,
        UpdatedAt = sub.UpdatedAt
    };
}
