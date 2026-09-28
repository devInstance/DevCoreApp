using DevInstance.DevCoreApp.Client.Services.Core.Api;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DevInstance.DevCoreApp.Client.Services.Tests.Core;

public class ApiBaseAddressTests
{
    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    private static readonly IConfiguration DevConfig = Config(
        ("DevServers:0:Origin", "http://localhost:5280"),
        ("DevServers:0:ApiBaseUrl", "http://devcoreapp.dev.localhost:5211"),
        ("DevServers:1:Origin", "https://localhost:7280"),
        ("DevServers:1:ApiBaseUrl", "https://devcoreapp.dev.localhost:7082"));

    [Theory]
    [InlineData("http://localhost:5280/", "http://devcoreapp.dev.localhost:5211/")]
    [InlineData("https://localhost:7280/", "https://devcoreapp.dev.localhost:7082/")]
    [InlineData("http://localhost:5290/mobile/", "http://localhost:5290/")]
    public void DevServerOrigin_MapsToItsApi_OtherwiseSameOrigin(string appBase, string expected)
    {
        Assert.Equal(new Uri(expected), ApiBaseAddress.Resolve(DevConfig, appBase));
    }

    [Fact]
    public void HostedByApi_UsesOwnOrigin_WithoutBasePath()
    {
        var result = ApiBaseAddress.Resolve(DevConfig, "http://devcoreapp.dev.localhost:5211/mobile/");

        Assert.Equal(new Uri("http://devcoreapp.dev.localhost:5211/"), result);
    }

    [Fact]
    public void ApiBaseUrl_IsUsed_WhenNoDevServerMatches()
    {
        var config = Config(("ApiBaseUrl", "https://api.example.com"));

        Assert.Equal(new Uri("https://api.example.com"), ApiBaseAddress.Resolve(config, "https://app.example.com/"));
    }
}
