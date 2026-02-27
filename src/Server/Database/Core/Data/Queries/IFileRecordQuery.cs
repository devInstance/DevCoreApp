using DevInstance.DevCoreApp.Server.Database.Core.Models.Files;
using DevInstance.WebServiceToolkit.Database.Queries;
using System;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface IFileRecordQuery : IModelQuery<FileRecord, IFileRecordQuery>,
        IQSearchable<IFileRecordQuery>,
        IQPageable<IFileRecordQuery>,
        IQSortable<IFileRecordQuery>
{
    IQueryable<FileRecord> Select();

    IFileRecordQuery ByEntityReference(string entityType, string entityId);
    IFileRecordQuery ByContentType(string contentType);
    IFileRecordQuery ByStorageProvider(string storageProvider);
}
