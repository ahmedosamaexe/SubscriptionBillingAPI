using FluentValidation;
using SubscriptionBillingAPI.Application.DTOs.Usage;

namespace SubscriptionBillingAPI.Application.Validators;

public class TrackUsageRequestValidator : AbstractValidator<TrackUsageRequest>
{
    public TrackUsageRequestValidator()
    {
        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Action is required.")
            .MaximumLength(100).WithMessage("Action name cannot exceed 100 characters.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be at least 1.");
    }
}
