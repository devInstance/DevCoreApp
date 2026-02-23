using DevInstance.DevCoreApp.Server.Database.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DevInstance.DevCoreApp.Server.Database.Core;

public static class ConfigurationExtension
{
    public static void ConfigureIdentityContext<T>(this IServiceCollection services) where T : ApplicationDbContext
    {
        services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<T>()
            .AddSignInManager()
            .AddDefaultTokenProviders();
    }
}
