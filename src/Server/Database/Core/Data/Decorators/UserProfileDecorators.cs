using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Model;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;

public static class UserProfileDecorators
{
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