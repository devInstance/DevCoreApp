using DevInstance.DevCoreApp.Shared.Utils.Core.Json;
using DevInstance.WebServiceToolkit.Controllers;
using DevInstance.WebServiceToolkit.Http.Query;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace DevInstance.DevCoreApp.Server.Api.Core.Controllers;

/// <summary>
/// MVC configuration for the <c>/api</c> wire contract (docs/WasmMigrationPlan.md, D2 and D6).
/// One place so <c>Program.cs</c> and the API contract tests run the same setup.
/// </summary>
public static class ApiConfigurationExtensions
{
    public static IMvcBuilder AddApiControllers(this IServiceCollection services)
    {
        return services.AddControllers(options =>
            {
                // A null result is JSON "null" with 200, not an empty 204 — BlazorToolkit clients
                // deserialize every success body, and an empty one fails to parse.
                options.OutputFormatters.RemoveType<HttpNoContentOutputFormatter>();
            })
            // Every DateTime on the wire is UTC ("...Z"); clients convert to local for display.
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter()))
            // [QueryModel] list queries (ListQuery and friends) bind from the query string.
            .AddWebServiceToolkitQuery()
            // Model-validation 400s use the same WebServiceError body as every other API error.
            .AddWebServiceToolkitErrors();
    }
}
