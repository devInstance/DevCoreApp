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

    [HttpPost("import/upload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ImportSessionItem>> Upload(
        IFormFile file,
        [FromQuery] string entityType)
    {
        return await this.HandleWebRequestAsync<ImportSessionItem>(async () =>
        {
            using var stream = file.OpenReadStream();
            var result = await _importExportService.ParseFileAsync(stream, file.FileName, entityType);
            return Ok(result.Result.Session);
        });
    }
}
