using Bogus;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Client.Services.Core.Notifications;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Model.Core.Notifications;
using DevInstance.WebServiceToolkit.Common.Tools;

namespace DevInstance.DevCoreApp.Client.Services.Mocks.Core.Notifications;

[BlazorServiceMock]
public class NotificationServiceMock : INotificationService
{
    private readonly List<NotificationItem> items;

    public NotificationServiceMock()
    {
        items = new Faker<NotificationItem>()
            .RuleFor(n => n.Id, _ => IdGenerator.New())
            .RuleFor(n => n.Title, f => f.Lorem.Sentence(4))
            .RuleFor(n => n.Message, f => f.Lorem.Sentence(10))
            .RuleFor(n => n.IsRead, f => f.Random.Bool(0.6f))
            .RuleFor(n => n.CreateDate, f => f.Date.Recent(7).ToUniversalTime())
            .Generate(12)
            .OrderByDescending(n => n.CreateDate)
            .ToList();
    }

    public Task<ServiceActionResult<PagedList<NotificationItem>>> GetListAsync(ListQuery query)
    {
        var page = items.Skip(query.Page * query.Top).Take(query.Top).ToArray();
        return Task.FromResult(ServiceActionResult<PagedList<NotificationItem>>.OK(
            PagedList.Create(page, items.Count, query.Top, query.Page)));
    }

    public Task<ServiceActionResult<int>> GetUnreadCountAsync() =>
        Task.FromResult(ServiceActionResult<int>.OK(items.Count(n => !n.IsRead)));

    public Task<ServiceActionResult<NotificationItem>> MarkAsReadAsync(string id)
    {
        var item = items.First(n => n.Id == id);
        item.IsRead = true;
        return Task.FromResult(ServiceActionResult<NotificationItem>.OK(item));
    }

    public Task<ServiceActionResult<int>> MarkAllReadAsync()
    {
        var count = items.Count(n => !n.IsRead);
        items.ForEach(n => n.IsRead = true);
        return Task.FromResult(ServiceActionResult<int>.OK(count));
    }
}
