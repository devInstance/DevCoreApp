using System.Security.Claims;
using DevInstance.DevCoreApp.Client.Services.Core.Me;
using DevInstance.DevCoreApp.Client.Services.Core.Time;
using DevInstance.DevCoreApp.Shared.Model.Core.Permissions;
using DevInstance.DevCoreApp.Shared.Model.Core.UserAdmin;
using Microsoft.AspNetCore.Components.Authorization;

namespace DevInstance.DevCoreApp.Client.Services.Core.Auth;

/// <summary>The signed-in user as the UI sees them (from <c>GET api/me</c>).</summary>
public interface ICurrentUserState
{
    /// <summary>Null when signed out or not loaded yet.</summary>
    CurrentUserItem? User { get; }

    /// <summary>Re-reads <c>api/me</c>, e.g. after the user edits their profile.</summary>
    void Reload();
}

/// <summary>
/// Builds the authentication state from <c>GET api/me</c> whenever a session exists. Roles become
/// role claims and permissions become <see cref="PermissionClaims.Type"/> claims, so
/// <c>&lt;AuthorizeView Policy="Admin.Users.View"&gt;</c> works exactly as on the server. These are
/// for showing and hiding UI only — every endpoint enforces its own policy.
/// Also applies the user's time zone to <see cref="ILocalTimeService"/>.
/// </summary>
public sealed class ApiAuthenticationStateProvider : AuthenticationStateProvider, ICurrentUserState, IDisposable
{
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly AuthTokenStore store;
    private readonly IMeService me;
    private readonly ILocalTimeService time;
    private Task<AuthenticationState>? state;

    public ApiAuthenticationStateProvider(AuthTokenStore store, IMeService me, ILocalTimeService time)
    {
        this.store = store;
        this.me = me;
        this.time = time;
        store.SignedInChanged += Reload;
    }

    public CurrentUserItem? User { get; private set; }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => state ??= LoadAsync();

    public void Reload()
    {
        state = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private async Task<AuthenticationState> LoadAsync()
    {
        User = null;
        if (await store.GetAsync() == null)
        {
            time.SetTimeZone(null);
            return Anonymous;
        }

        var result = await me.GetAsync();
        if (!result.Success || result.Result == null)
        {
            // 401 after a failed refresh already cleared the session; other failures (server
            // down) leave it, so a reload can recover without signing in again.
            return Anonymous;
        }

        User = result.Result;
        time.SetTimeZone(User.Profile?.TimeZoneId);
        return new AuthenticationState(BuildPrincipal(User));
    }

    public static ClaimsPrincipal BuildPrincipal(CurrentUserItem user)
    {
        var claims = new List<Claim>();
        var profile = user.Profile;
        if (profile != null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, profile.Id ?? ""));
            claims.Add(new Claim(ClaimTypes.Name, string.IsNullOrWhiteSpace(profile.FullName) ? profile.Email ?? "" : profile.FullName));
            if (!string.IsNullOrEmpty(profile.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, profile.Email));
            }
        }

        claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(user.Permissions.Select(p => new Claim(PermissionClaims.Type, p)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, ClaimTypes.Role));
    }

    public void Dispose() => store.SignedInChanged -= Reload;
}
