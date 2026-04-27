using FluentValidation;
using SubscriptionBillingAPI.Application.DTOs.Plans;

namespace SubscriptionBillingAPI.Application.Validators;

public class UpdatePlanRequestValidator : AbstractValidator<UpdatePlanRequest>
{
    public UpdatePlanRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(100).WithMessage("Plan name cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price must be greater than or equal to 0.")
            .When(x => x.Price.HasValue);
    }
}
