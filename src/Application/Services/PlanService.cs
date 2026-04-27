using SubscriptionBillingAPI.Application.Common.Interfaces;
using SubscriptionBillingAPI.Application.Common.Models;
using SubscriptionBillingAPI.Application.DTOs.Plans;
using SubscriptionBillingAPI.Domain.Entities;

namespace SubscriptionBillingAPI.Application.Services;

public class PlanService : IPlanService
{
    private readonly IAppDbContext _context;

    public PlanService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IEnumerable<PlanResponse>>> GetAllPlansAsync(CancellationToken cancellationToken = default)
    {
        var plans = _context.Plans
            .OrderBy(p => p.Price)
            .Select(p => MapToResponse(p))
            .ToList();

        return await Task.FromResult(Result<IEnumerable<PlanResponse>>.Success(plans));
    }

    public async Task<Result<PlanResponse>> GetPlanByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var plan = await _context.FindAsync<Plan>(id, cancellationToken);

        if (plan is null)
            return Result<PlanResponse>.Failure($"Plan with ID '{id}' was not found.");

        return Result<PlanResponse>.Success(MapToResponse(plan));
    }

    public async Task<Result<PlanResponse>> CreatePlanAsync(CreatePlanRequest request, CancellationToken cancellationToken = default)
    {
        var existingPlan = _context.Plans
            .FirstOrDefault(p => p.Name == request.Name);

        if (existingPlan is not null)
            return Result<PlanResponse>.Failure($"A plan named '{request.Name}' already exists.");

        var plan = new Plan
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            BillingCycle = request.BillingCycle,
            Features = request.Features,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Add(plan);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<PlanResponse>.Success(MapToResponse(plan));
    }

    public async Task<Result<PlanResponse>> UpdatePlanAsync(Guid id, UpdatePlanRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await _context.FindAsync<Plan>(id, cancellationToken);

        if (plan is null)
            return Result<PlanResponse>.Failure($"Plan with ID '{id}' was not found.");

        if (request.Name is not null) plan.Name = request.Name;
        if (request.Description is not null) plan.Description = request.Description;
        if (request.Price.HasValue) plan.Price = request.Price.Value;
        if (request.BillingCycle.HasValue) plan.BillingCycle = request.BillingCycle.Value;
        if (request.Features is not null) plan.Features = request.Features;
        if (request.IsActive.HasValue) plan.IsActive = request.IsActive.Value;
        plan.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<PlanResponse>.Success(MapToResponse(plan));
    }

    public async Task<Result> DeletePlanAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var plan = await _context.FindAsync<Plan>(id, cancellationToken);

        if (plan is null)
            return Result.Failure($"Plan with ID '{id}' was not found.");

        _context.Remove(plan);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static PlanResponse MapToResponse(Plan plan) => new()
    {
        Id = plan.Id,
        Name = plan.Name,
        Description = plan.Description,
        Price = plan.Price,
        BillingCycle = plan.BillingCycle,
        Features = plan.Features,
        IsActive = plan.IsActive,
        CreatedAt = plan.CreatedAt,
        UpdatedAt = plan.UpdatedAt
    };
}
