using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionBillingAPI.Application.DTOs.Subscriptions;
using SubscriptionBillingAPI.Application.Services;

namespace SubscriptionBillingAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    /// <summary>Start a new subscription by creating a Stripe Checkout Session.</summary>
    [HttpPost("subscribe")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        var result = await _subscriptionService.SubscribeAsync(userId, request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { Message = result.Error });

        return Ok(new { CheckoutUrl = result.Data });
    }

    /// <summary>Cancel a subscription at the end of the current billing period.</summary>
    [HttpPost("{subscriptionId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid subscriptionId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _subscriptionService.CancelAsync(userId, subscriptionId, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { Message = result.Error });

        return Ok(new { Message = "Subscription has been cancelled." });
    }

    /// <summary>Upgrade a subscription to a new plan via a new Stripe Checkout Session.</summary>
    [HttpPost("{subscriptionId:guid}/upgrade")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Upgrade(Guid subscriptionId, [FromBody] UpgradeRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        var result = await _subscriptionService.UpgradeAsync(userId, subscriptionId, request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { Message = result.Error });

        return Ok(new { CheckoutUrl = result.Data });
    }

    /// <summary>Pause an active subscription.</summary>
    [HttpPost("{subscriptionId:guid}/pause")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Pause(Guid subscriptionId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _subscriptionService.PauseAsync(userId, subscriptionId, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { Message = result.Error });

        return Ok(new { Message = "Subscription has been paused." });
    }

    /// <summary>Get a specific subscription by ID.</summary>
    [HttpGet("{subscriptionId:guid}")]
    [ProducesResponseType(typeof(SubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid subscriptionId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _subscriptionService.GetByIdAsync(userId, subscriptionId, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(new { Message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>Get all subscriptions for the current user.</summary>
    [HttpGet]
    [SubscriptionBillingAPI.API.Filters.QuotaEnforcementFilter]
    [ProducesResponseType(typeof(IEnumerable<SubscriptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetMySubscriptions(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _subscriptionService.GetUserSubscriptionsAsync(userId, cancellationToken);

        return Ok(result.Data);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user token.");

        return userId;
    }
}
