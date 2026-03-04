using DevInstance.DevCoreApp.Server.Admin.Services.ImportExport;
using DevInstance.DevCoreApp.Shared.Model.ImportExport;
using DevInstance.WebServiceToolkit.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static DevInstance.WebServiceToolkit.Controllers.ControllerUtils;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Controllers;

[Route("api/import-export")]
[ApiController]
[Authorize(Roles = "Owner,Admin")]
public class ImportExportController : ControllerBase
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
        return await this.HandleWebRequestAsync<ImportParseResult>(async () =>
        {
            using var stream = file.OpenReadStream();
            var result = await _importExportService.ParseHeadersAsync(stream, file.FileName);
            return Ok(result.Result);
        });
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
        return await this.HandleWebRequestAsync<ImportValidationResult>(async () =>
        {
            var mappings = System.Text.Json.JsonSerializer.Deserialize<List<ImportColumnMappingItem>>(mappingsJson) ?? new();
            using var stream = file.OpenReadStream();
            var result = await _importExportService.ValidateAsync(stream, file.FileName, entityType, mappings, organizationId);
            return Ok(result.Result);
        });
    }
}
