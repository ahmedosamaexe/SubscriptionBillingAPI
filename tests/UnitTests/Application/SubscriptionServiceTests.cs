using FluentAssertions;
using Moq;
using SubscriptionBillingAPI.Application.Common.Interfaces;
using SubscriptionBillingAPI.Application.DTOs.Subscriptions;
using SubscriptionBillingAPI.Application.Services;
using SubscriptionBillingAPI.Domain.Entities;
using SubscriptionBillingAPI.Domain.Enums;
using Xunit;

namespace SubscriptionBillingAPI.UnitTests.Application;

public class SubscriptionServiceTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<IStripePaymentService> _mockStripeService;
    private readonly SubscriptionService _service;

    public SubscriptionServiceTests()
    {
        _mockContext = new Mock<IAppDbContext>();
        _mockStripeService = new Mock<IStripePaymentService>();
        _service = new SubscriptionService(_mockContext.Object, _mockStripeService.Object);
    }

    [Fact]
    public async Task SubscribeAsync_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockContext.Setup(c => c.FindAsync<User>(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.SubscribeAsync(userId, new SubscribeRequest { PlanId = Guid.NewGuid() });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not found.");
    }

    [Fact]
    public async Task SubscribeAsync_WhenPlanNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        _mockContext.Setup(c => c.FindAsync<User>(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User());
        _mockContext.Setup(c => c.FindAsync<Plan>(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Plan?)null);

        // Act
        var result = await _service.SubscribeAsync(userId, new SubscribeRequest { PlanId = planId });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Plan not found.");
    }

    [Fact]
    public async Task SubscribeAsync_WhenUserHasActiveSubscription_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var plan = new Plan { Id = Guid.NewGuid(), IsActive = true, StripePriceId = "price_123" };
        var activeSub = new Subscription { UserId = userId, Status = SubscriptionStatus.Active };

        _mockContext.Setup(c => c.FindAsync<User>(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User());
        _mockContext.Setup(c => c.FindAsync<Plan>(plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var subscriptions = new List<Subscription> { activeSub }.AsQueryable();
        _mockContext.Setup(c => c.Subscriptions).Returns(subscriptions);

        // Act
        var result = await _service.SubscribeAsync(userId, new SubscribeRequest { PlanId = plan.Id });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("You already have an active subscription. Please cancel or upgrade instead.");
    }

    [Fact]
    public async Task SubscribeAsync_WhenValid_ShouldReturnCheckoutUrl()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var plan = new Plan { Id = Guid.NewGuid(), IsActive = true, StripePriceId = "price_123" };
        var user = new User { Id = userId, Email = "test@test.com", StripeCustomerId = "cus_123" };
        var checkoutUrl = "https://checkout.stripe.com/123";

        _mockContext.Setup(c => c.FindAsync<User>(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockContext.Setup(c => c.FindAsync<Plan>(plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _mockContext.Setup(c => c.Subscriptions).Returns(new List<Subscription>().AsQueryable());

        _mockStripeService.Setup(s => s.CreateCheckoutSessionAsync(
                userId, user.Email, plan.Id, plan.StripePriceId, user.StripeCustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checkoutUrl);

        // Act
        var result = await _service.SubscribeAsync(userId, new SubscribeRequest { PlanId = plan.Id });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(checkoutUrl);
    }
}
