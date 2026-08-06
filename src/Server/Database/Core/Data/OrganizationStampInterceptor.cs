using DevInstance.DevCoreApp.Server.Database.Core.Models.BackgroundTasks;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data;

/// <summary>
/// Stamps <see cref="IOrganizationScoped.OrganizationId"/> from the ambient
/// <see cref="IOperationContext"/> on every inserted row that has not set one explicitly, and
/// refuses the insert when no organization can be resolved.
/// <para>
/// <b>Why an interceptor rather than per-service assignment.</b> The read-side global query filter
/// (see <c>ApplicationDbContext.ApplyFilterToEntity</c>) is deliberately fail-open: an empty
/// <see cref="IOperationContext.VisibleOrganizationIds"/> disables filtering instead of returning
/// nothing. That makes a forgotten write-side stamp invisible in development — the row lands with
/// <see cref="Guid.Empty"/> and reads back fine for an unscoped user, then disappears for every
/// real one. Stamping centrally, in the one place every insert must pass through, means a new
/// create path cannot silently opt out of scoping.
/// </para>
/// <para>
/// Registered in <c>ApplicationDbContext.OnConfiguring</c>, which runs for every context instance —
/// the DI-scoped one and the short-lived ones built by <see cref="IAppDbContextFactory"/> for the
/// per-operation unit of work — so it also covers entities created without <c>CreateNew()</c>.
/// </para>
/// <para>
/// An already-set <see cref="IOrganizationScoped.OrganizationId"/> is left alone, so a caller that
/// deliberately writes into another organization (a background job stamping rows from the parent
/// record's organization, for example) keeps that ability.
/// </para>
/// </summary>
public class OrganizationStampInterceptor : SaveChangesInterceptor
{
    /// <summary>
    /// Types allowed to insert with an unresolved organization.
    /// <para>
    /// <see cref="BackgroundTask"/> rows are built inside a fresh DI scope whose operation context
    /// has been reset, so no ambient organization exists at insert time. <c>BackgroundWorker</c>
    /// carries the submitter's organization on <c>BackgroundRequestItem.OrganizationId</c> and
    /// assigns it explicitly where the caller supplied one; submissions that genuinely have no
    /// organization (account confirmation and password-reset mail sent from unauthenticated flows)
    /// still write <see cref="Guid.Empty"/>. Guarding those here would fail the email outright
    /// rather than merely hide a job row, so they are permitted through.
    /// </para>
    /// </summary>
    private static readonly HashSet<Type> UnscopedInsertsAllowed = new() { typeof(BackgroundTask) };

    private readonly IOperationContext _operationContext;

    public OrganizationStampInterceptor(IOperationContext operationContext)
    {
        _operationContext = operationContext;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is ApplicationDbContext db)
        {
            StampOrganization(db);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is ApplicationDbContext db)
        {
            StampOrganization(db);
        }

        return base.SavingChanges(eventData, result);
    }

    private void StampOrganization(ApplicationDbContext db)
    {
        foreach (var entry in db.ChangeTracker.Entries<IOrganizationScoped>())
        {
            if (entry.State != EntityState.Added)
                continue;

            if (entry.Entity.OrganizationId != Guid.Empty)
                continue;

            if (UnscopedInsertsAllowed.Contains(entry.Metadata.ClrType))
                continue;

            // ClrType.Name rather than GetTableName(): the latter is a relational-only extension
            // and returns nothing under the in-memory provider used by tests.
            entry.Entity.OrganizationId = _operationContext.PrimaryOrganizationId
                ?? throw new InvalidOperationException(
                    $"Cannot insert {entry.Metadata.ClrType.Name}: the row is organization-scoped but " +
                    "the operation context resolves no PrimaryOrganizationId. Establish an " +
                    "IOperationContext with an organization before writing, or set OrganizationId explicitly.");
        }
    }
}
