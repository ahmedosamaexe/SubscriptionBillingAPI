using SubscriptionBillingAPI.Domain.Entities;

namespace SubscriptionBillingAPI.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the database context used by the Application layer.
/// Uses plain IQueryable to avoid EF Core package dependency in Application.
/// </summary>
public interface IAppDbContext
{
    IQueryable<User> Users { get; }
    IQueryable<Plan> Plans { get; }
    IQueryable<Subscription> Subscriptions { get; }
    IQueryable<Invoice> Invoices { get; }
    IQueryable<UsageLog> UsageLogs { get; }

    void Add<T>(T entity) where T : class;
    void Remove<T>(T entity) where T : class;
    Task<T?> FindAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
