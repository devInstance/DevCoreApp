using DevInstance.SampleWebApp.Shared.Utils;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DevInstance.SampleWebApp.Server.WebService.Tools
{
    public static class TimeProviderConfigurationExtensions
    {
        public static void AddTimeProvider(this IServiceCollection services)
        {
            services.AddSingleton<ITimeProvider, TimeProvider>();
        }
    }
}
