using SubscriptionBillingAPI.Application.Common.Models;

namespace SubscriptionBillingAPI.Application.Services;

public interface IWebhookService
{
    Task<Result> HandleCheckoutSessionCompletedAsync(
        string stripeSubscriptionId,
        string stripeCustomerId,
        Guid userId,
        Guid planId,
        CancellationToken cancellationToken = default);

    Task<Result> HandleInvoicePaidAsync(
        string stripeSubscriptionId,
        string stripeInvoiceId,
        string? stripePaymentIntentId,
        decimal amountPaid,
        CancellationToken cancellationToken = default);

    Task<Result> HandleInvoicePaymentFailedAsync(
        string stripeSubscriptionId,
        CancellationToken cancellationToken = default);

    Task<Result> HandleSubscriptionDeletedAsync(
        string stripeSubscriptionId,
        CancellationToken cancellationToken = default);
}
