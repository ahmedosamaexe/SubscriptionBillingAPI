using SubscriptionBillingAPI.Domain.Enums;

namespace SubscriptionBillingAPI.Domain.Entities;

public class Plan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;
    public string Features { get; set; } = string.Empty; // JSON or comma-separated
    public bool IsActive { get; set; } = true;

    /// <summary>The Stripe Price ID used for checkout sessions.</summary>
    public string? StripePriceId { get; set; }

    /// <summary>Maximum API requests allowed per month. 0 = unlimited.</summary>
    public int MaxRequests { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
