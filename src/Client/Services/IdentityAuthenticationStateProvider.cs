using DevInstance.LogScope;
using DevInstance.SampleWebApp.Shared.Model;
using Microsoft.AspNetCore.Components.Authorization;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DevInstance.SampleWebApp.Client.Services
{
    public class IdentityAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IScopeLog log;
        private readonly AuthorizationService service;

        public IdentityAuthenticationStateProvider(IScopeManager l, AuthorizationService s)
        {
            log = l.CreateLogger(this);
            service = s;
        }

        public async Task LoginAsync(LoginParameters loginParameters)
        {
            await service.LoginAsync(loginParameters);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public async Task RegisterAsync(RegisterParameters registerParameters)
        {
            await service.RegisterAsync(registerParameters);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public async Task ForgotPasswordAsync(ForgotPasswordParameters forgotParameters)
        {
            await service.ForgotPasswordAsync(forgotParameters);
        }

        public async Task ResetPasswordAsync(ResetPasswordParameters resetPassswordParameters)
        {
            await service.ResetPasswordAsync(resetPassswordParameters);
        }

        public async Task LogoutAsync()
        {
            await service.LogoutAsync();
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public async Task DeleteAsync()
        {
            if (await service.DeleteAsync())
            {
                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            }
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = new ClaimsIdentity();
            try
            {
                var userInfo = await service.GetUserInfoAsync();
                if (userInfo.IsAuthenticated)
                {
                    var claims = new[] { new Claim(ClaimTypes.Name, userInfo.UserName) }.Concat(userInfo.ExposedClaims.Select(c => new Claim(c.Key, c.Value)));
                    identity = new ClaimsIdentity(claims, "Server authentication");
                }
            }
            catch (HttpRequestException ex)
            {
                log.E("Request failed", ex);
            }

            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
    }
}
