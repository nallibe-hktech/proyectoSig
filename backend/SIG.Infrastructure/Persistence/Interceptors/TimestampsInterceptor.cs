using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SIG.Domain.Common;

namespace SIG.Infrastructure.Persistence.Interceptors;

public class TimestampsInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyTimestamps(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ApplyTimestamps(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ApplyTimestamps(DbContext? context)
    {
        if (context is null) return;
        var now = DateTime.UtcNow;
        foreach (EntityEntry entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditable a)
            {
                if (entry.State == EntityState.Added)
                {
                    a.CreatedAt = now;
                    a.UpdatedAt = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    a.UpdatedAt = now;
                }
            }
        }
    }
}
