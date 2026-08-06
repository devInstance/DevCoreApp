using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DevInstance.DevCoreApp.Server.EmailProcessor.Core;
using DevInstance.DevCoreApp.Server.EmailProcessor.MailKit.Core;

namespace DevInstance.DevCoreApp.Server.EmailProcessor.MailKit
{
    public static class ConfigurationExtensions
    {
        public static void AddMailKit(this IServiceCollection services, IConfiguration configuration)
        {
            var emailConfig = new EmailConfiguration
            {
                SmtpServer = configuration["EmailConfiguration:SmtpServer"],
                Port = Int32.Parse(configuration["EmailConfiguration:Port"]),
                UserName = configuration["EmailConfiguration:UserName"],
                Password = configuration["EmailConfiguration:Password"]
            };
            services.AddScoped<IEmailProvider>(x => new MailKitEmailSender(emailConfig));
        }
    }
}
