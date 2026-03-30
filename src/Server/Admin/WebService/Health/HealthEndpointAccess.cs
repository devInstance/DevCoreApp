using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Health;

public class HealthEndpointAccess
{
    private readonly HealthEndpointSettings _settings;

    public HealthEndpointAccess(IOptions<HealthEndpointSettings> settings)
    {
        _settings = settings.Value;
    }

    public bool CanAccessReady(HttpContext context)
    {
        if (IsLocalRequest(context))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(_settings.ReadySharedSecret))
        {
            return false;
        }

        if (!context.Request.Headers.TryGetValue(_settings.ReadyHeaderName, out var headerValue))
        {
            return false;
        }

        return string.Equals(headerValue.ToString(), _settings.ReadySharedSecret, StringComparison.Ordinal);
    }

    private static bool IsLocalRequest(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp == null)
            return false;

        if (IPAddress.IsLoopback(remoteIp))
            return true;

        var localIp = context.Connection.LocalIpAddress;
        return localIp != null && remoteIp.Equals(localIp);
    }
}
