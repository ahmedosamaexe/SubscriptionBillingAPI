namespace SubscriptionBillingAPI.Application.DTOs.Usage;

public class UsageResponse
{
    public string Action { get; set; } = string.Empty;
    public long CurrentCount { get; set; }
    public int MaxAllowed { get; set; }
    public bool IsUnlimited { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public double UsagePercentage { get; set; }
}
