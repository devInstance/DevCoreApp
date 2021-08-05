using DevInstance.SampleWebApp.Server.Database.Core.Data.Queries;
using DevInstance.SampleWebApp.Server.Database.Core.Models;

namespace DevInstance.SampleWebApp.Server.Database.Core.Data
{
    public interface IQueryRepository
    {
        IUserProfilesQuery GetUserProfilesQuery(UserProfile currentProfile);
    }
}
