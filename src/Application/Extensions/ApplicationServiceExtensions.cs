using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionBillingAPI.Application.Services;

namespace SubscriptionBillingAPI.Application.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IPlanService, PlanService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IWebhookService, WebhookService>();
        services.AddScoped<IUsageService, UsageService>();

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceExtensions).Assembly);

        return services;
    }
}
