using DevInstance.BlazorToolkit.Http;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Client.Services.Core.Api;
using DevInstance.DevCoreApp.Client.Services.Core.Time;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.LogScope;
using DevInstance.DevCoreApp.Shared.Model.Core.Settings;

namespace DevInstance.DevCoreApp.Client.Services.Core.GridProfiles;

[BlazorService]
public class GridProfileService : ApiServiceBase, IGridProfileService
{
    public GridProfileService(IHttpApiContextFactory apiFactory, IScopeManager logManager) : base(apiFactory, logManager) { }

    public Task<ServiceActionResult<GridProfileItem?>> GetAsync(string gridName, string profileName = "Default") =>
        CallAsync<GridProfileItem?>(() => Api<GridProfileItem>($"api/grid-profiles/{Segment(gridName)}")
            .Get().Parameter("profileName", Segment(profileName)).ExecuteAsync());

    public Task<ServiceActionResult<GridProfileItem>> SaveAsync(GridProfileItem item) =>
        CallAsync(() => Api<GridProfileItem>("api/grid-profiles").Put(item).ExecuteAsync());
}
