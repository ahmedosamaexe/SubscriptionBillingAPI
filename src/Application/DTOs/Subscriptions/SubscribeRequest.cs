using System.ComponentModel.DataAnnotations;

namespace SubscriptionBillingAPI.Application.DTOs.Subscriptions;

public class SubscribeRequest
{
    [Required]
    public Guid PlanId { get; set; }
}
