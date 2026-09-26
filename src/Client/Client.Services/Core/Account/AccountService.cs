using DevInstance.BlazorToolkit.Http;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Client.Services.Core.Api;
using DevInstance.DevCoreApp.Shared.Model.Core.Account;
using DevInstance.LogScope;

namespace DevInstance.DevCoreApp.Client.Services.Core.Account;

/// <summary>
/// Uses the auth client (no bearer handler): these flows run signed out, and a stale session must
/// never be attached to them or trigger a token refresh.
/// </summary>
[BlazorService]
public class AccountService : IAccountService
{
    private readonly IHttpApiContextFactory apiFactory;
    private readonly IScopeLog log;

    public AccountService(IHttpApiContextFactory apiFactory, IScopeManager logManager)
    {
        this.apiFactory = apiFactory;
        log = logManager.CreateLogger(this);
    }

    private Task<ServiceActionResult<T>> PostAsync<T, TBody>(string action, TBody body) =>
        BlazorToolkit.Services.Wasm.ServiceUtils.HandleWebApiCallAsync<T>(async _ =>
            (await apiFactory.Create<T>(ApiClient.AuthHttpClientName, $"api/account/{action}")
                .Post<TBody>(body)
                .ExecuteAsync())!, log);

    public Task<ServiceActionResult<RegisterResult>> RegisterAsync(RegisterParameters input) =>
        PostAsync<RegisterResult, RegisterParameters>("register", input);

    public Task<ServiceActionResult<bool>> SendPasswordResetLinkAsync(ForgotPasswordParameters input) =>
        PostAsync<bool, ForgotPasswordParameters>("forgot-password", input);

    public Task<ServiceActionResult<bool>> ResetPasswordAsync(ResetPasswordParameters input) =>
        PostAsync<bool, ResetPasswordParameters>("reset-password", input);

    public Task<ServiceActionResult<ConfirmEmailResult>> ConfirmEmailAsync(ConfirmEmailRequest request) =>
        PostAsync<ConfirmEmailResult, ConfirmEmailRequest>("confirm-email", request);

    public Task<ServiceActionResult<bool>> IsSetupRequiredAsync() =>
        BlazorToolkit.Services.Wasm.ServiceUtils.HandleWebApiCallAsync<bool>(async _ =>
            await apiFactory.Create<bool>(ApiClient.AuthHttpClientName, "api/account/setup-required").Get().ExecuteAsync(), log);

    public Task<ServiceActionResult<bool>> SetInvitationPasswordAsync(InvitationPasswordRequest request) =>
        PostAsync<bool, InvitationPasswordRequest>("set-password", request);
}
