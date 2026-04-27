using SubscriptionBillingAPI.Application.Common.Interfaces;
using SubscriptionBillingAPI.Application.Common.Models;
using SubscriptionBillingAPI.Domain.Entities;
using SubscriptionBillingAPI.Domain.Enums;

namespace SubscriptionBillingAPI.Application.Services;

public class WebhookService : IWebhookService
{
    private readonly IAppDbContext _context;
    private readonly IBackgroundJobService _backgroundJobService;

    public WebhookService(IAppDbContext context, IBackgroundJobService backgroundJobService)
    {
        _context = context;
        _backgroundJobService = backgroundJobService;
    }

    public async Task<Result> HandleCheckoutSessionCompletedAsync(
        string stripeSubscriptionId,
        string stripeCustomerId,
        Guid userId,
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.FindAsync<User>(userId, cancellationToken);
        if (user is null)
            return Result.Failure("User not found.");

        // Save Stripe Customer ID on the user
        user.StripeCustomerId = stripeCustomerId;
        user.UpdatedAt = DateTime.UtcNow;

        var plan = await _context.FindAsync<Plan>(planId, cancellationToken);
        if (plan is null)
            return Result.Failure("Plan not found.");

        // Create and activate the subscription
        var subscription = new Subscription
        {
            UserId = userId,
            PlanId = planId,
            StartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        subscription.Activate(stripeSubscriptionId, stripeCustomerId);

        _context.Add(subscription);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> HandleInvoicePaidAsync(
        string stripeSubscriptionId,
        string stripeInvoiceId,
        string? stripePaymentIntentId,
        decimal amountPaid,
        CancellationToken cancellationToken = default)
    {
        var subscription = _context.Subscriptions
            .FirstOrDefault(s => s.StripeSubscriptionId == stripeSubscriptionId);

        if (subscription is null)
            return Result.Failure($"Subscription not found for Stripe ID: {stripeSubscriptionId}");

        // If in GracePeriod, reset to Active
        if (subscription.Status == SubscriptionStatus.GracePeriod)
        {
            subscription.ResetToActive();
        }

        // Create the Invoice record
        var invoice = new Invoice
        {
            UserId = subscription.UserId,
            SubscriptionId = subscription.Id,
            Amount = amountPaid,
            Status = InvoiceStatus.Paid,
            DueDate = DateTime.UtcNow,
            PaidAt = DateTime.UtcNow,
            StripeInvoiceId = stripeInvoiceId,
            StripePaymentIntentId = stripePaymentIntentId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> HandleInvoicePaymentFailedAsync(
        string stripeSubscriptionId,
        CancellationToken cancellationToken = default)
    {
        var subscription = _context.Subscriptions
            .FirstOrDefault(s => s.StripeSubscriptionId == stripeSubscriptionId);

        if (subscription is null)
            return Result.Failure($"Subscription not found for Stripe ID: {stripeSubscriptionId}");

        // Domain logic handles the state transition (GracePeriod or Suspended)
        subscription.HandlePaymentFailed();
        await _context.SaveChangesAsync(cancellationToken);

        // Enqueue a fire-and-forget background job for failed payment retry
        _backgroundJobService.EnqueueFailedPaymentRetry(stripeSubscriptionId);

        return Result.Success();
    }

    public async Task<Result> HandleSubscriptionDeletedAsync(
        string stripeSubscriptionId,
        CancellationToken cancellationToken = default)
    {
        var subscription = _context.Subscriptions
            .FirstOrDefault(s => s.StripeSubscriptionId == stripeSubscriptionId);

        if (subscription is null)
            return Result.Failure($"Subscription not found for Stripe ID: {stripeSubscriptionId}");

        subscription.Expire();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
