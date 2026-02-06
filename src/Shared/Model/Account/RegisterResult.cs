using System.Collections.Generic;

namespace DevInstance.DevCoreApp.Shared.Model.Account;

public class RegisterResult
{
    public bool Succeeded { get; set; }
    public bool RequiresEmailConfirmation { get; set; }
    public string? ErrorMessage { get; set; }

    public static RegisterResult Success(bool requiresConfirmation) => new()
    {
        Succeeded = true,
        RequiresEmailConfirmation = requiresConfirmation
    };

    public static RegisterResult Failed(IEnumerable<string> errors) => new()
    {
        ErrorMessage = $"Error: {string.Join(", ", errors)}"
    };
}
