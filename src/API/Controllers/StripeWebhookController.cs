using Microsoft.AspNetCore.Mvc;
using Stripe;
using SubscriptionBillingAPI.Application.Services;

namespace SubscriptionBillingAPI.API.Controllers;

[ApiController]
[Route("api/webhooks/stripe")]
public class StripeWebhookController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(
        IWebhookService webhookService,
        IConfiguration configuration,
        ILogger<StripeWebhookController> logger)
    {
        _webhookService = webhookService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>Stripe webhook endpoint. Verifies signatures and dispatches events.</summary>
    [HttpPost]
    public async Task<IActionResult> HandleWebhook(CancellationToken cancellationToken)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(cancellationToken);
        var webhookSecret = _configuration["StripeSettings:WebhookSecret"]
            ?? throw new InvalidOperationException("Stripe WebhookSecret is not configured.");

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                webhookSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Stripe webhook signature verification failed.");
            return BadRequest(new { Message = "Invalid Stripe signature." });
        }

        _logger.LogInformation("Received Stripe event: {EventType} (ID: {EventId})", stripeEvent.Type, stripeEvent.Id);

        switch (stripeEvent.Type)
        {
            case EventTypes.CheckoutSessionCompleted:
                await HandleCheckoutSessionCompleted(stripeEvent, cancellationToken);
                break;

            case EventTypes.InvoicePaid:
                await HandleInvoicePaid(stripeEvent, cancellationToken);
                break;

            case EventTypes.InvoicePaymentFailed:
                await HandleInvoicePaymentFailed(stripeEvent, cancellationToken);
                break;

            case EventTypes.CustomerSubscriptionDeleted:
                await HandleSubscriptionDeleted(stripeEvent, cancellationToken);
                break;

            default:
                _logger.LogInformation("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                break;
        }

        return Ok();
    }

    private async Task HandleCheckoutSessionCompleted(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not Stripe.Checkout.Session session)
        {
            _logger.LogWarning("checkout.session.completed: Could not deserialize session.");
            return;
        }

        var stripeSubscriptionId = session.SubscriptionId;
        var stripeCustomerId = session.CustomerId;

        if (string.IsNullOrEmpty(stripeSubscriptionId) || string.IsNullOrEmpty(stripeCustomerId))
        {
            _logger.LogWarning("checkout.session.completed: Missing subscription or customer ID.");
            return;
        }

        if (!session.Metadata.TryGetValue("userId", out var userIdStr) ||
            !Guid.TryParse(userIdStr, out var userId))
        {
            _logger.LogWarning("checkout.session.completed: Missing or invalid userId in metadata.");
            return;
        }

        if (!session.Metadata.TryGetValue("planId", out var planIdStr) ||
            !Guid.TryParse(planIdStr, out var planId))
        {
            _logger.LogWarning("checkout.session.completed: Missing or invalid planId in metadata.");
            return;
        }

        var result = await _webhookService.HandleCheckoutSessionCompletedAsync(
            stripeSubscriptionId, stripeCustomerId, userId, planId, cancellationToken);

        if (!result.IsSuccess)
            _logger.LogWarning("checkout.session.completed handler failed: {Error}", result.Error);
        else
            _logger.LogInformation("Subscription provisioned for User {UserId}, Plan {PlanId}.", userId, planId);
    }

    private async Task HandleInvoicePaid(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not Stripe.Invoice stripeInvoice)
        {
            _logger.LogWarning("invoice.paid: Could not deserialize invoice.");
            return;
        }

        // In Stripe.net v51+, subscription ID is accessed via Parent.SubscriptionDetails
        string? stripeSubscriptionId = null;
        if (stripeInvoice.Parent?.SubscriptionDetails?.Subscription is not null)
        {
            stripeSubscriptionId = stripeInvoice.Parent.SubscriptionDetails.Subscription.Id;
        }

        if (string.IsNullOrEmpty(stripeSubscriptionId))
        {
            _logger.LogInformation("invoice.paid: No subscription ID (one-time invoice). Skipping.");
            return;
        }

        // In v51+ AmountPaid and PaymentIntentId are accessed via InvoicePayments API.
        // For webhook processing, use the Total field from the invoice as the amount.
        var amountPaid = stripeInvoice.Total / 100m; // Stripe amounts are in cents

        var result = await _webhookService.HandleInvoicePaidAsync(
            stripeSubscriptionId,
            stripeInvoice.Id,
            null, // PaymentIntentId must be fetched separately via InvoicePayments API if needed
            amountPaid,
            cancellationToken);

        if (!result.IsSuccess)
            _logger.LogWarning("invoice.paid handler failed: {Error}", result.Error);
        else
            _logger.LogInformation("Invoice recorded for Stripe Subscription {SubscriptionId}.", stripeSubscriptionId);
    }

    private async Task HandleInvoicePaymentFailed(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not Stripe.Invoice stripeInvoice)
        {
            _logger.LogWarning("invoice.payment_failed: Could not deserialize invoice.");
            return;
        }

        // In Stripe.net v51+, subscription ID is accessed via Parent.SubscriptionDetails
        string? stripeSubscriptionId = null;
        if (stripeInvoice.Parent?.SubscriptionDetails?.Subscription is not null)
        {
            stripeSubscriptionId = stripeInvoice.Parent.SubscriptionDetails.Subscription.Id;
        }

        if (string.IsNullOrEmpty(stripeSubscriptionId))
        {
            _logger.LogInformation("invoice.payment_failed: No subscription ID. Skipping.");
            return;
        }

        var result = await _webhookService.HandleInvoicePaymentFailedAsync(
            stripeSubscriptionId, cancellationToken);

        if (!result.IsSuccess)
            _logger.LogWarning("invoice.payment_failed handler failed: {Error}", result.Error);
        else
            _logger.LogInformation("Payment failure handled for Stripe Subscription {SubscriptionId}.", stripeSubscriptionId);
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not Stripe.Subscription stripeSubscription)
        {
            _logger.LogWarning("customer.subscription.deleted: Could not deserialize subscription.");
            return;
        }

        var result = await _webhookService.HandleSubscriptionDeletedAsync(
            stripeSubscription.Id, cancellationToken);

        if (!result.IsSuccess)
            _logger.LogWarning("customer.subscription.deleted handler failed: {Error}", result.Error);
        else
            _logger.LogInformation("Subscription {SubscriptionId} expired.", stripeSubscription.Id);
    }
}
