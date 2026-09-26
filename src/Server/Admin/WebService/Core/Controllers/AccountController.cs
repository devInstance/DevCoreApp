using DevInstance.DevCoreApp.Server.Admin.Services.Core;
using DevInstance.DevCoreApp.Shared.Model.Core.Account;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Model.Core.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Core.Controllers;

/// <summary>
/// Anonymous account flows. Sign-in is <c>api/auth</c> (JWT); nothing here issues a cookie.
/// The confirm-email and set-password bodies carry the <c>userId</c>/<c>code</c> from the
/// emailed link, which the client reads from its route.
/// </summary>
[Route("api/account")]
[AllowAnonymous]
public class AccountController : ApiControllerBase
{
    private readonly IAccountService _service;

    public AccountController(IAccountService service) => _service = service;

    [HttpPost("register")]
    public Task<ActionResult<RegisterResult>> RegisterAsync([FromBody] RegisterParameters input)
        => HandleServiceAsync(() => _service.RegisterAsync(input));

    [HttpPost("forgot-password")]
    public Task<ActionResult<bool>> ForgotPasswordAsync([FromBody] ForgotPasswordParameters input)
        => HandleServiceAsync(() => _service.SendPasswordResetLinkAsync(input));

    [HttpPost("reset-password")]
    public Task<ActionResult<bool>> ResetPasswordAsync([FromBody] ResetPasswordParameters input)
        => HandleServiceAsync(() => _service.ResetPasswordAsync(input));

    [HttpPost("confirm-email")]
    public Task<ActionResult<ConfirmEmailResult>> ConfirmEmailAsync([FromBody] ConfirmEmailRequest request)
        => HandleServiceAsync(() => _service.ConfirmEmailAsync(request));

    /// <summary>True until the owner account exists; the client then links to the <c>/setup</c> page.</summary>
    [HttpGet("setup-required")]
    public Task<ActionResult<bool>> IsSetupRequiredAsync()
        => HandleServiceAsync(() => _service.IsSetupRequiredAsync());

    [HttpPost("set-password")]
    public Task<ActionResult<bool>> SetPasswordAsync([FromBody] InvitationPasswordRequest request)
        => HandleServiceAsync(() => _service.SetInvitationPasswordAsync(request));
}
