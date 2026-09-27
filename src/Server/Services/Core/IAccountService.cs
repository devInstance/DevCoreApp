using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Core.Account;

namespace DevInstance.DevCoreApp.Server.Services.Core;

/// <summary>
/// Anonymous account flows exposed through <c>api/account</c>, plus the first-run owner setup
/// (the server's <c>/setup</c> page). Sign-in itself is JWT (<c>IJwtAuthService</c>,
/// <c>api/auth</c>); nothing here issues a cookie.
/// Failures are thrown as WebServiceToolkit exceptions (400), so a returned result succeeded.
/// Email links are built from <c>App:BaseUrl</c> (or the request origin) and point at the
/// client routes in <c>AccountRoutes</c>.
/// </summary>
public interface IAccountService
{
    Task<ServiceActionResult<RegisterResult>> RegisterAsync(RegisterParameters input);

    /// <summary>Always succeeds, whether or not the email exists, to prevent enumeration.</summary>
    Task<ServiceActionResult<bool>> SendPasswordResetLinkAsync(ForgotPasswordParameters input);

    /// <summary><see cref="ResetPasswordParameters.Code"/> is the encoded code from the link.</summary>
    Task<ServiceActionResult<bool>> ResetPasswordAsync(ResetPasswordParameters input);

    Task<ServiceActionResult<ConfirmEmailResult>> ConfirmEmailAsync(ConfirmEmailRequest request);

    Task<ServiceActionResult<bool>> SetInvitationPasswordAsync(InvitationPasswordRequest request);

    /// <summary>True while no user exists — the only time owner setup is allowed.</summary>
    Task<ServiceActionResult<bool>> IsSetupRequiredAsync();

    /// <summary>Creates the Owner account in the root organization. 403 once any user exists.</summary>
    Task<ServiceActionResult<bool>> SetupOwnerAsync(SetupOwnerParameters input);
}
