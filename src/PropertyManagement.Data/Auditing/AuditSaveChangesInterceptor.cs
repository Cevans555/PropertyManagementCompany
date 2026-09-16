using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PropertyManagement.Core.Common.Interfaces;

namespace PropertyManagement.Data.Auditing;

public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public AuditSaveChangesInterceptor(ICurrentUser currentUser, TimeProvider timeProvider)
    {
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        StampAuditColumns(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        StampAuditColumns(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void StampAuditColumns(DbContext? context)
    {
        if (context is null)
            return;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var userId = _currentUser.UserId ?? CurrentUserDefaults.SystemUserId;

        foreach (var entry in context.ChangeTracker.Entries<ICreationAudited>())
        {
            if (entry.State == EntityState.Added)
            {
                StampCreated(entry, now, userId);
            }
            else if (entry.Entity is IAuditable && IsChanged(entry))
            {
                entry.Property(nameof(IAuditable.ModifiedAt)).CurrentValue = now;
                entry.Property(nameof(IAuditable.ModifiedById)).CurrentValue = userId;
            }
        }
    }

    private static void StampCreated(EntityEntry<ICreationAudited> entry, DateTime now, string userId)
    {
        if (entry.Entity.CreatedAt == default)
            entry.Property(nameof(ICreationAudited.CreatedAt)).CurrentValue = now;

        if (string.IsNullOrEmpty(entry.Entity.CreatedById))
            entry.Property(nameof(ICreationAudited.CreatedById)).CurrentValue = userId;
    }

    private static bool IsChanged(EntityEntry entry)
    {
        if (entry.State == EntityState.Modified)
            return true;

        return HasOwnedChanges(entry);
    }

    // An edited Address is tracked as its own owned entry, so its owner can look unchanged.
    private static bool HasOwnedChanges(EntityEntry entry)
    {
        foreach (var reference in entry.References)
        {
            var target = reference.TargetEntry;
            if (target is null || !target.Metadata.IsOwned())
                continue;

            if (target.State == EntityState.Added || target.State == EntityState.Modified || target.State == EntityState.Deleted)
                return true;
        }

        return false;
    }
}
