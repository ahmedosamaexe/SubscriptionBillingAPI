using Microsoft.Extensions.Logging;
using Stripe;
using SubscriptionBillingAPI.Application.Common.Interfaces;
using SubscriptionBillingAPI.Domain.Enums;

namespace SubscriptionBillingAPI.Infrastructure.Services;

/// <summary>
/// Fire-and-forget Hangfire job.
/// Enqueued by WebhookService when invoice.payment_failed is received.
/// Attempts to retry the payment via Stripe's invoice API.
/// </summary>
public class FailedPaymentRetryJob
{
    private readonly IAppDbContext _context;
    private readonly ILogger<FailedPaymentRetryJob> _logger;

    public FailedPaymentRetryJob(IAppDbContext context, ILogger<FailedPaymentRetryJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync(string stripeSubscriptionId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("FailedPaymentRetry: Processing retry for Stripe Subscription {SubId}.",
            stripeSubscriptionId);

        var subscription = _context.Subscriptions
            .FirstOrDefault(s => s.StripeSubscriptionId == stripeSubscriptionId);

        if (subscription is null)
        {
            _logger.LogWarning("FailedPaymentRetry: Subscription not found for {SubId}.", stripeSubscriptionId);
            return;
        }

        // If already suspended or expired, no point retrying
        if (subscription.Status is SubscriptionStatus.Suspended or SubscriptionStatus.Expired)
        {
            _logger.LogInformation("FailedPaymentRetry: Subscription {SubId} is {Status}. Skipping retry.",
                stripeSubscriptionId, subscription.Status);
            return;
        }

        try
        {
            // Retrieve the latest open invoice for this subscription and attempt to pay it
            var invoiceService = new InvoiceService();
            var invoices = await invoiceService.ListAsync(new InvoiceListOptions
            {
                Subscription = stripeSubscriptionId,
                Status = "open",
                Limit = 1
            }, cancellationToken: cancellationToken);

            var openInvoice = invoices.FirstOrDefault();
            if (openInvoice is not null)
            {
                await invoiceService.PayAsync(openInvoice.Id, cancellationToken: cancellationToken);
                _logger.LogInformation("FailedPaymentRetry: Successfully retried payment for invoice {InvoiceId}.",
                    openInvoice.Id);
            }
            else
            {
                _logger.LogInformation("FailedPaymentRetry: No open invoice found for subscription {SubId}.",
                    stripeSubscriptionId);
            }
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "FailedPaymentRetry: Stripe error during retry for {SubId}.",
                stripeSubscriptionId);
        }
    }
}
