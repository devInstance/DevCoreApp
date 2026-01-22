using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.WebService.Background.Requests;
using DevInstance.DevCoreApp.Server.WebService.Services;
using DevInstance.LogScope;
using System.Collections.Concurrent;

namespace DevInstance.DevCoreApp.Server.WebService.Background;

public class BackgroundWorker : BackgroundService, IBackgroundWorker
{
    protected readonly ConcurrentQueue<BackgroundRequestItem> theQueue = new();
    protected readonly IServiceScopeFactory factory;
    protected readonly IConfiguration _config;

    public BackgroundWorker(IServiceScopeFactory factory, IConfiguration config, IScopeManager manager)
    {
        this.factory = factory;
        _config = config;
    }

    public void Submit(BackgroundRequestItem item)
    {
        theQueue.Enqueue(item);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (var scope = factory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            while (!stoppingToken.IsCancellationRequested)
            {
                var utcNow = DateTime.UtcNow;
                if (theQueue.TryDequeue(out var request))
                {
                    try
                    {
                        switch (request.RequestType)
                        {
                            case BackgroundRequestType.SendEmail:
                            {
                                var emailRequest = (EmailRequest)request.Content;
                                var emailSenderService = scope.ServiceProvider.GetRequiredService<IEmailSenderService>();
                                await emailSenderService.SendAsync(emailRequest);
                            }
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        // TODO: Log exception
                    }
                }
                else
                {
#if DEBUG
                    await Task.Delay(1 * 1000, stoppingToken); // avoid tight loop
#else
                    await Task.Delay(10 * 1000, stoppingToken);
#endif
                }
            }
        }
    }
}
