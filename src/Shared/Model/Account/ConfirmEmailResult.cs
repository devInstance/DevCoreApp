namespace DevInstance.DevCoreApp.Shared.Model.Account;

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

    public static ConfirmEmailResult AlreadyConfirmedResult(string userId) => new()
    {
        Succeeded = true,
        AlreadyConfirmed = true,
        UserId = userId
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
