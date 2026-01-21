using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.WebServiceToolkit.Database.Queries;
using System;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface IUserProfilesQuery : IModelQuery<UserProfile, IUserProfilesQuery>, IQSearchable<IUserProfilesQuery>
{
    IQueryable<UserProfile> Select();

    IUserProfilesQuery ByLastName(string lastName);
    IUserProfilesQuery ByApplicationUserId(Guid id);
}
