using DevInstance.DevCoreApp.Client.Net.Api;
using DevInstance.DevCoreApp.Shared.Model;

namespace DevInstance.DevCoreApp.Client.Net;

internal class HttpApiContextFactory
{
    public static IApiContext<T> Create<T>(HttpClient http, string url)
    {
        return new HttpApiContext<T>(url, http);
    }
}
