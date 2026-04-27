namespace SubscriptionBillingAPI.Application.Common.Interfaces;

/// <summary>
/// Abstraction for background job operations.
/// Implemented by Infrastructure (Hangfire), consumed by Application layer.
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>Enqueues a fire-and-forget failed payment retry job.</summary>
    void EnqueueFailedPaymentRetry(string stripeSubscriptionId);
}
