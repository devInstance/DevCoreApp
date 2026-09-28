using System.ComponentModel.DataAnnotations;

namespace DevInstance.DevCoreApp.Shared.Model.Core.Account;

/// <summary>
/// Body of <c>POST api/account/confirm-email</c>: the <c>userId</c> and <c>code</c> query
/// parameters of the emailed confirmation or invitation link, passed through unchanged.
/// </summary>
public class ConfirmEmailRequest
{
    [Required]
    public string UserId { get; set; } = "";

    /// <summary>The Base64Url-encoded token exactly as it appears in the link.</summary>
    [Required]
    public string Code { get; set; } = "";
}
