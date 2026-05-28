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

        // Si no hay usuario autenticado (ej: durante login), no auditar
        int? userId = null;
        try { userId = _currentUser.UserId; }
        catch { }

        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Create,
                EntityState.Modified => AuditAction.Update,
                EntityState.Deleted => AuditAction.Delete,
                _ => AuditAction.Update
            };

            var typeName = entry.Entity.GetType().Name;
            var idVal = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "?";

            string? oldJson = null, newJson = null;
            try
            {
                if (entry.State == EntityState.Modified)
                {
                    var oldVals = entry.Properties.Where(p => p.IsModified && !ExcludedFields.Contains(p.Metadata.Name))
                        .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
                    var newVals = entry.Properties.Where(p => p.IsModified && !ExcludedFields.Contains(p.Metadata.Name))
                        .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
                    oldJson = JsonSerializer.Serialize(oldVals);
                    newJson = JsonSerializer.Serialize(newVals);
                }
                else if (entry.State == EntityState.Added)
                {
                    var newVals = entry.Properties.Where(p => !ExcludedFields.Contains(p.Metadata.Name))
                        .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
                    newJson = JsonSerializer.Serialize(newVals);
                }
                else if (entry.State == EntityState.Deleted)
                {
                    var oldVals = entry.Properties.Where(p => !ExcludedFields.Contains(p.Metadata.Name))
                        .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
                    oldJson = JsonSerializer.Serialize(oldVals);
                }
            }
            catch { /* ignore json errors */ }

            logs.Add(new AuditLog
            {
                UserId = userId,
                EntityType = typeName,
                EntityId = idVal,
                Action = action,
                OldValueJson = oldJson,
                NewValueJson = newJson,
                Timestamp = now,
                Ip = ip
            });
        }

        foreach (var log in logs)
            context.Set<AuditLog>().Add(log);
    }
}
