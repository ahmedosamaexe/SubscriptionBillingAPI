using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionBillingAPI.API.Services;
using SubscriptionBillingAPI.Application.Common.Interfaces;
using SubscriptionBillingAPI.Domain.Entities;
using SubscriptionBillingAPI.Domain.Enums;
using SubscriptionBillingAPI.Infrastructure.Persistence;
using SubscriptionBillingAPI.Infrastructure.Persistence.Configurations;
using SubscriptionBillingAPI.IntegrationTests.Infrastructure;
using Xunit;

namespace SubscriptionBillingAPI.IntegrationTests.API;

public class SubscriptionsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SubscriptionsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMySubscriptions_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange - No token provided
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/subscriptions");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMySubscriptions_WithExceededQuota_ShouldReturnTooManyRequests()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = $"quota_{Guid.NewGuid()}@test.com",
            PasswordHash = "hash"
        };

        // Create an active subscription on the Free Plan (100 MaxRequests)
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = PlanConfiguration.FreePlanId,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow
        };

        // Create a usage log that exceeds the 100 limit
        var usageLog = new UsageLog
        {
            UserId = userId,
            SubscriptionId = subscription.Id,
            Action = "api_calls",
            Count = 101, // > 100
            Month = DateTime.UtcNow.Month,
            Year = DateTime.UtcNow.Year
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(user);
            db.Subscriptions.Add(subscription);
            db.UsageLogs.Add(usageLog);
            await db.SaveChangesAsync();
        }

        // Generate valid JWT token
        using (var scope = _factory.Services.CreateScope())
        {
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var token = jwtService.GenerateToken(user);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/subscriptions");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Monthly usage quota exceeded");
    }
}
