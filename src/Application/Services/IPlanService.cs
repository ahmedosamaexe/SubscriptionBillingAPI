using SubscriptionBillingAPI.Application.Common.Models;
using SubscriptionBillingAPI.Application.DTOs.Plans;

namespace SubscriptionBillingAPI.Application.Services;

public interface IPlanService
{
    Task<Result<IEnumerable<PlanResponse>>> GetAllPlansAsync(CancellationToken cancellationToken = default);
    Task<Result<PlanResponse>> GetPlanByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PlanResponse>> CreatePlanAsync(CreatePlanRequest request, CancellationToken cancellationToken = default);
    Task<Result<PlanResponse>> UpdatePlanAsync(Guid id, UpdatePlanRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeletePlanAsync(Guid id, CancellationToken cancellationToken = default);
}
