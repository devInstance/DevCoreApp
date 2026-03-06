using System;
using System.Linq;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.WebServiceToolkit.Database.Queries;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface IFeatureFlagQuery : IModelQuery<FeatureFlag, IFeatureFlagQuery>,
        IQSearchable<IFeatureFlagQuery>,
        IQPageable<IFeatureFlagQuery>,
        IQSortable<IFeatureFlagQuery>
{
    IQueryable<FeatureFlag> Select();

    IFeatureFlagQuery ByName(string name);
    IFeatureFlagQuery ByOrganizationId(Guid organizationId);
    IFeatureFlagQuery GlobalOnly();
    IFeatureFlagQuery ByNameForEvaluation(string name);
}
