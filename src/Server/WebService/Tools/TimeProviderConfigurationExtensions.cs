using DevInstance.DevCoreApp.Shared.Utils;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DevInstance.DevCoreApp.Server.WebService.Tools
{
    public static class TimeProviderConfigurationExtensions
    {
        public static void AddTimeProvider(this IServiceCollection services)
        {
            services.AddSingleton<ITimeProvider, Shared.Utils.TimeProvider>();//TODO: migrate to a new .NET8 TimeProvider
        }
    }
}
