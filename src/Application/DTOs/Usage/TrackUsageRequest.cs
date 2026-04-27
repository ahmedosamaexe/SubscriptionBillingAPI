using System.ComponentModel.DataAnnotations;

namespace SubscriptionBillingAPI.Application.DTOs.Usage;

public class TrackUsageRequest
{
    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    /// <summary>How many units to add. Defaults to 1 if omitted.</summary>
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;
}
