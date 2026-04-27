using System.ComponentModel.DataAnnotations;
using SubscriptionBillingAPI.Domain.Enums;

namespace SubscriptionBillingAPI.Application.DTOs.Plans;

public class UpdatePlanRequest
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Price { get; set; }

    public BillingCycle? BillingCycle { get; set; }

    public string? Features { get; set; }

    public bool? IsActive { get; set; }
}
