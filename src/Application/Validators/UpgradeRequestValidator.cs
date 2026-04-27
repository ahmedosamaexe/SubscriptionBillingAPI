using FluentValidation;
using SubscriptionBillingAPI.Application.DTOs.Subscriptions;

namespace SubscriptionBillingAPI.Application.Validators;

public class UpgradeRequestValidator : AbstractValidator<UpgradeRequest>
{
    public UpgradeRequestValidator()
    {
        RuleFor(x => x.NewPlanId)
            .NotEmpty().WithMessage("NewPlanId is required.")
            .NotEqual(Guid.Empty).WithMessage("A valid NewPlanId must be provided.");
    }
}
