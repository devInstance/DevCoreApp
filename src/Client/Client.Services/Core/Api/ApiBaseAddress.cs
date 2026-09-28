using Microsoft.Extensions.Configuration;

namespace DevInstance.DevCoreApp.Client.Services.Core.Api;

/// <summary>
/// Resolves the API origin a WASM client calls, from its <c>wwwroot/appsettings*.json</c>:
/// <list type="number">
/// <item><c>DevServers</c>: a list of <c>{ "Origin": …, "ApiBaseUrl": … }</c>. When the app is
/// served from one of those origins (its own dev server, <c>dotnet run</c> on the client project),
/// it calls that entry's API. Kept in <c>appsettings.Development.json</c>.</item>
/// <item><c>ApiBaseUrl</c>: a client deployed on another origin than the API.</item>
/// <item>Otherwise the origin the app is served from, i.e. hosted by the Api project.</item>
/// </list>
/// </summary>
/// <remarks>
/// The dev-server case is keyed by origin, not by environment, because the .NET 10 WASM SDK bakes
/// the environment into the build (<c>WasmApplicationEnvironmentName</c>, <c>Development</c> for
/// every Debug build) and ignores the launch profile's <c>ASPNETCORE_ENVIRONMENT</c>. The Api host
/// serves that same build, and there the app must call its own origin.
/// </remarks>
public static class ApiBaseAddress
{
    public static Uri Resolve(IConfiguration configuration, string appBaseAddress)
    {
        var appOrigin = new Uri(new Uri(appBaseAddress).GetLeftPart(UriPartial.Authority));

        foreach (var devServer in configuration.GetSection("DevServers").GetChildren())
        {
            var origin = devServer["Origin"];
            var apiBaseUrl = devServer["ApiBaseUrl"];
            if (!string.IsNullOrWhiteSpace(origin) && !string.IsNullOrWhiteSpace(apiBaseUrl)
                && Uri.Compare(new Uri(origin), appOrigin, UriComponents.SchemeAndServer,
                    UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return new Uri(apiBaseUrl);
            }
        }

        var configured = configuration["ApiBaseUrl"];
        return !string.IsNullOrWhiteSpace(configured) ? new Uri(configured) : appOrigin;
    }
}
