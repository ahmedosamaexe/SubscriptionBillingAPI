using SubscriptionBillingAPI.Domain.Entities;

namespace SubscriptionBillingAPI.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}
