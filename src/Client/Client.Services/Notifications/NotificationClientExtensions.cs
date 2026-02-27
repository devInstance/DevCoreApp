using Microsoft.Extensions.DependencyInjection;

namespace DevInstance.DevCoreApp.Client.Services.Notifications;

public static class NotificationClientExtensions
{
    public static IServiceCollection AddNotificationHub(this IServiceCollection services)
    {
        services.AddScoped<INotificationHubClient, NotificationHubClient>();
        return services;
    }
}
