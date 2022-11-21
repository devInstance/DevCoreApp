using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data
{
    public interface IQueryRepository
    {
        IUserProfilesQuery GetUserProfilesQuery(UserProfile currentProfile);
    }
}
