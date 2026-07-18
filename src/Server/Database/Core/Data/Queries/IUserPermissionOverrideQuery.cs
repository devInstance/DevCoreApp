using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.WebServiceToolkit.Database.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface IUserPermissionOverrideQuery : IModelQuery<UserPermissionOverride, IUserPermissionOverrideQuery>
{
    IQueryable<UserPermissionOverride> Select();

    IUserPermissionOverrideQuery ByUserId(Guid userId);
    IUserPermissionOverrideQuery IncludePermission();

    /// <summary>
    /// Replaces all permission overrides for the user: removes existing rows and inserts
    /// the supplied overrides in a single save.
    /// </summary>
    Task ReplaceForUserAsync(Guid userId, IReadOnlyList<UserPermissionOverride> overrides);
}
