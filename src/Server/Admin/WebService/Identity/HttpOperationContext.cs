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
///
/// Note: Dependencies are resolved lazily (via IServiceProvider) to break a
/// circular dependency: IOperationContext → HttpOperationContext → UserManager
/// → UserStore → ApplicationDbContext → IOperationContext.
/// </summary>
public class HttpOperationContext : IOperationContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;

    private Guid? _resolvedUserId;
    private bool _userIdResolved;

    private static readonly IReadOnlySet<Guid> EmptyOrgSet = new HashSet<Guid>();

    public HttpOperationContext(
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
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
            if (!_userIdResolved)
            {
                _userIdResolved = true;
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.User != null)
                {
                    var userManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    var appUserId = userManager.GetUserId(httpContext.User);
                    if (!string.IsNullOrEmpty(appUserId) && Guid.TryParse(appUserId, out var parsedAppUserId))
                    {
                        var queryRepository = _serviceProvider.GetRequiredService<IQueryRepository>();
                        var profile = queryRepository
                            .GetUserProfilesQuery(null!)
                            .ByApplicationUserId(parsedAppUserId)
                            .Select()
                            .FirstOrDefault();

                        _resolvedUserId = profile?.Id;
                    }
                }
            }
            return _resolvedUserId;
        }
    }

    public Guid? PrimaryOrganizationId => null;

    public IReadOnlySet<Guid> VisibleOrganizationIds => EmptyOrgSet;

    public string? IpAddress =>
        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? CorrelationId =>
        Activity.Current?.Id ?? _httpContextAccessor.HttpContext?.TraceIdentifier;
}
