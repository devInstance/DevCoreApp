using Microsoft.VisualStudio.TestTools.UnitTesting;
using DevInstance.DevCoreApp.Shared.TestUtils;
using DevInstance.DevCoreApp.Server.Tests;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using Moq;
using DevInstance.DevCoreApp.Server.EmailProcessor;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Exceptions;
using System.Threading.Tasks;
using DevInstance.DevCoreApp.Server.WebService.Indentity;
using System.Security.Claims;

namespace DevInstance.DevCoreApp.Server.Services.Tests
{
    [TestClass()]
    public class AuthorizationServiceTests
    {
        #region Setup
        private delegate void OnSetupMock(Mock<IAuthorizationContext> authContext,
                                            Mock<IApplicationUserManager> userManager,
                                            Mock<IApplicationSignManager> singInManager,
                                            Mock<IEmailSender> emailSender,
                                            Mock<IQueryRepository> mockRepository);

        private AuthorizationService SetupServiceMock(OnSetupMock onSetup)
        {
            var log = new IScopeManagerMock();
            var authContext = ServerTestUtils.SetupAuthContextMock();
            var userManager = new Mock<IApplicationUserManager>();
            var signinManagerMq = ServerTestUtils.CreateSignManager(true);
            var emailSender = new Mock<IEmailSender>();
            var mockRepository = new Mock<IQueryRepository>();
            var timeProvider = TimerProviderMock.CreateTimerProvider();

            onSetup(authContext, userManager, signinManagerMq, emailSender, mockRepository);

            return new AuthorizationService(log,
                                            timeProvider,
                                            mockRepository.Object,
                                            userManager.Object,
                                            signinManagerMq.Object,
                                            authContext.Object,
                                            emailSender.Object);
        }
        #endregion

        [TestMethod()]
        [DataRow(false)]
        [DataRow(true)]
        public async Task LoginSuccessTest(bool existingProfile)
        {
            Mock<IAuthorizationContext> authContextMock = null;
            Mock<IUserProfilesQuery> mockSelect = null;

            var authorizationService =
                SetupServiceMock((authContext, userManager, signinManagerMq, emailSender, mockRepository) =>
            {
                authContextMock = authContext;
                // returns existing profile
                authContext.Setup(x => x.FindUserProfile(It.IsAny<ApplicationUser>())).Returns(existingProfile ? new UserProfile { Name = "test" } : null);

                userManager.Setup(x => x.FindByNameAsync(It.Is<string>(a => a == "test"))).ReturnsAsync(new ApplicationUser { UserName = "test" });

                mockSelect = new Mock<IUserProfilesQuery>();
                mockSelect.Setup(x => x.CreateNew()).Returns(new UserProfile { Email = "test" });

                mockRepository.Setup(x => x.GetUserProfilesQuery(It.IsAny<UserProfile>())).Returns(mockSelect.Object);
            });

            var result = await authorizationService.Login(new LoginParameters
            {
                UserName = "test",
                Password = "test"
            });

            authContextMock.Verify(mock => mock.ResetCurrentProfile(), Times.Once());
            mockSelect.Verify(mock => mock.Add(It.IsAny<UserProfile>()), existingProfile ? Times.Never() : Times.Once());

            Assert.IsTrue(result);
        }

        [TestMethod()]
        public async Task LoginFailedUserNameDoesntExistTest()
        {
            var authorizationService =
                SetupServiceMock((authContext, userManager, signinManagerMq, emailSender, mockRepository) =>
                {
                    userManager.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);

                    var mockSelect = new Mock<IUserProfilesQuery>();

                    mockRepository.Setup(x => x.GetUserProfilesQuery(It.IsAny<UserProfile>())).Returns(mockSelect.Object);

                });

            await Assert.ThrowsExceptionAsync<UnauthorizedException>(async () =>
            {
                await authorizationService.Login(new LoginParameters
                {
                    UserName = "test",
                    Password = "test"
                });
            });
        }

