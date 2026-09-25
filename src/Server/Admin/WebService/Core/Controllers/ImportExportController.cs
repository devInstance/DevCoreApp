using DevInstance.DevCoreApp.Server.Admin.Services.Core.ImportExport;
using DevInstance.DevCoreApp.Shared.Model.Core.ImportExport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Core.Controllers;

[Route("api/import-export")]
[Authorize(Roles = "Owner,Admin")]
public class ImportExportController : ApiControllerBase
{
    private readonly IImportExportService _importExportService;

    public ImportExportController(IImportExportService importExportService)
    {
        _importExportService = importExportService;
    }

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
    public async Task<ActionResult<ImportValidationResult>> Validate(
        IFormFile file,
        [FromQuery] string entityType,
        [FromForm] string mappingsJson,
        [FromQuery] string? organizationId = null)
    {
        // TODO (WASM Phase 1): bind the mappings as part of a multipart DTO instead of
        // deserializing a form field in the controller.
        var mappings = System.Text.Json.JsonSerializer.Deserialize<List<ImportColumnMappingItem>>(mappingsJson) ?? new();
        await using var stream = file.OpenReadStream();
        return await HandleServiceAsync(() => _importExportService.ValidateAsync(stream, file.FileName, entityType, mappings, organizationId));
    }
}
