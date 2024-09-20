using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DevInstance.DevCoreApp.Server.WebService.Tools
{
    public static class ServiceConfigurationExtensions
    {
        public static void AddServerAppServices(this IServiceCollection services)
        {
            foreach (var type in GetTypesWithHelpAttribute(Assembly.GetCallingAssembly()))
            {
                services.AddScoped(type);
            }
        }

        static IEnumerable<Type> GetTypesWithHelpAttribute(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof(AppServiceAttribute), true).Length > 0)
                {
                    yield return type;
                }
            }
        }
    }
}
