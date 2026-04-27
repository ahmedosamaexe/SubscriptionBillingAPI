using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SubscriptionBillingAPI.Application.Common.Interfaces;
using SubscriptionBillingAPI.Domain.Enums;

namespace SubscriptionBillingAPI.API.Filters;

/// <summary>
/// Action filter that checks the user's subscription status before allowing access.
/// Blocks access if the user has no active subscription, or if their subscription
/// is Suspended or Expired. Allows access during GracePeriod.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SubscriptionAccessFilter : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        if (user.Identity is not { IsAuthenticated: true })
        {
            context.Result = new UnauthorizedObjectResult(new { Message = "Authentication required." });
            return;
        }

        var userIdClaim = user.FindFirst("userId")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            context.Result = new UnauthorizedObjectResult(new { Message = "Invalid user token." });
            return;
        }

        var dbContext = context.HttpContext.RequestServices.GetRequiredService<IAppDbContext>();

        var activeSubscription = dbContext.Subscriptions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefault();

        if (activeSubscription is null)
        {
            context.Result = new ObjectResult(new
            {
                Message = "No subscription found. Please subscribe to access this resource."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        if (activeSubscription.Status is SubscriptionStatus.Suspended or SubscriptionStatus.Expired)
        {
            context.Result = new ObjectResult(new
            {
                Message = $"Your subscription is {activeSubscription.Status}. Please renew to regain access."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        if (activeSubscription.Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Paused)
        {
            context.Result = new ObjectResult(new
            {
                Message = $"Your subscription is {activeSubscription.Status}. Please reactivate to access this resource."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        // Active or GracePeriod — allow access
        await next();
    }
}
