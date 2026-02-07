using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.Admin.WebService.Authentication;
using Moq;
using System;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Tests;

public class ServerTestUtils
{

    public static Mock<IApplicationSignManager> CreateSignManager(bool succeeded)
    {
        var signinManagerMq = new Mock<IApplicationSignManager>();
        signinManagerMq.Setup(x => x.SignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns<ApplicationUser, string, bool>((x, y, z) => Task.FromResult((Microsoft.AspNetCore.Identity.SignInResult)new SignInResultMock(succeeded)));

        return signinManagerMq;
    }

    public static IAuthorizationContext SetupAuthContext()
    {
        return SetupAuthContextMock().Object;
    }

    public static Mock<IAuthorizationContext> SetupAuthContextMock()
    {
        var currentProfile = new UserProfile { };
        var mockAuthContext = new Mock<IAuthorizationContext>();
        mockAuthContext.Setup(x => x.CurrentProfile).Returns(currentProfile);
        //mockAuthContext.Setup(x => x.User).Returns(null);
        return mockAuthContext;
    }

}
