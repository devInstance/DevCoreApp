using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Core.UserAdmin;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Core.UserAdmin;

/// <summary>
/// The signed-in user as a client sees them (<c>GET api/me</c>).
/// </summary>
public interface ICurrentUserService
{
    Task<ServiceActionResult<CurrentUserItem>> GetAsync();
}
