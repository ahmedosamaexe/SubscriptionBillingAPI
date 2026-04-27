using SubscriptionBillingAPI.Application.Common.Models;
using SubscriptionBillingAPI.Application.DTOs.Subscriptions;

namespace SubscriptionBillingAPI.Application.Services;

public interface ISubscriptionService
{
    Task<Result<string>> SubscribeAsync(Guid userId, SubscribeRequest request, CancellationToken cancellationToken = default);
    Task<Result> CancelAsync(Guid userId, Guid subscriptionId, CancellationToken cancellationToken = default);
    Task<Result<string>> UpgradeAsync(Guid userId, Guid subscriptionId, UpgradeRequest request, CancellationToken cancellationToken = default);
    Task<Result> PauseAsync(Guid userId, Guid subscriptionId, CancellationToken cancellationToken = default);
    Task<Result<SubscriptionResponse>> GetByIdAsync(Guid userId, Guid subscriptionId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<SubscriptionResponse>>> GetUserSubscriptionsAsync(Guid userId, CancellationToken cancellationToken = default);
}
