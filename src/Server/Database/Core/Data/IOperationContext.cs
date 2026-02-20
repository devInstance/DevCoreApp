using System;
using System.Collections.Generic;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data;

/// <summary>
/// Provides contextual information about the current operation (HTTP request or background job)
/// to the data layer without introducing ASP.NET Core HTTP dependencies.
///
/// This abstraction allows EF Core interceptors, global query filters, and repositories to
/// access user identity and organization scope regardless of whether the code is running
/// inside an HTTP request pipeline or a background worker process.
///
/// Implementations:
///   - HttpOperationContext (WebService) — resolves values from HttpContext and Identity claims.
///   - BackgroundOperationContext (Services) — mutable context populated by the worker per job.
///
/// Registered in DI as scoped. A factory lambda selects the appropriate implementation
/// based on whether an HttpContext is present.
/// </summary>
public interface IOperationContext
{
    /// <summary>
    /// The internal database primary key (Guid) of the current UserProfile.
    /// This is NOT the ApplicationUser.Id — it is the UserProfile.Id that owns business data.
    /// Null when the request is unauthenticated or the user has no profile.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// The organization that new records should be assigned to.
    /// Null until the organization hierarchy feature is implemented.
    /// </summary>
    Guid? PrimaryOrganizationId { get; }

    /// <summary>
    /// The set of organization IDs the current user is allowed to see.
    /// Used by EF Core global query filters to scope data access.
    /// Empty until the organization hierarchy feature is implemented.
    /// </summary>
    IReadOnlySet<Guid> VisibleOrganizationIds { get; }

    /// <summary>
    /// The IP address of the caller. Populated from HttpContext.Connection.RemoteIpAddress
    /// for HTTP requests; null for background jobs unless explicitly set.
    /// </summary>
    string? IpAddress { get; }

    /// <summary>
    /// A unique identifier for tracing the current operation across logs and services.
    /// For HTTP requests this is Activity.Current.Id or HttpContext.TraceIdentifier.
    /// For background jobs this can be set to the Job.Id or a generated value.
    /// </summary>
    string? CorrelationId { get; }
}
