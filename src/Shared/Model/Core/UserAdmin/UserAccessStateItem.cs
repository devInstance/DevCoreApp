namespace DevInstance.DevCoreApp.Shared.Model.Core.UserAdmin;

/// <summary>
/// Whether a user can actually sign in, and the invitation link that gets them there.
/// <para>
/// Both flags matter independently. Login requires a confirmed email
/// (<c>SignIn.RequireConfirmedAccount</c> is on) <i>and</i> a password, and an administrator
/// creating an account has no other way to see which half is outstanding.
/// </para>
/// </summary>
public class UserAccessStateItem
{
    public bool EmailConfirmed { get; set; }

    public bool HasPassword { get; set; }

    /// <summary>
    /// Absolute invitation URL, for the administrator to copy and deliver by hand. Null when the
    /// public origin cannot be resolved — configure <c>App:BaseUrl</c> in that case.
    /// </summary>
    public string? InvitationLink { get; set; }

    /// <summary>True once the account can be signed into unaided.</summary>
    public bool CanSignIn => EmailConfirmed && HasPassword;
}
