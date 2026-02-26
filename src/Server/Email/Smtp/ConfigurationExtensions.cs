using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevInstance.DevCoreApp.Server.EmailProcessor.Smtp
{
    public static class ConfigurationExtensions
    {
        public static void AddSmtpEmail(this IServiceCollection services, IConfiguration configuration)
        {
            var emailConfig = new EmailConfiguration
            {
                SmtpServer = configuration["EmailConfiguration:SmtpServer"],
                Port = Int32.Parse(configuration["EmailConfiguration:Port"]),
                UserName = configuration["EmailConfiguration:UserName"],
                Password = configuration["EmailConfiguration:Password"]
            };
            services.AddScoped<IEmailProvider>(x => new SmtpEmailProvider(emailConfig));
        }
    }
}
