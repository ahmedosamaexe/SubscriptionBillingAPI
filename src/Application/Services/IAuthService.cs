using SubscriptionBillingAPI.Application.Common.Models;
using SubscriptionBillingAPI.Application.DTOs.Auth;

namespace SubscriptionBillingAPI.Application.Services;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
