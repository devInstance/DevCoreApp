namespace DevInstance.DevCoreApp.Shared.Model.Core.Account;

public class ConfirmEmailResult
{
    public bool Succeeded { get; set; }
    public bool AlreadyConfirmed { get; set; }
    public bool NeedsPassword { get; set; }
    public string? UserId { get; set; }
    public string? ErrorMessage { get; set; }

    public static ConfirmEmailResult Success(string userId, bool needsPassword) => new()
    {
        Succeeded = true,
        UserId = userId,
        NeedsPassword = needsPassword
    };

    /// <summary>
    /// The address was already confirmed. <paramref name="needsPassword"/> still has to be reported:
    /// an invited user who opens the link a second time before choosing a password is confirmed but
    /// cannot sign in, and telling them "you can now log in" strands them.
    /// </summary>
    public static ConfirmEmailResult AlreadyConfirmedResult(string userId, bool needsPassword = false) => new()
    {
        Succeeded = true,
        AlreadyConfirmed = true,
        UserId = userId,
        NeedsPassword = needsPassword
    };

    public static ConfirmEmailResult InvalidLink() => new()
    {
        ErrorMessage = "Invalid confirmation link."
    };

    public static ConfirmEmailResult UserNotFound() => new()
    {
        ErrorMessage = "User not found."
    };

    public static ConfirmEmailResult Failed() => new()
    {
        ErrorMessage = "Error confirming your email. The link may have expired."
    };
}
