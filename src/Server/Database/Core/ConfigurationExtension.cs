using DevInstance.DevCoreApp.Server.Database.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Database.Core;

public static class ConfigurationExtension
{
    public static void ConfigureIdentityContext<T>(this IServiceCollection services) where T : ApplicationDbContext
    {
        services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<T>()
            .AddSignInManager()
            .AddDefaultTokenProviders();
    }
}
