using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.SampleWebApp.Server.Indentity
{
    public static class ConfigurationExtensions
    {
        public static void ConfigureIdentity(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<IAuthorizationContext, AuthorizationContext>();

            services.AddAuthentication()
                .AddIdentityServerJwt();
        }
    }
}
