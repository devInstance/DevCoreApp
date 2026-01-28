using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Model;
using System.Collections.Generic;

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
    /// <param name="appUser">The application user associated with the profile.</param>
    /// <param name="roles">The roles assigned to the user.</param>
    /// <returns>A <see cref="UserProfileItem"/> view model or <c>null</c> if the profile is <c>null</c>.</returns>
    public static UserProfileItem ToView(this UserProfile profile, ApplicationUser? appUser = null, IList<string>? roles = null)
    {
        return new UserProfileItem
        {
            Id = profile.PublicId,
            Email = appUser.Email ?? string.Empty,
            FirstName = profile.FirstName ?? string.Empty,
            MiddleName = profile.MiddleName ?? string.Empty,
            LastName = profile.LastName ?? string.Empty,
            PhoneNumber = profile.PhoneNumber ?? string.Empty,
            Roles = roles != null ? string.Join(", ", roles) : string.Empty,
            Status = profile.Status.ToString(),
            CreateDate = profile.CreateDate,
            UpdateDate = profile.UpdateDate
        };
    }

    public static UserProfile ToRecord(this UserProfile profile, UserProfileItem newProfile)
    {
        profile.Email = newProfile.Email;
        profile.FirstName = newProfile.FirstName;
        profile.MiddleName = newProfile.MiddleName;
        profile.LastName = newProfile.LastName;
        profile.PhoneNumber = newProfile.PhoneNumber;

        return profile;
    }

}
