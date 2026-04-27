using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionBillingAPI.Domain.Entities;

namespace SubscriptionBillingAPI.Infrastructure.Persistence.Configurations;

public class UsageLogConfiguration : IEntityTypeConfiguration<UsageLog>
{
    public void Configure(EntityTypeBuilder<UsageLog> builder)
    {
        builder.HasKey(ul => ul.Id);

        builder.Property(ul => ul.Action)
            .IsRequired()
            .HasMaxLength(100);

        // Composite index for fast lookups: user + action + period
        builder.HasIndex(ul => new { ul.UserId, ul.Action, ul.Month, ul.Year });

        builder.HasOne(ul => ul.User)
            .WithMany(u => u.UsageLogs)
            .HasForeignKey(ul => ul.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ul => ul.Subscription)
            .WithMany(s => s.UsageLogs)
            .HasForeignKey(ul => ul.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
