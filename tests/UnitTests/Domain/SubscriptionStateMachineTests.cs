using FluentAssertions;
using SubscriptionBillingAPI.Domain.Entities;
using SubscriptionBillingAPI.Domain.Enums;
using Xunit;

namespace SubscriptionBillingAPI.UnitTests.Domain;

public class SubscriptionStateMachineTests
{
    [Fact]
    public void Activate_ShouldSetStatusToActiveAndClearProperties()
    {
        // Arrange
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Paused,
            PaymentRetryCount = 2,
            GracePeriodEndDate = DateTime.UtcNow.AddDays(1),
            PausedAt = DateTime.UtcNow
        };
        var stripeSubId = "sub_123";
        var stripeCustId = "cus_123";

        // Act
        subscription.Activate(stripeSubId, stripeCustId);

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.StripeSubscriptionId.Should().Be(stripeSubId);
        subscription.StripeCustomerId.Should().Be(stripeCustId);
        subscription.PaymentRetryCount.Should().Be(0);
        subscription.GracePeriodEndDate.Should().BeNull();
        subscription.PausedAt.Should().BeNull();
    }

    [Fact]
    public void HandlePaymentFailed_FromActive_ShouldTransitionToGracePeriod()
    {
        // Arrange
        var subscription = new Subscription { Status = SubscriptionStatus.Active };

        // Act
        subscription.HandlePaymentFailed();

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.GracePeriod);
        subscription.PaymentRetryCount.Should().Be(1);
        subscription.GracePeriodEndDate.Should().NotBeNull();
        subscription.GracePeriodEndDate.Value.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void HandlePaymentFailed_WhenRetryExceedsLimit_ShouldTransitionToSuspended()
    {
        // Arrange
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.GracePeriod,
            PaymentRetryCount = 2,
            GracePeriodEndDate = DateTime.UtcNow.AddDays(7)
        };

        // Act
        subscription.HandlePaymentFailed(); // 3rd failure

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Suspended);
        subscription.PaymentRetryCount.Should().Be(3);
        subscription.GracePeriodEndDate.Should().BeNull();
    }

    [Fact]
    public void Pause_WhenActive_ShouldTransitionToPaused()
    {
        // Arrange
        var subscription = new Subscription { Status = SubscriptionStatus.Active };

        // Act
        subscription.Pause();

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Paused);
        subscription.PausedAt.Should().NotBeNull();
    }

    [Fact]
    public void Pause_WhenNotActive_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var subscription = new Subscription { Status = SubscriptionStatus.Suspended };

        // Act
        var action = () => subscription.Pause();

        // Assert
        action.Should().Throw<InvalidOperationException>()
              .WithMessage("Only active subscriptions can be paused.");
    }
}
