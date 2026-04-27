using System.ComponentModel.DataAnnotations;
using SubscriptionBillingAPI.Domain.Enums;

namespace SubscriptionBillingAPI.Application.DTOs.Plans;

public class CreatePlanRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;

    public string Features { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
