using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionBillingAPI.Domain.Entities;
using SubscriptionBillingAPI.Domain.Enums;

namespace SubscriptionBillingAPI.Infrastructure.Persistence.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    // Well-known GUIDs for seeding
    public static readonly Guid FreePlanId  = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ProPlanId   = new("22222222-2222-2222-2222-222222222222");
    public static readonly Guid EntPlanId   = new("33333333-3333-3333-3333-333333333333");

    private static readonly DateTime SeedDate = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(p => p.Name)
            .IsUnique();

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.Price)
            .HasPrecision(18, 2);

        builder.Property(p => p.BillingCycle)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.Features)
            .HasMaxLength(2000);

        builder.Property(p => p.StripePriceId)
            .HasMaxLength(256);

        // Seed data
        builder.HasData(
            new Plan
            {
                Id = FreePlanId,
                Name = "Free",
                Description = "Perfect for individuals getting started.",
                Price = 0.00m,
                BillingCycle = BillingCycle.Monthly,
                Features = "1 project,5 GB storage,Community support",
                IsActive = true,
                StripePriceId = null,
                MaxRequests = 100,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate
            },
            new Plan
            {
                Id = ProPlanId,
                Name = "Pro",
                Description = "For professionals and growing teams.",
                Price = 29.99m,
                BillingCycle = BillingCycle.Monthly,
                Features = "Unlimited projects,50 GB storage,Priority email support,Advanced analytics",
                IsActive = true,
                StripePriceId = null,
                MaxRequests = 10000,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate
            },
            new Plan
            {
                Id = EntPlanId,
                Name = "Enterprise",
                Description = "Custom solutions for large organizations.",
                Price = 199.99m,
                BillingCycle = BillingCycle.Monthly,
                Features = "Unlimited projects,1 TB storage,24/7 dedicated support,Advanced analytics,Custom integrations,SLA guarantee,SSO & SAML",
                IsActive = true,
                StripePriceId = null,
                MaxRequests = 0, // Unlimited
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate
            }
        );
    }
}
