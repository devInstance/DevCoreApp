using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.WebServiceToolkit.Database.Queries;
using System.Collections.Generic;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface IPermissionQuery : IModelQuery<Permission, IPermissionQuery>
{
    IQueryable<Permission> Select();

    IPermissionQuery ByKeys(IEnumerable<string> keys);
    IPermissionQuery OrderedByDisplayOrder();
}
