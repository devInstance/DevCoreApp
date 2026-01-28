using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.WebServiceToolkit.Database.Queries;
using System;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface IGridProfilesQuery : IModelQuery<GridProfile, IGridProfilesQuery>
{
    IQueryable<GridProfile> Select();

    IGridProfilesQuery ByUserProfileId(Guid userProfileId);
    IGridProfilesQuery ByGridName(string gridName);
    IGridProfilesQuery ByProfileName(string profileName);
    IGridProfilesQuery ByIsGlobal(bool isGlobal);
}
