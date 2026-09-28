using DevInstance.DevCoreApp.Server.Services.Core.ImportExport;
using DevInstance.DevCoreApp.Shared.Model.Core.ImportExport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevInstance.DevCoreApp.Server.Api.Core.Controllers;

[Route("api/import-export")]
[Authorize(Roles = "Owner,Admin")]
public class ImportExportController : ApiControllerBase
{
    private readonly IImportExportService _importExportService;

    public ImportExportController(IImportExportService importExportService)
    {
        _importExportService = importExportService;
    }

    // ── Discovery ──

    [HttpGet("import/entity-types")]
    public ActionResult<List<string>> GetImportableEntityTypes()
        => HandleService(() => _importExportService.GetImportableEntityTypes());

    [HttpGet("import/{entityType}/fields")]
    public ActionResult<List<ImportFieldDescriptor>> GetImportFields(string entityType)
        => HandleService(() => _importExportService.GetImportFields(entityType));

    /// <summary>True when the caller has no primary organization and must pick one for the import.</summary>
    [HttpGet("import/requires-organization")]
    public ActionResult<bool> RequiresOrganizationSelection()
        => HandleService(() => _importExportService.RequiresOrganizationSelection());

    [HttpGet("export/entity-types")]
    public ActionResult<List<string>> GetExportableEntityTypes()
        => HandleService(() => _importExportService.GetExportableEntityTypes());

    [HttpGet("export/{entityType}/fields")]
    public ActionResult<List<ExportFieldDescriptor>> GetExportFields(string entityType)
        => HandleService(() => _importExportService.GetExportFields(entityType));

    // ── Import session ──

    [HttpGet("import/{sessionId}")]
    public Task<ActionResult<ImportSessionItem>> GetSessionAsync(string sessionId)
        => HandleServiceAsync(() => _importExportService.GetSessionAsync(sessionId));

    [HttpPost("import/{sessionId}/commit")]
    public Task<ActionResult<ImportCommitResult>> CommitAsync(string sessionId, [FromBody] ImportCommitRequest request)
        => HandleServiceAsync(() => _importExportService.CommitAsync(sessionId, request.ExcludedRows));

    [HttpPost("import/{sessionId}/rollback")]
    public Task<ActionResult<bool>> RollbackAsync(string sessionId)
        => HandleServiceAsync(() => _importExportService.RollbackAsync(sessionId));

    // ── Files ──

    [HttpPost("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Export([FromBody] ExportRequestItem request)
    {
        var result = await _importExportService.ExportAsync(request);
        var download = result.Result;
        return File(download.Stream, download.ContentType, download.FileName);
    }

    [HttpGet("template")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTemplate([FromQuery] string entityType, [FromQuery] ExportFileFormat format = ExportFileFormat.Csv)
    {
        var result = await _importExportService.GetTemplateAsync(entityType, format);
        var download = result.Result;
        return File(download.Stream, download.ContentType, download.FileName);
    }

    [HttpPost("import/parse-headers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ImportParseResult>> ParseHeaders(IFormFile file)
    {
        // IFormFile is an HTTP type services must not see; the stream stays open for the call.
        await using var stream = file.OpenReadStream();
        return await HandleServiceAsync(() => _importExportService.ParseHeadersAsync(stream, file.FileName));
    }

    [HttpPost("import/validate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ImportValidationResult>> Validate([FromForm] ImportValidateForm form)
    {
        // IFormFile is an HTTP type services must not see; the stream stays open for the call.
        await using var stream = form.File.OpenReadStream();
        return await HandleServiceAsync(() => _importExportService.ValidateAsync(
            stream, form.File.FileName, form.EntityType, form.Mappings, form.OrganizationId));
    }
}
