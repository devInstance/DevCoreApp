using DevInstance.DevCoreApp.Server.Admin.Services.Core.Files;
using DevInstance.DevCoreApp.Shared.Model.Core.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Core.Controllers;

[Route("api/files")]
[Authorize]
public class FileController : ApiControllerBase
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
        // IFormFile is an HTTP type services must not see; the stream stays open for the call.
        await using var stream = file.OpenReadStream();
        return await HandleServiceAsync(() => _fileService.UploadAsync(
            stream, file.FileName, file.ContentType, entityType, entityId));
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
    public Task<ActionResult<bool>> DeleteAsync(string filePublicId)
    {
        return HandleServiceAsync(() => _fileService.DeleteAsync(filePublicId));
    }

    [HttpGet("{filePublicId}/url")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<ActionResult<string>> GetUrlAsync(string filePublicId, [FromQuery] int? expiryMinutes = null)
    {
        // Minutes -> TimeSpan: TimeSpan has no stable query-string form, so the API takes minutes.
        TimeSpan? expiry = expiryMinutes.HasValue ? TimeSpan.FromMinutes(expiryMinutes.Value) : null;
        return HandleServiceAsync(() => _fileService.GetUrlAsync(filePublicId, expiry));
    }
}
