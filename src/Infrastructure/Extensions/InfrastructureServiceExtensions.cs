using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionBillingAPI.Application.Common.Interfaces;
using SubscriptionBillingAPI.Application.Services;
using SubscriptionBillingAPI.Infrastructure.Persistence;
using SubscriptionBillingAPI.Infrastructure.Services;

namespace SubscriptionBillingAPI.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;

        // ── EF Core with PostgreSQL ──────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
            )
        );

        services.AddScoped<IAppDbContext>(provider =>
            provider.GetRequiredService<AppDbContext>());

        // ── Application services implemented in Infrastructure ──
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IStripePaymentService, StripePaymentService>();
        services.AddScoped<IBackgroundJobService, HangfireBackgroundJobService>();

        // ── Hangfire Configuration (PostgreSQL storage) ──────────
        services.AddHangfire(config =>
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                  .UseSimpleAssemblyNameTypeSerializer()
                  .UseRecommendedSerializerSettings()
                  .UsePostgreSqlStorage(options =>
                      options.UseNpgsqlConnection(connectionString)));

        services.AddHangfireServer();

        // ── Register Hangfire job classes for DI resolution ──────
        services.AddScoped<DailyRenewalCheckJob>();
        services.AddScoped<GracePeriodExpiryJob>();
        services.AddScoped<MonthlyUsageResetJob>();
        services.AddScoped<FailedPaymentRetryJob>();

        return services;
    }
}
