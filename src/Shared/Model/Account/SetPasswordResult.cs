using System.Collections.Generic;

namespace DevInstance.DevCoreApp.Shared.Model.Account;

public class SetPasswordResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }

    public static SetPasswordResult Success() => new() { Succeeded = true };

    public static SetPasswordResult Failed(IEnumerable<string> errors) => new()
    {
        ErrorMessage = $"Error: {string.Join(", ", errors)}"
    };
}
