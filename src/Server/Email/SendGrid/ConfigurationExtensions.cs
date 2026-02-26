using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevInstance.DevCoreApp.Server.EmailProcessor.SendGrid
{
    public static class ConfigurationExtensions
    {
        public static void AddSendGrid(this IServiceCollection services, IConfiguration configuration)
        {
            var apiKey = configuration["EmailConfiguration:SendGridApiKey"]
                ?? throw new InvalidOperationException("EmailConfiguration:SendGridApiKey is required.");
            services.AddScoped<IEmailProvider>(x => new SendGridEmailProvider(apiKey));
        }
    }
}
