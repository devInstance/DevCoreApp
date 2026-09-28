using System.ComponentModel.DataAnnotations;

namespace DevInstance.DevCoreApp.Shared.Model.Core.Account;

/// <summary>
/// Body of <c>POST api/account/set-password</c>: an invited user choosing their first password.
/// Carries the link's <c>userId</c> and <c>code</c> because the endpoint is anonymous — the code
/// is what proves the caller received the invitation email.
/// </summary>
public class InvitationPasswordRequest : SetPasswordParameters
{
    [Required]
    public string UserId { get; set; } = "";

    /// <summary>The Base64Url-encoded token exactly as it appears in the link.</summary>
    [Required]
    public string Code { get; set; } = "";
}
