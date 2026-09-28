using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.WebServiceToolkit.Database.Queries;
using System;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface IUserProfilesQuery : IModelQuery<UserProfile, IUserProfilesQuery>, 
        IQSearchable<IUserProfilesQuery>, 
        IQPageable<IUserProfilesQuery>, 
        IQSortable<IUserProfilesQuery>
{
    IQueryable<UserProfile> Select();

    IUserProfilesQuery ByLastName(string lastName);
    IUserProfilesQuery ById(Guid id);
    IUserProfilesQuery ByApplicationUserId(Guid id);
    IUserProfilesQuery ByOrganizationId(Guid organizationId);

    /// <summary>
    /// Only profiles whose Identity user exists. <c>ApplicationUserId</c> has no foreign key, so a
    /// profile outlives an <c>AspNetUsers</c> row deleted outside the application. Apply it before
    /// counting or paging a list that shows accounts, or the count and the pages disagree.
    /// </summary>
    IUserProfilesQuery WithApplicationUser();
}
