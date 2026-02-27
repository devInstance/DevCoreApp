using DevInstance.DevCoreApp.Server.Database.Core.Models.Files;
using DevInstance.DevCoreApp.Shared.Model.Files;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;

public static class FileRecordDecorators
{
    public static FileRecordItem ToView(this FileRecord fileRecord)
    {
        return new FileRecordItem
        {
            Id = fileRecord.PublicId,
            FileName = fileRecord.FileName,
            OriginalName = fileRecord.OriginalName,
            ContentType = fileRecord.ContentType,
            SizeBytes = fileRecord.SizeBytes,
            StorageProvider = fileRecord.StorageProvider,
            StoragePath = fileRecord.StoragePath,
            EntityType = fileRecord.EntityType,
            EntityId = fileRecord.EntityId,
            CreateDate = fileRecord.CreateDate,
            UpdateDate = fileRecord.UpdateDate
        };
    }

    public static FileRecord ToRecord(this FileRecord fileRecord, FileRecordItem item)
    {
        fileRecord.FileName = item.FileName;
        fileRecord.OriginalName = item.OriginalName;
        fileRecord.ContentType = item.ContentType;
        fileRecord.SizeBytes = item.SizeBytes;
        fileRecord.StorageProvider = item.StorageProvider;
        fileRecord.StoragePath = item.StoragePath;
        fileRecord.EntityType = item.EntityType;
        fileRecord.EntityId = item.EntityId;

        return fileRecord;
    }
}
