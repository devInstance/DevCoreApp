namespace DevInstance.DevCoreApp.Server.Api.Core.Health;

public class HealthEndpointSettings
{
    public const string SectionName = "HealthEndpoints";

    public string ReadyHeaderName { get; set; } = "X-Health-Key";
    public string? ReadySharedSecret { get; set; }
}
