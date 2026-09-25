using DevInstance.LogScope;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Core.Hosting;

/// <summary>
/// Hosts the Blazor WASM clients (Desktop, Mobile) from this app and configures CORS for clients
/// served from another origin. Adapted from ThreadIQ's MobileClientHosting.
///
/// Each client is one setting, <c>WasmClients:{Name}:BasePath</c>:
/// <list type="bullet">
/// <item><c>"/mobile"</c> — the client is published into <c>wwwroot/mobile</c> and served at
/// <c>/mobile</c>. Same origin as the API: no CORS, no client-side API base URL.</item>
/// <item><c>"/"</c> — the client owns the site root (published into <c>wwwroot</c>). Only valid
/// once the Blazor Server UI is gone (WASM migration Phase 4) — until then it would collide
/// with it, so it is rejected.</item>
/// <item><c>""</c> or absent — this host does not serve that client. Deploying it elsewhere makes
/// it cross-origin: add its origin to <c>Cors:AllowedOrigins</c>.</item>
/// </list>
/// The value must match the <c>&lt;base href&gt;</c> in the client's <c>index.html</c>.
/// </summary>
public static class WasmClientHosting
{
    public const string CorsPolicy = "WasmClients";
    public const string CorsOriginsKey = "Cors:AllowedOrigins";

    private static readonly string[] ClientNames = ["Desktop", "Mobile"];

    /// <summary>
    /// Set to true when the Blazor Server UI no longer owns the site root, so a client may be
    /// hosted at "/". Flipped in WASM migration Phase 4.
    /// </summary>
    private static readonly bool SiteRootAvailable = false;

    /// <summary>
    /// Registers the CORS policy for clients hosted on other origins. With no origins
    /// configured the policy allows none, which is the correct same-origin default.
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
    /// Serves each configured client's <c>_framework</c> files. Must run <b>before</b>
    /// <c>UseStaticFiles</c>: it supplies the right content types (including the ICU <c>.dat</c>
    /// bundles) and negotiates the pre-compressed <c>.br</c>/<c>.gz</c> siblings.
    /// </summary>
    public static void UseWasmClients(this WebApplication app)
    {
        foreach (var (name, basePath) in ResolveClients(app.Configuration))
        {
            WarnIfNotStaged(app, name, basePath);
            app.UseBlazorFrameworkFiles(basePath == "/" ? default : new PathString(basePath));
        }
    }

    /// <summary>
    /// Client-side routing: any non-file URL under a client's base path returns its
    /// <c>index.html</c> so the WASM router resolves it. Map <b>after</b> controllers and hubs.
    /// A client at the site root must not swallow unknown <c>/api</c> URLs (a 200 HTML page where
    /// a client expects JSON or 404), so those get an explicit 404 first.
    /// </summary>
    public static void MapWasmClients(this WebApplication app)
    {
        foreach (var (_, basePath) in ResolveClients(app.Configuration))
        {
            if (basePath == "/")
            {
                app.MapFallback("/api/{**path}", context =>
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return Task.CompletedTask;
                });
                app.MapFallbackToFile("{*path:nonfile}", "index.html");
            }
            else
            {
                app.MapFallbackToFile($"{basePath}/{{*path:nonfile}}", $"{basePath.TrimStart('/')}/index.html");
            }
        }
    }

    /// <summary>
    /// The configured clients with normalized base paths ("/mobile", or "/" for the root).
    /// </summary>
    public static IEnumerable<(string Name, string BasePath)> ResolveClients(IConfiguration configuration)
    {
        foreach (var name in ClientNames)
        {
            var key = $"WasmClients:{name}:BasePath";
            var configured = configuration[key];
            if (string.IsNullOrWhiteSpace(configured))
            {
                continue;
            }

            var trimmed = configured.Trim().Trim('/');
            if (trimmed.IndexOfAny(['?', '#', '\\', ' ']) >= 0)
            {
                throw new InvalidOperationException($"{key} must be a plain path such as \"/mobile\"; got \"{configured}\".");
            }

            if (trimmed.Length == 0 && !SiteRootAvailable)
            {
                throw new InvalidOperationException(
                    $"{key} cannot be \"/\" yet: the Blazor Server admin UI still owns the site root. " +
                    "Use a sub-path such as \"/desktop\" until the WASM migration cut-over.");
            }

            yield return (name, "/" + trimmed);
        }
    }

    /// <summary>
    /// A configured client with nothing published 404s every URL, and nothing else at startup
    /// would say so. Warn rather than throw: running the host without staging a client is a
    /// normal development state.
    /// </summary>
    private static void WarnIfNotStaged(WebApplication app, string name, string basePath)
    {
        var webRoot = app.Environment.WebRootPath;
        if (string.IsNullOrEmpty(webRoot))
        {
            return;
        }

        var folder = basePath.Trim('/').Replace('/', Path.DirectorySeparatorChar);
        var index = Path.Combine(webRoot, folder, "index.html");
        if (!File.Exists(index))
        {
            var log = app.Services.GetRequiredService<IScopeManager>().CreateLogger(typeof(WasmClientHosting));
            log.W($"WasmClients:{name}:BasePath is \"{basePath}\" but no client is published at {index}; every {basePath} URL will 404.");
        }
    }
}
