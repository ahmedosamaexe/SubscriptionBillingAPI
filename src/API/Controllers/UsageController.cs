using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionBillingAPI.Application.DTOs.Usage;
using SubscriptionBillingAPI.Application.Services;

namespace SubscriptionBillingAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class UsageController : ControllerBase
{
    private readonly IUsageService _usageService;

    public UsageController(IUsageService usageService)
    {
        _usageService = usageService;
    }

    /// <summary>Returns current user's usage stats vs plan limits for the current month.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(IEnumerable<UsageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyUsage(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _usageService.GetCurrentUsageAsync(userId, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(new { Message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>Explicitly track a specific usage event (e.g. "ProjectCreated").</summary>
    [HttpPost("track")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TrackUsage([FromBody] TrackUsageRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        var result = await _usageService.IncrementUsageAsync(userId, request.Action, request.Quantity, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { Message = result.Error });

        return Ok(new { Message = $"Usage tracked: {request.Action} (+{request.Quantity})" });
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
