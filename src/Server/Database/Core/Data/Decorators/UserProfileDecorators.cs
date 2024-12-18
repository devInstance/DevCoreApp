using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Model;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;

/// <summary>
/// Provides extension methods for converting <see cref="UserProfile"/> objects to view models.
/// </summary>
public static class UserProfileDecorators
{
    /// <summary>
    /// Converts a <see cref="UserProfile"/> to a <see cref="UserProfileItem"/> view model.
    /// </summary>
    /// <param name="profile">The user profile to convert.</param>
    /// <returns>A <see cref="UserProfileItem"/> view model or <c>null</c> if the profile is <c>null</c>.</returns>
    public static UserProfileItem ToView(this UserProfile profile)
    {
        if (profile == null) return null;

        return new UserProfileItem
        {
            Id = profile.PublicId,
            Name = profile.Name,
            Email = profile.Email,
            CreateDate = profile.CreateDate
        };
    }
}
