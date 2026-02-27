using DevInstance.DevCoreApp.Server.Admin.Services.Files;
using DevInstance.DevCoreApp.Shared.Model.Files;
using DevInstance.WebServiceToolkit.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static DevInstance.WebServiceToolkit.Controllers.ControllerUtils;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Controllers;

[Route("api/files")]
[ApiController]
[Authorize]
public class FileController : ControllerBase
{
    private readonly IFileService _fileService;

    public FileController(IFileService fileService)
    {
        _fileService = fileService;
    }

    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FileRecordItem>> UploadAsync(
        IFormFile file,
        [FromForm] string? entityType = null,
        [FromForm] string? entityId = null)
    {
        return await this.HandleWebRequestAsync<FileRecordItem>(async () =>
        {
            using var stream = file.OpenReadStream();
            var result = await _fileService.UploadAsync(
                stream, file.FileName, file.ContentType, entityType, entityId);
            return Ok(result.Result);
        });
    }

    [HttpGet("{filePublicId}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DownloadAsync(string filePublicId)
    {
        var result = await _fileService.DownloadAsync(filePublicId);
        var download = result.Result;
        return File(download.Stream, download.ContentType, download.FileName);
    }

    [HttpDelete("{filePublicId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<bool>> DeleteAsync(string filePublicId)
    {
        return await this.HandleWebRequestAsync<bool>(async () =>
        {
            var result = await _fileService.DeleteAsync(filePublicId);
            return Ok(result.Result);
        });
    }

    [HttpGet("{filePublicId}/url")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<string>> GetUrlAsync(string filePublicId, [FromQuery] int? expiryMinutes = null)
    {
        return await this.HandleWebRequestAsync<string>(async () =>
        {
            TimeSpan? expiry = expiryMinutes.HasValue ? TimeSpan.FromMinutes(expiryMinutes.Value) : null;
            var result = await _fileService.GetUrlAsync(filePublicId, expiry);
            return Ok(result.Result);
        });
    }
}
