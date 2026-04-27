namespace SubscriptionBillingAPI.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the Stripe payment provider. 
/// Implemented in Infrastructure, consumed by Application.
/// </summary>
public interface IStripePaymentService
{
    /// <summary>Creates a Stripe Checkout Session and returns the session URL.</summary>
    Task<string> CreateCheckoutSessionAsync(
        Guid userId,
        string userEmail,
        Guid planId,
        string stripePriceId,
        string? existingStripeCustomerId,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a Stripe Checkout Session for upgrading an existing subscription.</summary>
    Task<string> CreateUpgradeCheckoutSessionAsync(
        Guid userId,
        string userEmail,
        string stripeSubscriptionId,
        string newStripePriceId,
        string stripeCustomerId,
        CancellationToken cancellationToken = default);

    /// <summary>Cancels a Stripe subscription at period end.</summary>
    Task CancelSubscriptionAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default);

    /// <summary>Pauses a Stripe subscription (pause collection).</summary>
    Task PauseSubscriptionAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default);

    /// <summary>Resumes a paused Stripe subscription.</summary>
    Task ResumeSubscriptionAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default);
}
