using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IOperationContext _operationContext;
    private List<AuditEntry>? _pendingEntries;

    public AuditInterceptor(IOperationContext operationContext)
    {
        _operationContext = operationContext;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not ApplicationDbContext db)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        _pendingEntries = CaptureAuditEntries(db);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not ApplicationDbContext db)
            return base.SavingChanges(eventData, result);

        _pendingEntries = CaptureAuditEntries(db);

        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is ApplicationDbContext db && _pendingEntries is { Count: > 0 })
        {
            await WriteAuditLogsAsync(db, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(
        SaveChangesCompletedEventData eventData,
        int result)
    {
        if (eventData.Context is ApplicationDbContext db && _pendingEntries is { Count: > 0 })
        {
            WriteAuditLogsAsync(db, CancellationToken.None).GetAwaiter().GetResult();
        }

        return base.SavedChanges(eventData, result);
    }

    private List<AuditEntry> CaptureAuditEntries(ApplicationDbContext db)
    {
        var entries = new List<AuditEntry>();
        var now = DateTime.UtcNow;

        foreach (var entry in db.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog)
                continue;

            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            var tableName = entry.Metadata.GetTableName();
            if (tableName == null)
                continue;

            var auditEntry = new AuditEntry
            {
                TableName = tableName,
                Action = entry.State switch
                {
                    EntityState.Added => AuditAction.Insert,
                    EntityState.Modified => AuditAction.Update,
                    EntityState.Deleted => AuditAction.Delete,
                    _ => AuditAction.Update
                },
                ChangedAt = now,
                ChangedByUserId = _operationContext.UserId,
                IpAddress = _operationContext.IpAddress,
                CorrelationId = _operationContext.CorrelationId,
            };

            // Capture OrganizationId if the entity is org-scoped
            if (entry.Entity is IOrganizationScoped scoped)
            {
                auditEntry.OrganizationId = scoped.OrganizationId;
            }

            var excludedProperties = new HashSet<string>();
            foreach (var prop in entry.Metadata.GetProperties())
            {
                var clrProperty = prop.PropertyInfo;
                if (clrProperty != null &&
                    clrProperty.GetCustomAttributes(typeof(AuditExcludeAttribute), true).Length > 0)
                {
                    excludedProperties.Add(prop.Name);
                }
            }

            var oldValues = new Dictionary<string, object?>();
            var newValues = new Dictionary<string, object?>();
            var tempKeyProperties = new List<string>();

            foreach (var property in entry.Properties)
            {
                var propertyName = property.Metadata.Name;

                if (excludedProperties.Contains(propertyName))
                    continue;

                if (property.Metadata.IsPrimaryKey())
                {
                    // For Added entities, the PK may be a temp value (e.g. Guid.Empty)
                    // that gets replaced after SaveChanges. Track these for deferred resolution.
                    if (entry.State == EntityState.Added && property.IsTemporary)
                    {
                        tempKeyProperties.Add(propertyName);
                    }

                    auditEntry.RecordId = property.CurrentValue?.ToString() ?? string.Empty;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        newValues[propertyName] = property.CurrentValue;
                        break;

                    case EntityState.Deleted:
                        oldValues[propertyName] = property.OriginalValue;
                        break;

                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            oldValues[propertyName] = property.OriginalValue;
                            newValues[propertyName] = property.CurrentValue;
                        }
                        break;
                }
            }

            // For Added entities, capture temp key properties for post-save resolution
            auditEntry.TempKeyProperties = tempKeyProperties;
            auditEntry.EntityEntry = entry.State == EntityState.Added ? entry : null;

            if (oldValues.Count > 0)
                auditEntry.OldValues = JsonSerializer.Serialize(oldValues);
            if (newValues.Count > 0)
                auditEntry.NewValues = JsonSerializer.Serialize(newValues);

            entries.Add(auditEntry);
        }

        return entries;
    }

    private async Task WriteAuditLogsAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var auditLogs = new List<AuditLog>();

        foreach (var entry in _pendingEntries!)
        {
            // For newly added entities, resolve the final PK value after SaveChanges
            if (entry.EntityEntry != null && entry.TempKeyProperties.Count > 0)
            {
                foreach (var keyProp in entry.TempKeyProperties)
                {
                    var prop = entry.EntityEntry.Property(keyProp);
                    entry.RecordId = prop.CurrentValue?.ToString() ?? string.Empty;
                }
            }

            auditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                OrganizationId = entry.OrganizationId,
                TableName = entry.TableName,
                RecordId = entry.RecordId,
                Action = entry.Action,
                OldValues = entry.OldValues,
                NewValues = entry.NewValues,
                ChangedByUserId = entry.ChangedByUserId,
                ChangedAt = entry.ChangedAt,
                IpAddress = entry.IpAddress,
                CorrelationId = entry.CorrelationId,
                Source = AuditSource.Application
            });
        }

        if (auditLogs.Count > 0)
        {
            db.AuditLogs.AddRange(auditLogs);
            await db.SaveChangesAsync(cancellationToken);
        }

        _pendingEntries = null;
    }

    /// <summary>
    /// Internal representation of a pending audit entry captured before SaveChanges completes.
    /// </summary>
    private class AuditEntry
    {
        public string TableName { get; set; } = string.Empty;
        public string RecordId { get; set; } = string.Empty;
        public AuditAction Action { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid? ChangedByUserId { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? IpAddress { get; set; }
        public string? CorrelationId { get; set; }

        // For deferred PK resolution on Added entities
        public EntityEntry? EntityEntry { get; set; }
        public List<string> TempKeyProperties { get; set; } = new();
    }
}
