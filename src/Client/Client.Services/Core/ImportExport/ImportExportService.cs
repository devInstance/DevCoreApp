using DevInstance.BlazorToolkit.Http;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Client.Services.Core.Api;
using DevInstance.DevCoreApp.Client.Services.Core.Time;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.LogScope;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DevInstance.DevCoreApp.Shared.Model.Core.ImportExport;

namespace DevInstance.DevCoreApp.Client.Services.Core.ImportExport;

/// <summary>
/// JSON endpoints go through BlazorToolkit's <c>IApiContext</c>. File uploads (multipart) and file
/// downloads use the named <see cref="HttpClient"/> directly, because <c>IApiContext</c> is
/// JSON-only; the bearer token still comes from the client's handler.
/// </summary>
[BlazorService]
public class ImportExportService : ApiServiceBase, IImportExportService
{
    private const string Root = "api/import-export";
    private readonly IHttpClientFactory httpFactory;

    public ImportExportService(IHttpApiContextFactory apiFactory, IHttpClientFactory httpFactory, IScopeManager logManager)
        : base(apiFactory, logManager)
    {
        this.httpFactory = httpFactory;
    }

    private HttpClient Http => httpFactory.CreateClient(ApiClient.HttpClientName);

    public Task<ServiceActionResult<List<string>>> GetImportableEntityTypesAsync() =>
        CallAsync(() => Api<List<string>>($"{Root}/import/entity-types").Get().ExecuteAsync());

    public Task<ServiceActionResult<List<ImportFieldDescriptor>>> GetImportFieldsAsync(string entityType) =>
        CallAsync(() => Api<List<ImportFieldDescriptor>>($"{Root}/import/{Segment(entityType)}/fields").Get().ExecuteAsync());

    public Task<ServiceActionResult<bool>> RequiresOrganizationSelectionAsync() =>
        CallAsync(() => Api<bool>($"{Root}/import/requires-organization").Get().ExecuteAsync());

    public Task<ServiceActionResult<ImportParseResult>> ParseHeadersAsync(Stream fileStream, string fileName) =>
        CallAsync(async () =>
        {
            using var content = await FileContentAsync(fileStream, fileName);
            return await ReadAsync<ImportParseResult>(await Http.PostAsync($"{Root}/import/parse-headers", content));
        });

    public Task<ServiceActionResult<ImportValidationResult>> ValidateAsync(
        Stream fileStream, string fileName, string entityType,
        List<ImportColumnMappingItem> mappings, string? organizationId = null) =>
        CallAsync(async () =>
        {
            using var content = await FileContentAsync(fileStream, fileName);
            content.Add(new StringContent(JsonSerializer.Serialize(mappings)), "mappingsJson");

            var url = $"{Root}/import/validate?entityType={Segment(entityType)}";
            if (!string.IsNullOrEmpty(organizationId))
            {
                url += $"&organizationId={Segment(organizationId)}";
            }
            return await ReadAsync<ImportValidationResult>(await Http.PostAsync(url, content));
        });

    public Task<ServiceActionResult<ImportCommitResult>> CommitAsync(string sessionId, List<int>? excludedRows = null) =>
        CallAsync(() => Api<ImportCommitResult>($"{Root}/import/{Segment(sessionId)}/commit")
            .Post<ImportCommitRequest>(new ImportCommitRequest { ExcludedRows = excludedRows! })
            .ExecuteAsync());

    public Task<ServiceActionResult<bool>> RollbackAsync(string sessionId) =>
        CallAsync(() => Api<bool>($"{Root}/import/{Segment(sessionId)}/rollback").Post<object?>(null).ExecuteAsync());

    public Task<ServiceActionResult<ImportSessionItem>> GetSessionAsync(string sessionId) =>
        CallAsync(() => Api<ImportSessionItem>($"{Root}/import/{Segment(sessionId)}").Get().ExecuteAsync());

    public Task<ServiceActionResult<List<ExportFieldDescriptor>>> GetExportFieldsAsync(string entityType) =>
        CallAsync(() => Api<List<ExportFieldDescriptor>>($"{Root}/export/{Segment(entityType)}/fields").Get().ExecuteAsync());

    public Task<ServiceActionResult<ExportDownloadResult>> ExportAsync(ExportRequestItem request) =>
        CallAsync(async () =>
        {
            using var response = await Http.PostAsJsonAsync($"{Root}/export", request);
            await ThrowIfFailedAsync(response);
            return new ExportDownloadResult
            {
                Stream = new MemoryStream(await response.Content.ReadAsByteArrayAsync()),
                ContentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream",
                FileName = response.Content.Headers.ContentDisposition?.FileNameStar
                    ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                    ?? "export"
            };
        });

    /// <summary>
    /// Copies the caller's stream (from its current position) instead of wrapping it: disposing
    /// the form would otherwise close it, and the import page reuses one buffered file stream for
    /// parse and then validate.
    /// </summary>
    private static async Task<MultipartFormDataContent> FileContentAsync(Stream stream, string fileName)
    {
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        var file = new ByteArrayContent(buffer.ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        return new MultipartFormDataContent { { file, "file", fileName } };
    }

    private static async Task<T?> ReadAsync<T>(HttpResponseMessage response)
    {
        using (response)
        {
            await ThrowIfFailedAsync(response);
            return await response.Content.ReadFromJsonAsync<T>();
        }
    }

    /// <summary>
    /// Surfaces the server's error message (a <c>ServiceActionError</c> body) the same way
    /// <c>IApiContext</c> does, so pages show "Record conflict: already imported" rather than a
    /// bare status code.
    /// </summary>
    private static async Task ThrowIfFailedAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        ServiceActionError? error = null;
        try
        {
            error = await response.Content.ReadFromJsonAsync<ServiceActionError>();
        }
        catch (JsonException)
        {
        }

        throw new DevInstance.BlazorToolkit.Exceptions.HttpServerException(
            error ?? new ServiceActionError { Message = $"Request failed ({(int)response.StatusCode})." },
            response.StatusCode);
    }
}
