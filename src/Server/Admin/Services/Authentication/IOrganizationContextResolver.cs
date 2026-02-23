namespace DevInstance.DevCoreApp.Server.Admin.Services.Authentication;

/// <summary>
/// Precomputes the set of all OrganizationId values visible to a user.
/// Called during claims transformation to populate IOperationContext.VisibleOrganizationIds.
/// </summary>
public interface IOrganizationContextResolver
{
    /// <summary>
    /// Resolves all organization IDs visible to the given user based on their
    /// UserOrganization assignments and access scopes.
    ///
    /// For Self scope: includes only the assigned organization.
    /// For WithChildren scope: includes the assigned organization plus all
    /// descendants found via materialized path prefix matching.
    ///
    /// Returns the primary organization ID (the assignment marked IsPrimary)
    /// along with the full set of visible IDs.
    /// </summary>
    Task<OrganizationContextResult> ResolveAsync(Guid userId);

    /// <summary>
    /// Invalidates cached organization context for a specific user.
    /// Call this when a user's organization assignments change.
    /// </summary>
    void InvalidateCache(Guid userId);
}

/// <summary>
/// Result of resolving a user's organization context.
/// </summary>
public class OrganizationContextResult
{
    public Guid? PrimaryOrganizationId { get; init; }
    public HashSet<Guid> VisibleOrganizationIds { get; init; } = new();
}
