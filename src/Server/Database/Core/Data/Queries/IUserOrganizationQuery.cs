using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.WebServiceToolkit.Database.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface IUserOrganizationQuery : IModelQuery<UserOrganization, IUserOrganizationQuery>
{
    IQueryable<UserOrganization> Select();

    IUserOrganizationQuery ByUserId(Guid userId);
    IUserOrganizationQuery IncludeOrganization();

    /// <summary>
    /// Replaces all organization assignments for the user: removes existing rows and
    /// inserts the supplied assignments in a single save.
    /// </summary>
    Task ReplaceForUserAsync(Guid userId, IReadOnlyList<UserOrganization> assignments);
}
