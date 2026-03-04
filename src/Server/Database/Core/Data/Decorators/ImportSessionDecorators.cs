using System.Collections.Generic;
using System.Text.Json;
using DevInstance.DevCoreApp.Server.Database.Core.Models.ImportExport;
using DevInstance.DevCoreApp.Shared.Model.ImportExport;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;

public static class ImportSessionDecorators
{
    public static ImportSessionItem ToView(this ImportSession session)
    {
        List<ImportColumnMappingItem>? mappings = null;
        if (!string.IsNullOrEmpty(session.ColumnMappingJson))
        {
            mappings = JsonSerializer.Deserialize<List<ImportColumnMappingItem>>(session.ColumnMappingJson);
        }

        return new ImportSessionItem
        {
            Id = session.PublicId,
            EntityType = session.EntityType,
            OriginalFileName = session.OriginalFileName,
            FileFormat = session.FileFormat,
            Status = session.Status,
            FileRecordId = session.FileRecordId,
            ColumnMappings = mappings,
            TotalRows = session.TotalRows,
            ValidRows = session.ValidRows,
            ErrorRows = session.ErrorRows,
            ImportedRows = session.ImportedRows,
            UpdatedRows = session.UpdatedRows,
            ErrorMessage = session.ErrorMessage,
            CreateDate = session.CreateDate,
            UpdateDate = session.UpdateDate
        };
    }

    public static ImportSession ToRecord(this ImportSession session, ImportSessionItem item)
    {
        session.EntityType = item.EntityType;
        session.OriginalFileName = item.OriginalFileName;
        session.FileFormat = item.FileFormat;
        session.Status = item.Status;
        session.FileRecordId = item.FileRecordId;
        session.TotalRows = item.TotalRows;
        session.ValidRows = item.ValidRows;
        session.ErrorRows = item.ErrorRows;
        session.ImportedRows = item.ImportedRows;
        session.ErrorMessage = item.ErrorMessage;

        if (item.ColumnMappings != null)
        {
            session.ColumnMappingJson = JsonSerializer.Serialize(item.ColumnMappings);
        }

        return session;
    }
}
