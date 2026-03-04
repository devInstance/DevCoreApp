using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.Authentication;
using DevInstance.DevCoreApp.Server.Admin.Services.Settings;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;
using DevInstance.DevCoreApp.Shared.Model.Files;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.DevCoreApp.Server.StorageProcessor;
using DevInstance.LogScope;
using DevInstance.DevCoreApp.Server.Admin.Services.Exceptions;
using DevInstance.WebServiceToolkit.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Files;

[BlazorService]
public class FileService : BaseService, IFileService
{
    private readonly IScopeLog log;
    private readonly IFileStorageProvider StorageProvider;
    private readonly ISettingsService SettingsService;
    private readonly IOperationContext OperationContext;

    private const string SettingsCategory = "Storage";
    private const string MaxFileSizeBytesKey = "MaxFileSizeBytes";
    private const string AllowedContentTypesKey = "AllowedContentTypes";
    private const string SoftDeleteKey = "SoftDelete";

    private const long DefaultMaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private const string DefaultAllowedContentTypes = "*"; // all types

    public FileService(IScopeManager logManager,
                       ITimeProvider timeProvider,
                       IQueryRepository query,
                       IAuthorizationContext authorizationContext,
                       IFileStorageProvider storageProvider,
                       ISettingsService settingsService,
                       IOperationContext operationContext)
        : base(logManager, timeProvider, query, authorizationContext)
    {
        log = logManager.CreateLogger(this);
        StorageProvider = storageProvider;
        SettingsService = settingsService;
        OperationContext = operationContext;
    }

    public async Task<ServiceActionResult<FileRecordItem>> UploadAsync(
        Stream stream, string originalName, string contentType,
        string? entityType = null, string? entityId = null,
        Guid? organizationIdOverride = null)
    {
        using var l = log.TraceScope();

        // Validate content type
        var allowedTypes = await SettingsService.GetAsync<string>(SettingsCategory, AllowedContentTypesKey);
        allowedTypes ??= DefaultAllowedContentTypes;

        if (allowedTypes != "*")
        {
            var allowed = allowedTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!allowed.Any(t => string.Equals(t, contentType, StringComparison.OrdinalIgnoreCase)))
            {
                throw new BadRequestException($"Content type '{contentType}' is not allowed.");
            }
        }

        // Validate file size
        var maxSize = await SettingsService.GetAsync<long>(SettingsCategory, MaxFileSizeBytesKey);
        if (maxSize <= 0) maxSize = DefaultMaxFileSizeBytes;

        if (stream.CanSeek && stream.Length > maxSize)
        {
            throw new BadRequestException($"File size exceeds the maximum allowed size of {maxSize} bytes.");
        }

        // Upload to storage provider
        var uploadResult = await StorageProvider.UploadAsync(stream, originalName, contentType);
        if (!uploadResult.Success)
        {
            l.E($"Storage provider upload failed: {uploadResult.ErrorMessage}");
            throw new BusinessRuleException($"File upload failed: {uploadResult.ErrorMessage}");
        }

        // Validate size after upload (for non-seekable streams)
        if (uploadResult.SizeBytes > maxSize)
        {
            await StorageProvider.DeleteAsync(uploadResult.StoragePath!);
            throw new BadRequestException($"File size exceeds the maximum allowed size of {maxSize} bytes.");
        }

        // Create FileRecord
        var fileQuery = Repository.GetFileRecordQuery(AuthorizationContext.CurrentProfile);
        var fileRecord = fileQuery.CreateNew();
        fileRecord.OriginalName = originalName;
        fileRecord.FileName = Path.GetFileName(uploadResult.StoragePath!);
        fileRecord.ContentType = contentType;
        fileRecord.SizeBytes = uploadResult.SizeBytes;
        fileRecord.StorageProvider = "Local"; // from config/provider
        fileRecord.StoragePath = uploadResult.StoragePath!;
        fileRecord.EntityType = entityType;
        fileRecord.EntityId = entityId;
        fileRecord.OrganizationId = organizationIdOverride
            ?? OperationContext.PrimaryOrganizationId
            ?? Guid.Empty;
        fileRecord.CreatedBy = AuthorizationContext.CurrentProfile;
        fileRecord.UpdatedBy = AuthorizationContext.CurrentProfile;

        await fileQuery.AddAsync(fileRecord);

        l.I($"File uploaded: {originalName} → {uploadResult.StoragePath}");
        return ServiceActionResult<FileRecordItem>.OK(fileRecord.ToView());
    }

    public async Task<ServiceActionResult<FileDownloadResult>> DownloadAsync(string filePublicId)
    {
        using var l = log.TraceScope();

        var fileQuery = Repository.GetFileRecordQuery(AuthorizationContext.CurrentProfile);
        var fileRecord = await fileQuery.ByPublicId(filePublicId).Select().FirstOrDefaultAsync();

        if (fileRecord == null)
            throw new RecordNotFoundException("File not found.");

        var stream = await StorageProvider.DownloadAsync(fileRecord.StoragePath);

        l.I($"File downloaded: {fileRecord.OriginalName}");
        return ServiceActionResult<FileDownloadResult>.OK(new FileDownloadResult
        {
            Stream = stream,
            ContentType = fileRecord.ContentType,
            FileName = fileRecord.OriginalName
        });
    }

    public async Task<ServiceActionResult<bool>> DeleteAsync(string filePublicId)
    {
        using var l = log.TraceScope();

        var fileQuery = Repository.GetFileRecordQuery(AuthorizationContext.CurrentProfile);
        var fileRecord = await fileQuery.ByPublicId(filePublicId).Select().FirstOrDefaultAsync();

        if (fileRecord == null)
            throw new RecordNotFoundException("File not found.");

        var softDelete = await SettingsService.GetAsync<bool>(SettingsCategory, SoftDeleteKey);

        if (softDelete)
        {
            fileRecord.IsActive = false;
            fileRecord.UpdatedBy = AuthorizationContext.CurrentProfile;
            await fileQuery.UpdateAsync(fileRecord);
            l.I($"File soft-deleted: {fileRecord.OriginalName}");
        }
        else
        {
            await StorageProvider.DeleteAsync(fileRecord.StoragePath);
            await fileQuery.RemoveAsync(fileRecord);
            l.I($"File hard-deleted: {fileRecord.OriginalName}");
        }

        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<string>> GetUrlAsync(string filePublicId, TimeSpan? expiry = null)
    {
        using var l = log.TraceScope();

        var fileQuery = Repository.GetFileRecordQuery(AuthorizationContext.CurrentProfile);
        var fileRecord = await fileQuery.ByPublicId(filePublicId).Select().FirstOrDefaultAsync();

        if (fileRecord == null)
            throw new RecordNotFoundException("File not found.");

        var url = await StorageProvider.GetUrlAsync(fileRecord.StoragePath, expiry);

        return ServiceActionResult<string>.OK(url);
    }
}
