using DevInstance.SampleWebApp.Server.Database.Core;
using DevInstance.SampleWebApp.Server.Database.Core.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.SampleWebApp.Server.WebService.Indentity
{
    public static class ConfigurationExtensions
    {
        public static void ConfigureIdentity(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<IAuthorizationContext, AuthorizationContext>();

            services.Configure<IdentityOptions>(options =>
            {
                // Password settings
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                //options.Password.RequiredUniqueChars = 6;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                options.Lockout.MaxFailedAccessAttempts = 10;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.RequireUniqueEmail = false;
            });

            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = false;
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = 401;
                    return Task.CompletedTask;
                };
            });

            services.Configure<DataProtectionTokenProviderOptions>(opt => opt.TokenLifespan = TimeSpan.FromHours(2));

            //services.AddAuthentication().AddIdentityServerJwt();

            services.AddScoped<IApplicationSignManager, ApplicationSignManager>();
            services.AddScoped<IApplicationUserManager, ApplicationUserManager>();

        }
    }
}
