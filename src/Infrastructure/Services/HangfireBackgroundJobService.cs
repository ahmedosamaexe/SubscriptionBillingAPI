using Hangfire;
using SubscriptionBillingAPI.Application.Common.Interfaces;

namespace SubscriptionBillingAPI.Infrastructure.Services;

/// <summary>
/// Hangfire-backed implementation of IBackgroundJobService.
/// Enqueues fire-and-forget jobs via the Hangfire BackgroundJob API.
/// </summary>
public class HangfireBackgroundJobService : IBackgroundJobService
{
    public void EnqueueFailedPaymentRetry(string stripeSubscriptionId)
    {
        BackgroundJob.Enqueue<FailedPaymentRetryJob>(
            job => job.ExecuteAsync(stripeSubscriptionId, CancellationToken.None));
    }
}
