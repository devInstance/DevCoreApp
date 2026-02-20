using DevInstance.DevCoreApp.Server.Database.Core.Data;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Background;

/// <summary>
/// IOperationContext implementation for background worker jobs.
///
/// Unlike HttpOperationContext (which derives values from HttpContext), this class
/// exposes public setters so the BackgroundWorker can populate context from job metadata
/// before executing each job. This enables audit logging and future org-scoped queries
/// to work correctly in background jobs.
///
/// Usage in BackgroundWorker.ExecuteAsync:
///   1. Create a new DI scope per job
///   2. Resolve BackgroundOperationContext from the scope
///   3. Call Reset() to clear any prior state
///   4. Set UserId, CorrelationId, etc. from BackgroundRequestItem metadata
///   5. Execute the job — any service or interceptor that injects IOperationContext
///      will receive the values set here
/// </summary>
public class BackgroundOperationContext : IOperationContext
{
    private HashSet<Guid> visibleOrganizationIds = new();

    public Guid? UserId { get; set; }
    public Guid? PrimaryOrganizationId { get; set; }
    public IReadOnlySet<Guid> VisibleOrganizationIds => visibleOrganizationIds;
    public string? IpAddress { get; set; }
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Replaces the visible organization set. Use this instead of setting the
    /// VisibleOrganizationIds property directly (which is read-only on the interface).
    /// </summary>
    public void SetVisibleOrganizationIds(IEnumerable<Guid> ids)
    {
        visibleOrganizationIds = new HashSet<Guid>(ids);
    }

    /// <summary>
    /// Clears all properties to their default values. Called at the start of each
    /// job to ensure no state leaks between consecutive jobs within the same scope.
    /// </summary>
    public void Reset()
    {
        UserId = null;
        PrimaryOrganizationId = null;
        visibleOrganizationIds = new();
        IpAddress = null;
        CorrelationId = null;
    }
}
