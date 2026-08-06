using System.Collections.Generic;

namespace DevInstance.DevCoreApp.Shared.Model.Core.Account;

public class ResetPasswordResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }

    public static ResetPasswordResult Success() => new() { Succeeded = true };

    public static ResetPasswordResult Failed(IEnumerable<string> errors) => new()
    {
        ErrorMessage = $"Error: {string.Join(", ", errors)}"
    };
}
