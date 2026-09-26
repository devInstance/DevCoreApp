using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Core.Account;

namespace DevInstance.DevCoreApp.Client.Services.Core.Account;

/// <summary>
/// Anonymous account flows (<c>api/account</c>): self-registration and the links sent by email.
/// Sign-in itself is <c>IAuthService</c>. Failures come back as the server's message.
/// </summary>
public interface IAccountService
{
    Task<ServiceActionResult<RegisterResult>> RegisterAsync(RegisterParameters input);

    /// <summary>Succeeds whether or not the email exists, so it cannot be used to probe accounts.</summary>
    Task<ServiceActionResult<bool>> SendPasswordResetLinkAsync(ForgotPasswordParameters input);

    /// <summary><see cref="ResetPasswordParameters.Code"/> is the code exactly as it appears in the link.</summary>
    Task<ServiceActionResult<bool>> ResetPasswordAsync(ResetPasswordParameters input);

    /// <summary>Confirms the email from the confirmation or invitation link.</summary>
    Task<ServiceActionResult<ConfirmEmailResult>> ConfirmEmailAsync(ConfirmEmailRequest request);

    /// <summary>An invited user's first password; the link's code proves they received the email.</summary>
    Task<ServiceActionResult<bool>> SetInvitationPasswordAsync(InvitationPasswordRequest request);

    /// <summary>True until the owner account exists (then the server's <c>/setup</c> page is closed).</summary>
    Task<ServiceActionResult<bool>> IsSetupRequiredAsync();
}
