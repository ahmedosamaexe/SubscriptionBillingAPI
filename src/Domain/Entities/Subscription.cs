using SubscriptionBillingAPI.Domain.Enums;

namespace SubscriptionBillingAPI.Domain.Entities;

public class Subscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? PausedAt { get; set; }
    public bool AutoRenew { get; set; } = true;
    public string? StripeSubscriptionId { get; set; }
    public string? StripeCustomerId { get; set; }

    /// <summary>Number of consecutive failed payment attempts.</summary>
    public int PaymentRetryCount { get; set; }

    /// <summary>When the grace period expires and status moves to Suspended.</summary>
    public DateTime? GracePeriodEndDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public Plan Plan { get; set; } = null!;
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<UsageLog> UsageLogs { get; set; } = new List<UsageLog>();

    // ──────────────────────────────────────────────────────────
    // Domain State Machine
    // ──────────────────────────────────────────────────────────

    private static readonly int GracePeriodDays = 7;
    private static readonly int MaxRetryCount = 3;

    /// <summary>
    /// Activates the subscription (e.g. after checkout completes).
    /// </summary>
    public void Activate(string stripeSubscriptionId, string stripeCustomerId)
    {
        Status = SubscriptionStatus.Active;
        StripeSubscriptionId = stripeSubscriptionId;
        StripeCustomerId = stripeCustomerId;
        PaymentRetryCount = 0;
        GracePeriodEndDate = null;
        PausedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Handles a failed payment — transitions to GracePeriod or Suspended.
    /// </summary>
    public void HandlePaymentFailed()
    {
        PaymentRetryCount++;
        UpdatedAt = DateTime.UtcNow;

        if (PaymentRetryCount >= MaxRetryCount)
        {
            Status = SubscriptionStatus.Suspended;
            GracePeriodEndDate = null;
        }
        else if (Status == SubscriptionStatus.Active)
        {
            Status = SubscriptionStatus.GracePeriod;
            GracePeriodEndDate = DateTime.UtcNow.AddDays(GracePeriodDays);
        }
        // If already in GracePeriod, keep the existing end date
    }

    /// <summary>
    /// Resets back to Active when a payment succeeds during grace period.
    /// </summary>
    public void ResetToActive()
    {
        Status = SubscriptionStatus.Active;
        PaymentRetryCount = 0;
        GracePeriodEndDate = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the subscription as cancelled.
    /// </summary>
    public void Cancel()
    {
        Status = SubscriptionStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        AutoRenew = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the subscription as expired (e.g. Stripe deleted it).
    /// </summary>
    public void Expire()
    {
        Status = SubscriptionStatus.Expired;
        EndDate = DateTime.UtcNow;
        AutoRenew = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Pauses the subscription.
    /// </summary>
    public void Pause()
    {
        if (Status != SubscriptionStatus.Active)
            throw new InvalidOperationException("Only active subscriptions can be paused.");

        Status = SubscriptionStatus.Paused;
        PausedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Resumes a paused subscription.
    /// </summary>
    public void Resume()
    {
        if (Status != SubscriptionStatus.Paused)
            throw new InvalidOperationException("Only paused subscriptions can be resumed.");

        Status = SubscriptionStatus.Active;
        PausedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks whether the subscription allows access to premium features.
    /// Active and GracePeriod allow access; Suspended, Expired, Cancelled, Paused do not.
    /// </summary>
    public bool AllowsAccess()
    {
        return Status is SubscriptionStatus.Active or SubscriptionStatus.GracePeriod;
    }
}
