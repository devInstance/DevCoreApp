namespace DevInstance.DevCoreApp.Server.Admin.WebService.Core.Hosting;

/// <summary>
/// Hosts the Blazor WebAssembly clients and configures CORS for clients served from elsewhere.
///
/// The server project references both client projects (the "hosted Blazor WebAssembly" model), so
/// their published files arrive as static web assets on every build, run and publish:
/// <list type="bullet">
/// <item><b>Desktop</b> (admin UI) at the site root <c>/</c>.</item>
/// <item><b>Mobile</b> at <c>/mobile</c> — its <c>StaticWebAssetBasePath</c> and
/// <c>&lt;base href="/mobile/"&gt;</c>.</item>
/// </list>
/// The files, including the fingerprinted and pre-compressed <c>_framework</c> runtime, are served by
/// <c>app.MapStaticAssets()</c>. (The older <c>UseBlazorFrameworkFiles</c> middleware must not be
/// added: it branches the pipeline for <c>/_framework</c> and short-circuits those endpoints.)
/// A client deployed to another origin instead (CDN, separate site) sets <c>ApiBaseUrl</c> in its
/// <c>wwwroot/appsettings.json</c>, and its origin goes in <c>Cors:AllowedOrigins</c>.
/// </summary>
public static class WasmClientHosting
{
    public const string CorsPolicy = "WasmClients";
    public const string CorsOriginsKey = "Cors:AllowedOrigins";
    public const string MobileBasePath = "/mobile";

    /// <summary>
    /// Registers the CORS policy for clients hosted on other origins. With no origins configured
    /// the policy allows none, which is the correct default for the same-origin hosted clients.
    /// </summary>
    public static IServiceCollection AddWasmClientCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection(CorsOriginsKey).Get<string[]>() ?? [];

        services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
        {
            if (origins.Length == 0)
            {
                return;
            }

            // No credentials: the clients authenticate with a bearer token, never a cookie.
            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }));

        return services;
    }

    /// <summary>
    /// Client-side routing: any non-file URL that no server endpoint claimed is a client route, so
    /// it gets that client's <c>index.html</c>. Map <b>after</b> every server endpoint.
    /// <c>/api</c>, <c>/hubs</c> and <c>/health</c> are excluded explicitly: an unknown API URL must
    /// be a 404, not a 200 HTML page where a client expects JSON.
    /// </summary>
    public static void MapWasmClients(this WebApplication app)
    {
        foreach (var serverPrefix in new[] { "/api", "/hubs", "/health" })
        {
            app.MapFallback($"{serverPrefix}/{{**path}}", context =>
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            });
        }

        app.MapFallbackToFile($"{MobileBasePath}/{{*path:nonfile}}", $"{MobileBasePath.TrimStart('/')}/index.html");
        app.MapFallbackToFile("{*path:nonfile}", "index.html");
    }
}
