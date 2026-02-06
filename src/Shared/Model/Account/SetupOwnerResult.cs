using System.Collections.Generic;

namespace DevInstance.DevCoreApp.Shared.Model.Account;

public class SetupOwnerResult
{
    public bool Succeeded { get; set; }
    public bool UsersAlreadyExist { get; set; }
    public string? ErrorMessage { get; set; }

    public static SetupOwnerResult Success() => new() { Succeeded = true };

    public static SetupOwnerResult AlreadySetup() => new() { UsersAlreadyExist = true };

    public static SetupOwnerResult Failed(IEnumerable<string> errors) => new()
    {
        ErrorMessage = $"Error: {string.Join(", ", errors)}"
    };
}
