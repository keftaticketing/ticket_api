namespace TicketSystem.Infrastructure.Persistence.Interceptors;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TicketSystem.Application.Abstractions.Authentication;
using TicketSystem.Application.Abstractions.Time;
using TicketSystem.Domain.Common;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;

public sealed class AuditingSaveChangesInterceptor(
    ICurrentUserService currentUserService,
    IBusinessClock businessClock) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            AddAuditEntries(eventData.Context);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            AddAuditEntries(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    private void AddAuditEntries(DbContext context)
    {
        var userId = currentUserService.UserId;
        var userName = currentUserService.UserName;
        var now = businessClock.UtcNow;

        var entries = context.ChangeTracker.Entries()
            .Where(entry => entry.Entity is IAuditableEntity
                            && entry.Entity is not AuditLog
                            && entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var entityName = entry.Metadata.ClrType.Name;
            var entityId = GetEntityId(entry);
            if (string.IsNullOrWhiteSpace(entityId))
            {
                continue;
            }

            var audit = new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityName = entityName,
                EntityId = entityId,
                Action = entry.State switch
                {
                    EntityState.Added => AuditAction.Created,
                    EntityState.Modified => AuditAction.Updated,
                    EntityState.Deleted => AuditAction.Deleted,
                    _ => AuditAction.Updated
                },
                UserId = userId,
                UserName = userName,
                OccurredAt = now,
                Changes = SerializeChanges(entry)
            };

            context.Set<AuditLog>().Add(audit);
        }
    }

    private static string GetEntityId(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var key = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        if (key?.CurrentValue is null)
        {
            return string.Empty;
        }

        return key.CurrentValue.ToString() ?? string.Empty;
    }

    private static string? SerializeChanges(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        return entry.State switch
        {
            EntityState.Added => JsonSerializer.Serialize(
                entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue), JsonOptions),
            EntityState.Deleted => JsonSerializer.Serialize(
                entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue), JsonOptions),
            EntityState.Modified => JsonSerializer.Serialize(
                entry.Properties
                    .Where(p => p.IsModified)
                    .ToDictionary(
                        p => p.Metadata.Name,
                        p => new { Old = p.OriginalValue, New = p.CurrentValue }),
                JsonOptions),
            _ => null
        };
    }
}
