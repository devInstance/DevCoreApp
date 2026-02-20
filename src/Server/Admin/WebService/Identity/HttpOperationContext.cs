using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using Microsoft.AspNetCore.Identity;
using System.Diagnostics;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Identity;

/// <summary>
/// IOperationContext implementation for HTTP request pipelines.
///
/// Resolves the current user from ASP.NET Identity claims and maps it to the
/// internal UserProfile.Id. The resolution is lazy — the database query only
/// runs on first access and the result is cached for the lifetime of the scope.
///
/// This follows the same resolution pattern as AuthorizationContext.CurrentProfile:
///   ClaimsPrincipal → UserManager.GetUserId() → UserProfilesQuery.ByApplicationUserId() → UserProfile.Id
///
/// Organization properties return null/empty because the organization hierarchy
/// entities have not been implemented yet. Once they are, this class will resolve
/// the user's primary org and visible org set from the UserOrganizations table.
/// </summary>
public class HttpOperationContext : IOperationContext
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IQueryRepository queryRepository;

    private Guid? resolvedUserId;
    private bool userIdResolved;

    private static readonly IReadOnlySet<Guid> EmptyOrgSet = new HashSet<Guid>();

    public HttpOperationContext(
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager,
        IQueryRepository queryRepository)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.userManager = userManager;
        this.queryRepository = queryRepository;
    }

    /// <summary>
    /// Lazily resolves the UserProfile.Id (internal Guid PK) from the current
    /// ClaimsPrincipal. The result is cached for the lifetime of this scoped instance.
    /// Returns null for unauthenticated requests or users without a profile.
    /// </summary>
    public Guid? UserId
    {
        get
        {
            if (!userIdResolved)
            {
                userIdResolved = true;
                var httpContext = httpContextAccessor.HttpContext;
                if (httpContext?.User != null)
                {
                    // Extract the ApplicationUser.Id from Identity claims
                    var appUserId = userManager.GetUserId(httpContext.User);
                    if (!string.IsNullOrEmpty(appUserId) && Guid.TryParse(appUserId, out var parsedAppUserId))
                    {
                        // Look up the UserProfile linked to this ApplicationUser
                        var profile = queryRepository
                            .GetUserProfilesQuery(null!)
                            .ByApplicationUserId(parsedAppUserId)
                            .Select()
                            .FirstOrDefault();

                        resolvedUserId = profile?.Id;
                    }
                }
            }
            return resolvedUserId;
        }
    }

    /// <summary>
    /// Not yet implemented — will resolve from UserOrganizations once the org hierarchy is built.
    /// </summary>
    public Guid? PrimaryOrganizationId => null;

    /// <summary>
    /// Not yet implemented — will resolve from UserOrganizations once the org hierarchy is built.
    /// </summary>
    public IReadOnlySet<Guid> VisibleOrganizationIds => EmptyOrgSet;

    public string? IpAddress =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    /// <summary>
    /// Returns the W3C trace ID from the current Activity if available (for distributed tracing),
    /// otherwise falls back to ASP.NET Core's per-request TraceIdentifier.
    /// </summary>
    public string? CorrelationId =>
        Activity.Current?.Id ?? httpContextAccessor.HttpContext?.TraceIdentifier;
}
