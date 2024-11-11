using DevInstance.DevCoreApp.Server.Database.Core.Models;
using System;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface IUserProfilesQuery : IModelQuery<UserProfile, IUserProfilesQuery>, IQSearchable<IUserProfilesQuery>
{
    IQueryable<UserProfile> Select();

    IUserProfilesQuery ByName(string name);
    IUserProfilesQuery ByApplicationUserId(Guid id);
}
