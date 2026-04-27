using SubscriptionBillingAPI.Domain.Enums;

namespace SubscriptionBillingAPI.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;

    /// <summary>Stripe Customer ID for this user.</summary>
    public string? StripeCustomerId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<UsageLog> UsageLogs { get; set; } = new List<UsageLog>();
}
