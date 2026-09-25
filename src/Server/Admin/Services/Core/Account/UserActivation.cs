using DevInstance.DevCoreApp.Server.Database.Core.Models;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Core.Account;

/// <summary>
/// When a <see cref="UserProfile"/> becomes <see cref="UserStatus.LIVE"/>.
/// <para>
/// Kept as a pure predicate over already-loaded values so it is unit-testable without a database
/// (see the testing notes in the root CLAUDE.md). The two callers — the invited user setting their
/// own password, and an administrator setting it for them — do their own I/O around it.
/// </para>
/// </summary>
public static class UserActivation
{
    /// <summary>
    /// True when the profile should be moved to <see cref="UserStatus.LIVE"/>.
    /// <para>
    /// Both conditions are required because both gate sign-in independently:
    /// <c>SignIn.RequireConfirmedAccount</c> blocks an unconfirmed address, and a user with no
    /// password has nothing to sign in with. "Usable" is the only thing LIVE has ever been intended
    /// to mean.
    /// </para>
    /// <para>
    /// <see cref="UserStatus.SUSPENDED"/> is deliberately <b>not</b> promoted. Nothing writes it
    /// today, but if suspension is ever implemented, confirming an address or resetting a password
    /// must not quietly lift it.
    /// </para>
    /// </summary>
    public static bool ShouldActivate(UserStatus current, bool emailConfirmed, bool hasPassword)
    {
        if (!emailConfirmed || !hasPassword)
        {
            return false;
        }

        return current == UserStatus.INITIATED || current == UserStatus.UNKNOWN;
    }
}
