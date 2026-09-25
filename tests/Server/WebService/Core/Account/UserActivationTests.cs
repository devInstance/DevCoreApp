using DevInstance.DevCoreApp.Server.Admin.Services.Core.Account;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using Xunit;

namespace DevInstance.DevCoreApp.Server.Tests.Core.Account;

public class UserActivationTests
{
    [Fact]
    public void activates_an_invited_user_once_confirmed_and_password_set()
    {
        Assert.True(UserActivation.ShouldActivate(UserStatus.INITIATED, emailConfirmed: true, hasPassword: true));
    }

    [Fact]
    public void activates_a_profile_left_at_the_default_unknown()
    {
        Assert.True(UserActivation.ShouldActivate(UserStatus.UNKNOWN, emailConfirmed: true, hasPassword: true));
    }

    [Theory]
    [InlineData(false, true)]   // confirmed the address but never chose a password
    [InlineData(true, false)]   // has a password but RequireConfirmedAccount still blocks sign-in
    [InlineData(false, false)]
    public void does_not_activate_until_the_account_can_actually_sign_in(bool emailConfirmed, bool hasPassword)
    {
        Assert.False(UserActivation.ShouldActivate(UserStatus.INITIATED, emailConfirmed, hasPassword));
    }

    [Fact]
    public void leaves_an_already_live_profile_alone()
    {
        Assert.False(UserActivation.ShouldActivate(UserStatus.LIVE, emailConfirmed: true, hasPassword: true));
    }

    [Fact]
    public void never_lifts_a_suspension()
    {
        // A password reset or a re-confirmation must not quietly reinstate a suspended account.
        Assert.False(UserActivation.ShouldActivate(UserStatus.SUSPENDED, emailConfirmed: true, hasPassword: true));
    }
}
