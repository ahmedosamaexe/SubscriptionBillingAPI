using Microsoft.EntityFrameworkCore;
using SubscriptionBillingAPI.Application.Common.Interfaces;
using SubscriptionBillingAPI.Domain.Entities;

namespace SubscriptionBillingAPI.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<UsageLog> UsageLogs => Set<UsageLog>();

    // ── IAppDbContext explicit implementations ──────────────────
    IQueryable<User> IAppDbContext.Users => Users;
    IQueryable<Plan> IAppDbContext.Plans => Plans;
    IQueryable<Subscription> IAppDbContext.Subscriptions => Subscriptions;
    IQueryable<Invoice> IAppDbContext.Invoices => Invoices;
    IQueryable<UsageLog> IAppDbContext.UsageLogs => UsageLogs;

    void IAppDbContext.Add<T>(T entity) => base.Add(entity);
    void IAppDbContext.Remove<T>(T entity) => base.Remove(entity);

    async Task<T?> IAppDbContext.FindAsync<T>(Guid id, CancellationToken cancellationToken)
        where T : class
        => await base.FindAsync<T>([id], cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
