using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Client.Services.Core.Account;
using DevInstance.DevCoreApp.Shared.Model.Core.Account;

namespace DevInstance.DevCoreApp.Client.Services.Mocks.Core.Account;

/// <summary>Every account flow succeeds; the confirm-email link asks for a password, as an invitation does.</summary>
[BlazorServiceMock]
public class AccountServiceMock : IAccountService
{
    public async Task<ServiceActionResult<RegisterResult>> RegisterAsync(RegisterParameters input)
    {
        await Task.Delay(300);
        return ServiceActionResult<RegisterResult>.OK(RegisterResult.Success(requiresConfirmation: true));
    }

    public Task<ServiceActionResult<bool>> SendPasswordResetLinkAsync(ForgotPasswordParameters input) =>
        Task.FromResult(ServiceActionResult<bool>.OK(true));

    public Task<ServiceActionResult<bool>> ResetPasswordAsync(ResetPasswordParameters input) =>
        Task.FromResult(ServiceActionResult<bool>.OK(true));

    public Task<ServiceActionResult<ConfirmEmailResult>> ConfirmEmailAsync(ConfirmEmailRequest request) =>
        Task.FromResult(ServiceActionResult<ConfirmEmailResult>.OK(ConfirmEmailResult.Success(request.UserId, needsPassword: true)));

    public Task<ServiceActionResult<bool>> IsSetupRequiredAsync() =>
        Task.FromResult(ServiceActionResult<bool>.OK(false));

    public Task<ServiceActionResult<bool>> SetInvitationPasswordAsync(InvitationPasswordRequest request) =>
        Task.FromResult(ServiceActionResult<bool>.OK(true));
}