        [TestMethod()]
        public async Task LoginFailedInvalidPasswordTest()
        {
            var authorizationService =
                SetupServiceMock((authContext, userManager, signinManagerMq, emailSender, mockRepository) =>
                {
                    userManager.Setup(x => x.FindByNameAsync(It.Is<string>(a => a == "test"))).ReturnsAsync(new ApplicationUser { UserName = "test" });

                    signinManagerMq.Setup(x => x.SignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(new SignInResultMock(false));

                    var mockSelect = new Mock<IUserProfilesQuery>();

                    mockRepository.Setup(x => x.GetUserProfilesQuery(It.IsAny<UserProfile>())).Returns(mockSelect.Object);

                });

            await Assert.ThrowsExceptionAsync<UnauthorizedException>(async () =>
            {
                await authorizationService.Login(new LoginParameters
                {
                    UserName = "test",
                    Password = "test"
                });
            });
        }

        [TestMethod()]
        public async Task LogoutTest()
        {
            Mock<IAuthorizationContext> authContextMock = null;
            Mock<IApplicationSignManager> singInManagerMock = null;

            var authorizationService =
                SetupServiceMock((authContext, userManager, signinManagerMq, emailSender, mockRepository) =>
                {
                    authContextMock = authContext;
                    singInManagerMock = signinManagerMq;

                    var mockSelect = new Mock<IUserProfilesQuery>();
                    mockRepository.Setup(x => x.GetUserProfilesQuery(It.IsAny<UserProfile>())).Returns(mockSelect.Object);
                });

            var result = await authorizationService.Logout();

            authContextMock.Verify(mock => mock.ResetCurrentProfile(), Times.Once());
            singInManagerMock.Verify(x => x.SignOutAsync(), Times.Once());

            Assert.IsTrue(result);
        }

        [TestMethod()]
        public async Task RegisterSuccessTest()
        {
            Mock<IApplicationUserManager> userManagerMock = null;
            var authorizationService =
                SetupServiceMock((authContext, userManager, signinManagerMq, emailSender, mockRepository) =>
                {
                    authContext.Setup(x => x.FindUserProfile(It.IsAny<ApplicationUser>())).Returns(new UserProfile { Name = "test" });

                    userManagerMock = userManager;
                    userManagerMock.Setup(x => x.FindByNameAsync(It.Is<string>(a => a == "test"))).ReturnsAsync(new ApplicationUser { UserName = "test" });
                    userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(new IdentityResultMock(true));
                });

            var result = await authorizationService.Register(new RegisterParameters {
                UserName = "test",
                Password = "password",
                PasswordConfirm = "password"
            });

            Assert.IsTrue(result);

        }

        [TestMethod()]
        public async Task RegisterFailedTest()
        {
            Mock<IApplicationUserManager> userManagerMock = null;
            var authorizationService =
                SetupServiceMock((authContext, userManager, signinManagerMq, emailSender, mockRepository) =>
                {
                    userManagerMock = userManager;
                    userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(new IdentityResultMock(false));
                });

            await Assert.ThrowsExceptionAsync<BadRequestException>(async () =>
            {
                await authorizationService.Register(new RegisterParameters
                {
                    UserName = "test",
                    Password = "password",
                    PasswordConfirm = "password"
                });
            });
        }

        [TestMethod()]
        public async Task DeleteSuccessTest()
        {
            Mock<IAuthorizationContext> authContextMock = null;
            Mock<IApplicationUserManager> userManagerMock = null;

            var authorizationService =
                SetupServiceMock((authContext, userManager, signinManagerMq, emailSender, mockRepository) =>
                {
                    authContextMock = authContext;
                    userManagerMock = userManager;
                    userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(new ApplicationUser { UserName = "test" });
                });

            var result = await authorizationService.Delete();

            authContextMock.Verify(mock => mock.ResetCurrentProfile(), Times.Once());
            userManagerMock.Verify(x => x.DeleteAsync(It.IsAny<ApplicationUser>()), Times.Once());

            Assert.IsTrue(result);
        }

        [TestMethod()]
        public async Task DeleteFailedTest()
        {
            var authorizationService =
                SetupServiceMock((authContext, userManager, signinManagerMq, emailSender, mockRepository) =>
                {
                    userManager.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);
                });

            await Assert.ThrowsExceptionAsync<UnauthorizedException>(async () =>
            {
                await authorizationService.Delete();
            });
        }

