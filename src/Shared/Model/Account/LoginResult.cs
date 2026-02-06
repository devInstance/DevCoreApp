namespace DevInstance.DevCoreApp.Shared.Model.Account;

public class LoginResult
{
    public bool Succeeded { get; set; }
    public bool IsLockedOut { get; set; }
    public string? ErrorMessage { get; set; }

    public static LoginResult Success() => new() { Succeeded = true };
    public static LoginResult LockedOut() => new() { IsLockedOut = true, ErrorMessage = "Error: Account is locked out. Please try again later." };
    public static LoginResult InvalidLogin() => new() { ErrorMessage = "Error: Invalid login attempt." };
}
