namespace SubscriptionBillingAPI.Domain.Entities;

public class UsageLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid SubscriptionId { get; set; }

    /// <summary>The action being tracked, e.g. "api_calls", "ProjectCreated", "storage_gb".</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Accumulated count for this action within the tracking period.</summary>
    public long Count { get; set; }

    /// <summary>Month component of the tracking period (1-12).</summary>
    public int Month { get; set; }

    /// <summary>Year component of the tracking period.</summary>
    public int Year { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public Subscription Subscription { get; set; } = null!;
}
