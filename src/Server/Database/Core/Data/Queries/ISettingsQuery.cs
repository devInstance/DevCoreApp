using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.WebServiceToolkit.Database.Queries;
using System;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface ISettingsQuery : IModelQuery<Setting, ISettingsQuery>,
        IQSearchable<ISettingsQuery>,
        IQPageable<ISettingsQuery>,
        IQSortable<ISettingsQuery>
{
    IQueryable<Setting> Select();

    ISettingsQuery ByCategory(string category);
    ISettingsQuery ByCategoryAndKey(string category, string key);
    ISettingsQuery ByTenantId(Guid? tenantId);
    ISettingsQuery ByOrganizationId(Guid? organizationId);
    ISettingsQuery ByUserId(Guid? userId);
    ISettingsQuery SystemOnly();
}
