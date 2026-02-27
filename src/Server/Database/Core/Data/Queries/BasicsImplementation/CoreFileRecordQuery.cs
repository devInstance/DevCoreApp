using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Files;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using System;
using System.Linq;

namespace NoCrast.Server.Database.Postgres.Data.Queries;

public class CoreFileRecordQuery : CoreDatabaseObjectQuery<FileRecord, CoreFileRecordQuery>, IFileRecordQuery
{
    private CoreFileRecordQuery(IQueryable<FileRecord> q, IScopeManager logManager,
                         ITimeProvider timeProvider,
                         ApplicationDbContext dB,
                         UserProfile currentProfile)
        : base(q, logManager, timeProvider, dB, currentProfile)
    {
    }

    public CoreFileRecordQuery(IScopeManager logManager,
                               ITimeProvider timeProvider,
                               ApplicationDbContext dB,
                               UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
    }

    public IFileRecordQuery ByPublicId(string id) => ByPublicIdHelper(id);

    public IFileRecordQuery ByEntityReference(string entityType, string entityId)
    {
        currentQuery = from fr in currentQuery
                       where fr.EntityType == entityType && fr.EntityId == entityId
                       select fr;
        return this;
    }

    public IFileRecordQuery ByContentType(string contentType)
    {
        currentQuery = from fr in currentQuery
                       where fr.ContentType == contentType
                       select fr;
        return this;
    }

    public IFileRecordQuery ByStorageProvider(string storageProvider)
    {
        currentQuery = from fr in currentQuery
                       where fr.StorageProvider == storageProvider
                       select fr;
        return this;
    }

    public IFileRecordQuery Clone()
    {
        return new CoreFileRecordQuery(currentQuery, LogManager, TimeProvider, DB, CurrentProfile);
    }

    public IFileRecordQuery Search(string search)
    {
        currentQuery = from fr in currentQuery
                       where fr.OriginalName.IndexOf(search) >= 0 ||
                             fr.FileName.IndexOf(search) >= 0
                       select fr;
        return this;
    }

    public IFileRecordQuery Skip(int value) => SkipHelper(value);

    public IFileRecordQuery Take(int value) => TakeHelper(value);

    public IFileRecordQuery SortBy(string column, bool isAsc)
    {
        this.IsAsc = isAsc;

        if (string.Compare(column, "createdate", true) == 0)
        {
            currentQuery = isAsc
                ? (from fr in currentQuery orderby fr.CreateDate select fr)
                : (from fr in currentQuery orderby fr.CreateDate descending select fr);
            SortedBy = "createdate";
        }
        else if (string.Compare(column, "originalname", true) == 0)
        {
            currentQuery = isAsc
                ? (from fr in currentQuery orderby fr.OriginalName select fr)
                : (from fr in currentQuery orderby fr.OriginalName descending select fr);
            SortedBy = "originalname";
        }
        else if (string.Compare(column, "sizebytes", true) == 0)
        {
            currentQuery = isAsc
                ? (from fr in currentQuery orderby fr.SizeBytes select fr)
                : (from fr in currentQuery orderby fr.SizeBytes descending select fr);
            SortedBy = "sizebytes";
        }
        else if (string.Compare(column, "contenttype", true) == 0)
        {
            currentQuery = isAsc
                ? (from fr in currentQuery orderby fr.ContentType select fr)
                : (from fr in currentQuery orderby fr.ContentType descending select fr);
            SortedBy = "contenttype";
        }
        else
        {
            throw new ArgumentException("Invalid column name");
        }

        return this;
    }
}
