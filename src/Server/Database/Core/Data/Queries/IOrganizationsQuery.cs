using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.WebServiceToolkit.Database.Queries;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface IOrganizationsQuery : IModelQuery<Organization, IOrganizationsQuery>,
        IQSearchable<IOrganizationsQuery>,
        IQPageable<IOrganizationsQuery>,
        IQSortable<IOrganizationsQuery>
{
    IQueryable<Organization> Select();
    IOrganizationsQuery ByParentId(Guid? parentId);
    IOrganizationsQuery ByPathPrefix(string pathPrefix);
    IOrganizationsQuery ByPublicIds(IEnumerable<string> publicIds);
    IOrganizationsQuery ByType(string type);
    IOrganizationsQuery ByIsActive(bool isActive);
    IOrganizationsQuery ByLevel(int level);
    IOrganizationsQuery ByCode(string code);
}
