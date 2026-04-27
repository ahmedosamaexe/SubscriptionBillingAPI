using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SubscriptionBillingAPI.Application.Services;

namespace SubscriptionBillingAPI.API.Filters;

/// <summary>
/// Action filter that enforces usage quotas before allowing access to the action.
/// Checks if the user has exceeded their Plan.MaxRequests for the current month.
/// Returns 429 Too Many Requests if exceeded.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class QuotaEnforcementFilter : Attribute, IAsyncActionFilter
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

        var usageService = context.HttpContext.RequestServices.GetRequiredService<IUsageService>();

        var isExceeded = await usageService.IsQuotaExceededAsync(userId, context.HttpContext.RequestAborted);

        if (isExceeded)
        {
            context.Result = new ObjectResult(new
            {
                Message = "Monthly usage quota exceeded. Please upgrade your plan for additional requests."
            })
            {
                StatusCode = StatusCodes.Status429TooManyRequests
            };
            return;
        }

        await next();
    }
}
