using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Common;
using SIG.Domain.Entities;
using SIG.Domain.Enums;

namespace SIG.Infrastructure.Persistence.Interceptors;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;
    private static readonly string[] ExcludedTypes = new[] { nameof(AuditLog), nameof(CalculationLog), nameof(ApprovalHistory) };
    private static readonly string[] ExcludedFields = new[] { "PasswordHash" };
    internal static readonly AsyncLocal<bool> SuppressAudit = new();

    public AuditInterceptor(ICurrentUserService currentUser) { _currentUser = currentUser; }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (!SuppressAudit.Value)
            AddAuditEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (!SuppressAudit.Value)
            AddAuditEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddAuditEntries(DbContext? context)
    {
        if (context is null) return;
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditable && (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            .Where(e => !ExcludedTypes.Contains(e.Entity.GetType().Name))
            .ToList();

        var logs = new List<AuditLog>();
        var ip = _currentUser.Ip;
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            var log = BuildAuditLog(entry, GetUserId(), ip, now);
            if (log is not null)
                logs.Add(log);
        }

        foreach (var log in logs)
            context.Set<AuditLog>().Add(log);
    }

    private int? GetUserId()
    {
        try { return _currentUser.UserId; }
        catch { return null; }
    }

    private static AuditLog? BuildAuditLog(EntityEntry entry, int? userId, string? ip, DateTime now)
    {
        var action = MapAuditAction(entry.State);

        var typeName = entry.Entity.GetType().Name;
        var idVal = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "?";

        var (oldJson, newJson) = SerializeChanges(entry);

        return new AuditLog
        {
            UserId = userId,
            EntityType = typeName,
            EntityId = idVal,
            Action = action,
            OldValueJson = oldJson,
            NewValueJson = newJson,
            Timestamp = now,
            Ip = ip
        };
    }

    private static AuditAction MapAuditAction(EntityState state)
    {
        return state switch
        {
            EntityState.Added => AuditAction.Create,
            EntityState.Modified => AuditAction.Update,
            EntityState.Deleted => AuditAction.Delete,
            _ => AuditAction.Update
        };
    }

    private static (string? oldJson, string? newJson) SerializeChanges(EntityEntry entry)
    {
        try
        {
            return MapSerialization(entry);
        }
        catch { return (null, null); }
    }

    private static (string? oldJson, string? newJson) MapSerialization(EntityEntry entry)
    {
        return entry.State switch
        {
            EntityState.Modified => SerializeModified(entry),
            EntityState.Added => SerializeAdded(entry),
            EntityState.Deleted => SerializeDeleted(entry),
            _ => (null, null)
        };
    }

    private static (string? oldJson, string? newJson) SerializeModified(EntityEntry entry)
    {
        var filter = entry.Properties.Where(p => p.IsModified && !ExcludedFields.Contains(p.Metadata.Name));
        var oldVals = filter.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
        var newVals = filter.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
        return (JsonSerializer.Serialize(oldVals), JsonSerializer.Serialize(newVals));
    }

    private static (string? oldJson, string? newJson) SerializeAdded(EntityEntry entry)
    {
        var newVals = entry.Properties.Where(p => !ExcludedFields.Contains(p.Metadata.Name))
            .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
        return (null, JsonSerializer.Serialize(newVals));
    }

    private static (string? oldJson, string? newJson) SerializeDeleted(EntityEntry entry)
    {
        var oldVals = entry.Properties.Where(p => !ExcludedFields.Contains(p.Metadata.Name))
            .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
        return (JsonSerializer.Serialize(oldVals), null);
    }
}
