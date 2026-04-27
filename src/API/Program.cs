using System.Text;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FluentValidation.AspNetCore;
using Scalar.AspNetCore;
using Serilog;
using SubscriptionBillingAPI.API.Middleware;
using SubscriptionBillingAPI.API.Services;
using SubscriptionBillingAPI.Application.Common.Interfaces;
using SubscriptionBillingAPI.Application.Extensions;
using SubscriptionBillingAPI.Infrastructure.Extensions;
using SubscriptionBillingAPI.Infrastructure.Persistence;
using SubscriptionBillingAPI.Infrastructure.Services;

// ──────────────────────────────────────────────────────────────
// Serilog bootstrap logger (captures startup errors)
// ──────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting SubscriptionBillingAPI...");

    var builder = WebApplication.CreateBuilder(args);

    // ──────────────────────────────────────────────────────────
    // Serilog — read full config from appsettings.json
    // ──────────────────────────────────────────────────────────
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration)
                     .ReadFrom.Services(services)
                     .Enrich.FromLogContext());

    // ──────────────────────────────────────────────────────────
    // Application & Infrastructure services
    // ──────────────────────────────────────────────────────────
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // JWT token service lives in API (has JwtBearer package)
    builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

    // ──────────────────────────────────────────────────────────
    // FluentValidation Auto-Validation
    // ──────────────────────────────────────────────────────────
    builder.Services.AddFluentValidationAutoValidation();

    // ──────────────────────────────────────────────────────────
    // Controllers & API explorer (for Scalar)
    // ──────────────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // ──────────────────────────────────────────────────────────
    // OpenAPI / Swagger
    // ──────────────────────────────────────────────────────────
    builder.Services.AddOpenApi();

    // ──────────────────────────────────────────────────────────
    // JWT Authentication
    // ──────────────────────────────────────────────────────────
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"]
        ?? throw new InvalidOperationException("JWT SecretKey is missing from configuration.");

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtSettings["Issuer"],
            ValidAudience            = jwtSettings["Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

    builder.Services.AddAuthorization();

    // ──────────────────────────────────────────────────────────
    // Build the application
    // ──────────────────────────────────────────────────────────
    var app = builder.Build();

    // ──────────────────────────────────────────────────────────
    // Auto-migration on startup
    // ──────────────────────────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Log.Information("Applying EF Core migrations...");
        try 
        {
            await db.Database.MigrateAsync();
            Log.Information("Migrations applied successfully.");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to apply migrations. The database might not be available.");
        }
    }

    // ──────────────────────────────────────────────────────────
    // Middleware pipeline
    // ──────────────────────────────────────────────────────────
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.Title = "Subscription Billing API";
            options.Theme = ScalarTheme.DeepSpace;
            options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
        });
    }

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();

    // ──────────────────────────────────────────────────────────
    // Hangfire Dashboard (Admin only)
    // ──────────────────────────────────────────────────────────
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new HangfireDashboardAuthorizationFilter()]
    });

    app.MapControllers();

    // ──────────────────────────────────────────────────────────
    // Schedule Recurring Hangfire Jobs
    // ──────────────────────────────────────────────────────────
    RecurringJob.AddOrUpdate<DailyRenewalCheckJob>(
        "daily-renewal-check",
        job => job.ExecuteAsync(CancellationToken.None),
        "5 0 * * *"); // Daily at 00:05 UTC

    RecurringJob.AddOrUpdate<GracePeriodExpiryJob>(
        "grace-period-expiry",
        job => job.ExecuteAsync(CancellationToken.None),
        "0 1 * * *"); // Daily at 01:00 UTC

    RecurringJob.AddOrUpdate<MonthlyUsageResetJob>(
        "monthly-usage-reset",
        job => job.ExecuteAsync(CancellationToken.None),
        "1 0 1 * *"); // 1st of month at 00:01 UTC

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    await Log.CloseAndFlushAsync();
}