        [TestMethod()]
        public async Task ChangePasswordSuccessTest()
        {
            Mock<IApplicationUserManager> userManagerMock = null;

            var authorizationService =
                SetupServiceMock((authContext, userManager, signinManagerMq, emailSender, mockRepository) =>
                {
                    userManagerMock = userManager;
                    userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("test");
                    userManagerMock.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(new ApplicationUser { UserName = "test" });
                    userManagerMock.Setup(x => x.ChangePasswordAsync(It.IsAny<ApplicationUser>(),
                                                                It.Is<string>(v => v == "test"),
                                                                It.Is<string>(v => v == "test"))).ReturnsAsync(new IdentityResultMock(true));
                });

            var result = await authorizationService.ChangePassword(new ChangePasswordParameters { 
                OldPassword = "test",
                NewPassword = "test",
                NewPasswordConfirm = "test"
            });

            userManagerMock.Verify(x => x.ChangePasswordAsync(It.IsAny<ApplicationUser>(),
                                                                It.Is<string>(v => v == "test"),
                                                                It.Is<string>(v => v == "test")),
                                                                Times.Once());

            Assert.IsTrue(result);
        }

        [TestMethod()]
        public async Task ChangePasswordFailedUserNotFoundTest()
        {
            Mock<IApplicationUserManager> userManagerMock = null;

            var authorizationService =
                SetupServiceMock((authContext, userManager, signinManagerMq, emailSender, mockRepository) =>
                {
                    userManagerMock = userManager;
                    userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("test");
                    userManagerMock.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);
                });

