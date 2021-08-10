using DevInstance.SampleWebApp.Server.Database.Core.Models;
using DevInstance.SampleWebApp.Shared.Model;

namespace DevInstance.SampleWebApp.Server.Database.Core.Data.Decorators
{
    public static class UserProfileDecorators
    {
        public static UserProfileItem ToView(this UserProfile profile)
        {
            return new UserProfileItem
            {
                Id = profile.PublicId,
                Name = profile.Name,
                Email = profile.Email,
                CreateDate = profile.CreateDate
            };
        }
    }
}