using DevInstance.DevCoreApp.Shared.Model.Core.UserAdmin;
using Xunit;

namespace DevInstance.DevCoreApp.Server.Database.Core.UserAdmin.Tests;

public class UserStatusLabelsTests
{
    [Theory]
    [InlineData("INITIATED", "Invited")]
    [InlineData("LIVE", "Active")]
    [InlineData("SUSPENDED", "Suspended")]
    [InlineData("UNKNOWN", "Unknown")]
    public void maps_every_status_to_a_readable_label(string status, string expected)
    {
        Assert.Equal(expected, UserStatusLabels.For(status));
    }

    [Fact]
    public void tolerates_the_casing_a_mock_or_an_older_row_might_carry()
    {
        Assert.Equal("Active", UserStatusLabels.For("Live"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void treats_a_missing_value_as_unknown(string? status)
    {
        Assert.Equal("Unknown", UserStatusLabels.For(status));
    }

    [Fact]
    public void returns_an_unrecognised_value_unchanged_rather_than_blanking_it()
    {
        // Losing the value would hide exactly the case worth noticing — a status the UI does not know.
        Assert.Equal("ARCHIVED", UserStatusLabels.For("ARCHIVED"));
    }

    [Theory]
    [InlineData("LIVE", "UserStatus_Live")]
    [InlineData("INITIATED", "UserStatus_Initiated")]
    [InlineData(null, "UserStatus_Unknown")]
    public void builds_the_resource_key_a_localizer_will_look_up(string? status, string expected)
    {
        Assert.Equal(expected, UserStatusLabels.ResourceKey(status));
    }
}
