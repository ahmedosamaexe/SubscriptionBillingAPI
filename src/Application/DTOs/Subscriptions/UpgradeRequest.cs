using System.ComponentModel.DataAnnotations;

namespace SubscriptionBillingAPI.Application.DTOs.Subscriptions;

public class UpgradeRequest
{
    [Required]
    public Guid NewPlanId { get; set; }
}
