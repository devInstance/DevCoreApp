using Xunit;
using System;
using DevInstance.DevCoreApp.Server.Database.Core.Models;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators.Tests;

public class UserProfileDecoratorsTests
{
    [Fact]
    public void ToViewTest()
    {
        var TEST_TIME = new DateTime(2021, 8, 17, 21, 0, 0);
        UserProfile profile = new UserProfile()
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            PublicId = "Test",
            ApplicationUserId = Guid.NewGuid(),
            Status = UserStatus.LIVE,
            CreateDate = TEST_TIME,
            UpdateDate = TEST_TIME
        };

        var result = profile.ToView();

        Assert.NotNull(result);

        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal(TEST_TIME, result.CreateDate);
        Assert.Equal("Test", result.Id);
    }
}
