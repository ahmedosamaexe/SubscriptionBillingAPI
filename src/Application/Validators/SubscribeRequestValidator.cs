using FluentValidation;
using SubscriptionBillingAPI.Application.DTOs.Subscriptions;

namespace SubscriptionBillingAPI.Application.Validators;

public class SubscribeRequestValidator : AbstractValidator<SubscribeRequest>
{
    public SubscribeRequestValidator()
    {
        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("PlanId is required.")
            .NotEqual(Guid.Empty).WithMessage("A valid PlanId must be provided.");
    }
}
