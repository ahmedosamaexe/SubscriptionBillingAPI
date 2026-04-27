using SubscriptionBillingAPI.Domain.Enums;

namespace SubscriptionBillingAPI.Application.DTOs.Subscriptions;

public class SubscriptionResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public SubscriptionStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? PausedAt { get; set; }
    public bool AutoRenew { get; set; }
    public int PaymentRetryCount { get; set; }
    public DateTime? GracePeriodEndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
