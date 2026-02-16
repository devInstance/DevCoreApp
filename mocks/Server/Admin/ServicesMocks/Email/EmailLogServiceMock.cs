using Bogus;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.Email;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.WebServiceToolkit.Common.Model;
using DevInstance.WebServiceToolkit.Common.Tools;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Mocks.Email;

[BlazorServiceMock]
public class EmailLogServiceMock : IEmailLogService
{
    const int TotalCount = 50;
    List<EmailLogItem> modelList = new List<EmailLogItem>();

    private int delay = 500;

    public EmailLogServiceMock()
    {
        var faker = CreateFaker();
        modelList = faker.Generate(TotalCount);
    }

    private static Faker<EmailLogItem> CreateFaker()
    {
        return new Faker<EmailLogItem>()
            .RuleFor(e => e.Id, f => IdGenerator.New())
            .RuleFor(e => e.FromAddress, f => "noreply@example.com")
            .RuleFor(e => e.FromName, f => "DevCoreApp")
            .RuleFor(e => e.ToAddress, f => f.Internet.Email())
            .RuleFor(e => e.ToName, f => f.Name.FullName())
            .RuleFor(e => e.Subject, f => f.PickRandom(
                "Welcome to DevCoreApp",
                "Password Reset Request",
                "Account Verification",
                "Your account has been created"))
            .RuleFor(e => e.Content, f => $"<html><body><p>{f.Lorem.Paragraphs(2)}</p></body></html>")
            .RuleFor(e => e.IsHtml, true)
            .RuleFor(e => e.Status, f => f.PickRandom("Sent", "Failed", "Batched"))
            .RuleFor(e => e.ErrorMessage, (f, e) => e.Status == "Failed" ? f.PickRandom("SMTP timeout", "Invalid recipient", "Connection refused") : null)
            .RuleFor(e => e.TemplateName, f => f.PickRandom("Registration", "PasswordReset", "Welcome"))
            .RuleFor(e => e.ScheduledDate, f => f.Date.Recent(30))
            .RuleFor(e => e.SentDate, (f, e) => e.Status == "Sent" ? e.ScheduledDate.AddSeconds(f.Random.Int(1, 30)) : null)
            .RuleFor(e => e.CreateDate, (f, e) => e.ScheduledDate)
            .RuleFor(e => e.UpdateDate, (f, e) => e.SentDate ?? e.ScheduledDate);
    }

    public async Task<ServiceActionResult<ModelList<EmailLogItem>>> GetAllAsync(
        int? top, int? page, string? sortField = null, bool? isAsc = null,
        string? search = null, int? status = null,
        DateTime? startDate = null, DateTime? endDate = null)
    {
        var pageVal = page ?? 0;
        var topVal = top ?? 10;

        var filtered = modelList.AsEnumerable();

        if (!string.IsNullOrEmpty(search))
        {
            filtered = filtered.Where(e =>
                e.ToAddress.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.ToName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.Subject.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (startDate.HasValue)
            filtered = filtered.Where(e => e.ScheduledDate >= startDate.Value);
        if (endDate.HasValue)
            filtered = filtered.Where(e => e.ScheduledDate <= endDate.Value);

        var filteredList = filtered.ToList();

        var items = filteredList
            .Skip(pageVal * topVal)
            .Take(topVal)
            .ToArray();

        string[] sortBy = !string.IsNullOrEmpty(sortField)
            ? new[] { (isAsc == false ? "-" : "") + sortField }
            : null;

        await Task.Delay(delay);

        return ServiceActionResult<ModelList<EmailLogItem>>.OK(
            ModelListResult.CreateList(items, filteredList.Count, topVal, pageVal, sortBy, search));
    }

    public Task<ServiceActionResult<ModelList<EmailLogItem>>> GetListAsync(int? top, int? page, string[] sortBy, string search)
    {
        string sortField = null;
        bool? isAsc = null;
        if (sortBy != null && sortBy.Length > 0)
        {
            var first = sortBy[0];
            isAsc = !first.StartsWith("-");
            sortField = isAsc == true ? first : first.Substring(1);
        }
        return GetAllAsync(top, page, sortField, isAsc, search);
    }

    public async Task<ServiceActionResult<EmailLogItem>> GetAsync(string publicId)
    {
        var item = modelList.Find(e => e.Id == publicId);
        if (item == null) throw new InvalidOperationException("Email log entry not found.");

        await Task.Delay(delay);

        return ServiceActionResult<EmailLogItem>.OK(item);
    }

    public Task<ServiceActionResult<EmailLogItem>> AddAsync(EmailLogItem item)
    {
        throw new NotImplementedException();
    }

    public Task<ServiceActionResult<EmailLogItem>> UpdateAsync(string id, EmailLogItem item)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceActionResult<EmailLogItem>> DeleteAsync(string publicId)
    {
        var item = modelList.Find(e => e.Id == publicId);
        if (item == null) throw new InvalidOperationException("Email log entry not found.");

        modelList.Remove(item);

        await Task.Delay(delay);

        return ServiceActionResult<EmailLogItem>.OK(item);
    }

    public async Task<ServiceActionResult<bool>> DeleteMultipleAsync(List<string> publicIds)
    {
        modelList.RemoveAll(e => publicIds.Contains(e.Id));

        await Task.Delay(delay);

        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<bool>> ResendAsync(string publicId)
    {
        var item = modelList.Find(e => e.Id == publicId);
        if (item == null) throw new InvalidOperationException("Email log entry not found.");

        item.Status = "Batched";
        item.ErrorMessage = null;
        item.SentDate = null;
        item.ScheduledDate = DateTime.UtcNow;

        await Task.Delay(delay);

        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<int>> ResendAllFailedAsync(
        int? status = null, DateTime? startDate = null, DateTime? endDate = null, string? search = null)
    {
        var failed = modelList.Where(e => e.Status == "Failed").ToList();

        foreach (var item in failed)
        {
            item.Status = "Batched";
            item.ErrorMessage = null;
            item.SentDate = null;
            item.ScheduledDate = DateTime.UtcNow;
        }

        await Task.Delay(delay);

        return ServiceActionResult<int>.OK(failed.Count);
    }
}
