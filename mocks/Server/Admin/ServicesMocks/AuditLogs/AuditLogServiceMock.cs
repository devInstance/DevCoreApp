using Bogus;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.AuditLogs;
using DevInstance.DevCoreApp.Shared.Model.AuditLogs;
using DevInstance.WebServiceToolkit.Common.Model;
using DevInstance.WebServiceToolkit.Common.Tools;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Mocks.AuditLogs;

[BlazorServiceMock]
public class AuditLogServiceMock : IAuditLogService
{
    const int TotalCount = 80;
    List<AuditLogItem> modelList;

    private int delay = 500;

    private static readonly string[] TableNames = new[]
    {
        "UserProfiles", "Organizations", "EmailLogs", "AspNetRoles",
        "RolePermissions", "Settings", "BackgroundTasks", "FileRecords"
    };

    private static readonly string[] UserNames = new[]
    {
        "John Smith", "Jane Doe", "Bob Wilson", "Alice Brown", "System"
    };

    public AuditLogServiceMock()
    {
        var faker = CreateFaker();
        modelList = faker.Generate(TotalCount);
    }

    private static Faker<AuditLogItem> CreateFaker()
    {
        return new Faker<AuditLogItem>()
            .RuleFor(a => a.Id, f => Guid.NewGuid().ToString())
            .RuleFor(a => a.TableName, f => f.PickRandom(TableNames))
            .RuleFor(a => a.RecordId, f => IdGenerator.New())
            .RuleFor(a => a.Action, f => f.PickRandom("Insert", "Update", "Delete"))
            .RuleFor(a => a.Source, f => f.PickRandom("Application", "Application", "Application", "Database"))
            .RuleFor(a => a.ChangedByUserId, (f, a) => a.Source == "Database" ? null : IdGenerator.New())
            .RuleFor(a => a.ChangedByUserName, (f, a) => a.Source == "Database" ? null : f.PickRandom(UserNames))
            .RuleFor(a => a.ChangedAt, f => f.Date.Recent(30))
            .RuleFor(a => a.IpAddress, (f, a) => a.Source == "Database" ? null : f.Internet.IpAddress().ToString())
            .RuleFor(a => a.CorrelationId, (f, a) => a.Source == "Database" ? null : Guid.NewGuid().ToString()[..8])
            .RuleFor(a => a.OldValues, (f, a) => a.Action == "Insert" ? null : GenerateOldValues(f, a.TableName))
            .RuleFor(a => a.NewValues, (f, a) => a.Action == "Delete" ? null : GenerateNewValues(f, a.TableName));
    }

    private static string GenerateOldValues(Faker f, string tableName)
    {
        return tableName switch
        {
            "UserProfiles" => $"{{\"Email\":\"{f.Internet.Email()}\",\"FirstName\":\"{f.Name.FirstName()}\",\"LastName\":\"{f.Name.LastName()}\",\"IsActive\":true}}",
            "Organizations" => $"{{\"Name\":\"{f.Company.CompanyName()}\",\"IsActive\":true,\"Type\":\"Department\"}}",
            "Settings" => $"{{\"Key\":\"smtp.host\",\"Value\":\"{f.Internet.DomainName()}\"}}",
            _ => $"{{\"Status\":\"{f.PickRandom("Active", "Pending", "Draft")}\",\"UpdateDate\":\"{f.Date.Recent(60):O}\"}}"
        };
    }

    private static string GenerateNewValues(Faker f, string tableName)
    {
        return tableName switch
        {
            "UserProfiles" => $"{{\"Email\":\"{f.Internet.Email()}\",\"FirstName\":\"{f.Name.FirstName()}\",\"LastName\":\"{f.Name.LastName()}\",\"IsActive\":{f.Random.Bool().ToString().ToLower()}}}",
            "Organizations" => $"{{\"Name\":\"{f.Company.CompanyName()}\",\"IsActive\":{f.Random.Bool().ToString().ToLower()},\"Type\":\"Department\"}}",
            "Settings" => $"{{\"Key\":\"smtp.host\",\"Value\":\"{f.Internet.DomainName()}\"}}",
            _ => $"{{\"Status\":\"{f.PickRandom("Active", "Completed", "Approved")}\",\"UpdateDate\":\"{DateTime.UtcNow:O}\"}}"
        };
    }

    public async Task<ServiceActionResult<ModelList<AuditLogItem>>> GetAllAsync(
        int? top, int? page, string? sortField = null, bool? isAsc = null,
        string? search = null, int? action = null, int? source = null,
        string? tableName = null, string? recordId = null,
        DateTime? startDate = null, DateTime? endDate = null)
    {
        var pageVal = page ?? 0;
        var topVal = top ?? 10;

        var filtered = modelList.AsEnumerable();

        if (!string.IsNullOrEmpty(search))
        {
            filtered = filtered.Where(a =>
                a.TableName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                a.RecordId.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (a.ChangedByUserName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (a.IpAddress?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (action.HasValue)
        {
            var actionName = ((DevInstance.DevCoreApp.Server.Database.Core.Models.AuditAction)action.Value).ToString();
            filtered = filtered.Where(a => a.Action == actionName);
        }

        if (source.HasValue)
        {
            var sourceName = ((DevInstance.DevCoreApp.Server.Database.Core.Models.AuditSource)source.Value).ToString();
            filtered = filtered.Where(a => a.Source == sourceName);
        }

        if (!string.IsNullOrEmpty(tableName))
        {
            filtered = filtered.Where(a => a.TableName == tableName);
        }

        if (!string.IsNullOrEmpty(recordId))
        {
            filtered = filtered.Where(a => a.RecordId.Contains(recordId, StringComparison.OrdinalIgnoreCase));
        }

        if (startDate.HasValue)
            filtered = filtered.Where(a => a.ChangedAt >= startDate.Value);
        if (endDate.HasValue)
            filtered = filtered.Where(a => a.ChangedAt <= endDate.Value);

        var filteredList = filtered.ToList();

        var items = filteredList
            .Skip(pageVal * topVal)
            .Take(topVal)
            .ToArray();

        string[]? sortBy = !string.IsNullOrEmpty(sortField)
            ? new[] { (isAsc == false ? "-" : "") + sortField }
            : null;

        await Task.Delay(delay);

        return ServiceActionResult<ModelList<AuditLogItem>>.OK(
            ModelListResult.CreateList(items, filteredList.Count, topVal, pageVal, sortBy, search));
    }
}