            await Assert.ThrowsExceptionAsync<UnauthorizedException>(async () =>
            {
                await authorizationService.ChangePassword(new ChangePasswordParameters
                {
                    OldPassword = "test",
                    NewPassword = "test",
                    NewPasswordConfirm = "test"
                });
            });
        }

        [TestMethod()]
        public async Task ChangePasswordFailedTest()
        {
            Mock<IApplicationUserManager> userManagerMock = null;

            var authorizationService =
                SetupServiceMock((authContext, userManager, signinManagerMq, emailSender, mockRepository) =>
                {
                    userManagerMock = userManager;
                    userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("test");
                    userManagerMock.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(new ApplicationUser { UserName = "test" });
                    userManagerMock.Setup(x => x.ChangePasswordAsync(It.IsAny<ApplicationUser>(),
                                                                It.Is<string>(v => v == "test"),
                                                                It.Is<string>(v => v == "test"))).ReturnsAsync(new IdentityResultMock(false));
                });

            await Assert.ThrowsExceptionAsync<UnauthorizedException>(async () =>
            {
                await authorizationService.ChangePassword(new ChangePasswordParameters
                {
                    OldPassword = "test",
                    NewPassword = "test",
                    NewPasswordConfirm = "test"
                });
            });

            userManagerMock.Verify(x => x.ChangePasswordAsync(It.IsAny<ApplicationUser>(),
                                                                It.Is<string>(v => v == "test"),
                                                                It.Is<string>(v => v == "test")),
                                                                Times.Once());
        }

        [TestMethod()]
        public async Task ForgotPasswordSuccessTest()
        {
            Mock<IApplicationUserManager> userManagerMock = null;
            Mock<IEmailSender> emailSenderMock = null;

            var authorizationService =
                SetupServiceMock((authContext, userManager, signinManagerMq, emailSender, mockRepository) =>
                {
                    userManagerMock = userManager;
                    userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(new ApplicationUser { UserName = "test@test.com" });
                    userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(It.IsAny<ApplicationUser>()))
                                    .ReturnsAsync("ttttokenxxx");
                    emailSenderMock = emailSender;
                    emailSender.Setup(x => x.SendAsync(It.IsAny<IEmailMessage>()));
                });

            await authorizationService.ForgotPasswordAsync(new ForgotPasswordParameters
            {
                Email = "test@test.com"
            }, "https", "test.com", 8080);

            userManagerMock.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Once());
            userManagerMock.Verify(x => x.GeneratePasswordResetTokenAsync(It.IsAny<ApplicationUser>()), Times.Once());
            emailSenderMock.Verify(x => x.SendAsync(It.IsAny<IEmailMessage>()), Times.Once());
        }

        [TestMethod()]
        public async Task ForgotPasswordEmailDoesntExistTest()
        {
            Mock<IApplicationUserManager> userManagerMock = null;
            Mock<IEmailSender> emailSenderMock = null;

            var authorizationService =
                SetupServiceMock((authContext, userManager, signinManagerMq, emailSender, mockRepository) =>
                {
                    userManagerMock = userManager;
                    userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);
                    userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(It.IsAny<ApplicationUser>()))
                                    .ReturnsAsync("ttttokenxxx");

                    emailSenderMock = emailSender;
                    emailSender.Setup(x => x.SendAsync(It.IsAny<IEmailMessage>()));
                });

            var result = await authorizationService.ForgotPasswordAsync(new ForgotPasswordParameters
            {
                Email = "test@test.com"
            }, "https", "test.com", 8080);

            Assert.IsTrue(result);

            userManagerMock.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Once());
            // Following two should not be called
            userManagerMock.Verify(x => x.GeneratePasswordResetTokenAsync(It.IsAny<ApplicationUser>()), Times.Never());
            emailSenderMock.Verify(x => x.SendAsync(It.IsAny<IEmailMessage>()), Times.Never());
        }

        [TestMethod()]
        public async Task ResetPasswordSuccessTest()
        {
            Mock<IApplicationUserManager> userManagerMock = null;

            var authorizationService =
                SetupServiceMock((authContext, userManager, signinManagerMq, emailSender, mockRepository) =>
                {
                    userManagerMock = userManager;
                    userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(new ApplicationUser { UserName = "test" });
                    userManagerMock.Setup(x => x.ResetPasswordAsync(It.IsAny<ApplicationUser>(),
                                                                It.Is<string>(v => v == "test"),
                                                                It.Is<string>(v => v == "test"))).ReturnsAsync(new IdentityResultMock(true));
                });

            await authorizationService.ResetPasswordAsync(new ResetPasswordParameters
            {
                Email = "test",
                ConfirmPassword = "test",
                Password = "test",
                Token =  "test"
            });
        }

        [TestMethod()]
        public async Task ResetPasswordFailedTest()
        {
            Mock<IApplicationUserManager> userManagerMock = null;

            var authorizationService =
                SetupServiceMock((authContext, userManager, signinManagerMq, emailSender, mockRepository) =>
                {
                    userManagerMock = userManager;
                    userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(new ApplicationUser { UserName = "test" });
                    userManagerMock.Setup(x => x.ResetPasswordAsync(It.IsAny<ApplicationUser>(),
                                                                It.Is<string>(v => v == "test"),
                                                                It.Is<string>(v => v == "test"))).ReturnsAsync(new IdentityResultMock(false));
                });

            await Assert.ThrowsExceptionAsync<BadRequestException>(async () =>
            {
                await authorizationService.ResetPasswordAsync(new ResetPasswordParameters
                {
                    Email = "test",
                    ConfirmPassword = "test",
                    Password = "test",
                    Token = "test"
                });
            });
        }
    }
}