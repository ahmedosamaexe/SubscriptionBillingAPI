using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;
using SubscriptionBillingAPI.Application.Common.Interfaces;

namespace SubscriptionBillingAPI.Infrastructure.Services;

public class StripePaymentService : IStripePaymentService
{
    private readonly IConfiguration _configuration;

    public StripePaymentService(IConfiguration configuration)
    {
        _configuration = configuration;
        StripeConfiguration.ApiKey = _configuration["StripeSettings:SecretKey"];
    }

    public async Task<string> CreateCheckoutSessionAsync(
        Guid userId,
        string userEmail,
        Guid planId,
        string stripePriceId,
        string? existingStripeCustomerId,
        CancellationToken cancellationToken = default)
    {
        var sessionService = new SessionService();

        var options = new SessionCreateOptions
        {
            Mode = "subscription",
            SuccessUrl = _configuration["StripeSettings:SuccessUrl"] ?? "https://localhost:7001/success?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = _configuration["StripeSettings:CancelUrl"] ?? "https://localhost:7001/cancel",
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Price = stripePriceId,
                    Quantity = 1
                }
            ],
            Metadata = new Dictionary<string, string>
            {
                { "userId", userId.ToString() },
                { "planId", planId.ToString() }
            }
        };

        if (!string.IsNullOrEmpty(existingStripeCustomerId))
        {
            options.Customer = existingStripeCustomerId;
        }
        else
        {
            options.CustomerEmail = userEmail;
        }

        var session = await sessionService.CreateAsync(options, cancellationToken: cancellationToken);
        return session.Url;
    }

    public async Task<string> CreateUpgradeCheckoutSessionAsync(
        Guid userId,
        string userEmail,
        string stripeSubscriptionId,
        string newStripePriceId,
        string stripeCustomerId,
        CancellationToken cancellationToken = default)
    {
        // For upgrades, we create a new checkout session with the new price
        // and pass the existing subscription info in metadata so the webhook can handle it
        var sessionService = new SessionService();

        var options = new SessionCreateOptions
        {
            Mode = "subscription",
            Customer = stripeCustomerId,
            SuccessUrl = _configuration["StripeSettings:SuccessUrl"] ?? "https://localhost:7001/success?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = _configuration["StripeSettings:CancelUrl"] ?? "https://localhost:7001/cancel",
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Price = newStripePriceId,
                    Quantity = 1
                }
            ],
            Metadata = new Dictionary<string, string>
            {
                { "userId", userId.ToString() },
                { "upgradeFromSubscription", stripeSubscriptionId }
            }
        };

        var session = await sessionService.CreateAsync(options, cancellationToken: cancellationToken);
        return session.Url;
    }

    public async Task CancelSubscriptionAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default)
    {
        var subscriptionService = new Stripe.SubscriptionService();
        await subscriptionService.UpdateAsync(stripeSubscriptionId, new SubscriptionUpdateOptions
        {
            CancelAtPeriodEnd = true
        }, cancellationToken: cancellationToken);
    }

    public async Task PauseSubscriptionAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default)
    {
        var subscriptionService = new Stripe.SubscriptionService();
        await subscriptionService.UpdateAsync(stripeSubscriptionId, new SubscriptionUpdateOptions
        {
            PauseCollection = new SubscriptionPauseCollectionOptions
            {
                Behavior = "void"
            }
        }, cancellationToken: cancellationToken);
    }

    public async Task ResumeSubscriptionAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default)
    {
        var subscriptionService = new Stripe.SubscriptionService();
        await subscriptionService.UpdateAsync(stripeSubscriptionId, new SubscriptionUpdateOptions
        {
            PauseCollection = new SubscriptionPauseCollectionOptions() // passing empty resets / clears pause
        }, cancellationToken: cancellationToken);
    }
}
