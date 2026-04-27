using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionBillingAPI.Domain.Entities;

namespace SubscriptionBillingAPI.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(s => s.StripeSubscriptionId)
            .HasMaxLength(256);

        builder.Property(s => s.StripeCustomerId)
            .HasMaxLength(256);

        builder.HasOne(s => s.User)
            .WithMany(u => u.Subscriptions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Plan)
            .WithMany(p => p.Subscriptions)
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Invoices)
            .WithOne(i => i.Subscription)
            .HasForeignKey(i => i.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.UsageLogs)
            .WithOne(ul => ul.Subscription)
            .HasForeignKey(ul => ul.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
