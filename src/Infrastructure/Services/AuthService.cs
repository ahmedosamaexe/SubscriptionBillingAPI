using SubscriptionBillingAPI.Application.Common.Interfaces;
using SubscriptionBillingAPI.Application.Common.Models;
using SubscriptionBillingAPI.Application.DTOs.Auth;
using SubscriptionBillingAPI.Domain.Entities;
using SubscriptionBillingAPI.Domain.Enums;
using BCrypt.Net;
using SubscriptionBillingAPI.Application.Services;

namespace SubscriptionBillingAPI.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IAppDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        IAppDbContext context,
        IJwtTokenService jwtTokenService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var existingUser = _context.Users
            .FirstOrDefault(u => u.Email == normalizedEmail);

        if (existingUser is not null)
            return Result<AuthResponse>.Failure("A user with this email already exists.");

        var user = new User
        {
            Email = normalizedEmail,
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        _context.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        var token = _jwtTokenService.GenerateToken(user);

        return Result<AuthResponse>.Success(new AuthResponse
        {
            Token = token,
            Email = user.Email,
            Role = user.Role.ToString(),
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        });
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = _context.Users
            .FirstOrDefault(u => u.Email == normalizedEmail);

        if (user is null)
            return Result<AuthResponse>.Failure("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Result<AuthResponse>.Failure("Invalid email or password.");

        var token = _jwtTokenService.GenerateToken(user);

        return await Task.FromResult(Result<AuthResponse>.Success(new AuthResponse
        {
            Token = token,
            Email = user.Email,
            Role = user.Role.ToString(),
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        }));
    }
}
