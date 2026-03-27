using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Authentication;

public static class ConfigurationExtensions
{
    public static void AddAppIdentity(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<AuthorizationContext>();
        services.AddScoped<BackgroundAuthorizationContext>();
        services.AddScoped<IAuthorizationContext>(sp =>
        {
            var accessor = sp.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            if (accessor.HttpContext != null)
                return sp.GetRequiredService<AuthorizationContext>();
            return sp.GetRequiredService<BackgroundAuthorizationContext>();
        });

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
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = 401;
                }
                else
                {
                    context.Response.Redirect(context.RedirectUri);
                }
                return Task.CompletedTask;
            };
        });

        services.Configure<DataProtectionTokenProviderOptions>(opt => opt.TokenLifespan = TimeSpan.FromHours(2));

        //services.AddAuthentication().AddIdentityServerJwt();

        services.AddScoped<IApplicationSignManager, ApplicationSignManager>();
        services.AddScoped<IApplicationUserManager, ApplicationUserManager>();
        services.AddScoped<IOrganizationContextResolver, OrganizationContextResolver>();
        services.AddScoped<IClaimsTransformation, PermissionClaimsTransformation>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddMemoryCache();

    }

    /// <summary>
    /// Applies pending EF Core migrations, seeds Identity roles,
    /// and runs all registered IDataSeeder implementations in order.
    /// </summary>
    public static async Task MigrateAndSeedAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        // Apply pending migrations
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        // Seed Identity roles
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var systemRoles = new HashSet<string> { ApplicationRoles.Owner, ApplicationRoles.Admin };
        foreach (var roleName in ApplicationRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName)
                {
                    IsSystemRole = systemRoles.Contains(roleName)
                });
            }
        }

        // Run all data seeders in order
        var seeders = services.GetServices<IDataSeeder>()
            .OrderBy(s => s.Order)
            .ToList();

        foreach (var seeder in seeders)
        {
            await seeder.SeedAsync();
        }
    }
}
