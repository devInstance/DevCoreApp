using DevInstance.BlazorToolkit.Http;
using DevInstance.DevCoreApp.Client.Services.Core.Api;
using DevInstance.DevCoreApp.Shared.Model.Core;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using Xunit;

namespace DevInstance.DevCoreApp.Client.Services.Tests.Core;

public class ApiQueryExtensionsTests
{
    private static string BuildUri(object query)
    {
        var factory = new HttpApiContextFactory(null!);
        return factory.Create<EmailLogItem>(new HttpClient(), "api/email-logs").Get().Query(query).Uri;
    }

    [Fact]
    public void list_query_becomes_escaped_query_string()
    {
        var uri = BuildUri(new ListQuery { Top = 10, Page = 2, SortBy = new[] { "-CreateDate", "Name" }, Search = "a&b c" });

        Assert.Contains("top=10", uri);
        Assert.Contains("page=2", uri);
        Assert.Contains("sortBy=-CreateDate,Name", uri);
        Assert.Contains("search=a%26b%20c", uri);
    }

    [Fact]
    public void computed_properties_and_nulls_are_left_out()
    {
        var uri = BuildUri(new ListQuery { SortBy = new[] { "-Name" } });

        Assert.DoesNotContain("sortField", uri);
        Assert.DoesNotContain("isAsc", uri);
        Assert.DoesNotContain("search", uri);
    }

    [Fact]
    public void dates_are_sent_as_utc_iso_8601()
    {
        var uri = BuildUri(new EmailLogQuery
        {
            StartDate = new DateTime(2026, 9, 25, 14, 30, 0, DateTimeKind.Utc),
            Status = 3,
        });

        Assert.Contains("startDate=2026-09-25T14%3A30%3A00.0000000Z", uri);
        Assert.Contains("status=3", uri);
    }

    [Fact]
    public void local_dates_are_converted_to_utc()
    {
        var local = new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Local);

        var formatted = Uri.UnescapeDataString(ApiQueryExtensions.Format(local)!);

        Assert.Equal(local.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'"), formatted);
    }

    [Fact]
    public void values_are_culture_invariant()
    {
        var previous = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            Assert.Equal("1.5", ApiQueryExtensions.Format(1.5m));
            Assert.Equal("true", ApiQueryExtensions.Format(true));
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }
}
