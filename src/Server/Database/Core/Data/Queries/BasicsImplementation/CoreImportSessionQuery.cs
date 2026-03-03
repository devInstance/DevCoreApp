using System;
using System.Linq;
using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.Database.Core.Models.ImportExport;
using DevInstance.DevCoreApp.Shared.Model.ImportExport;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;

namespace NoCrast.Server.Database.Postgres.Data.Queries;

public class CoreImportSessionQuery : CoreDatabaseObjectQuery<ImportSession, CoreImportSessionQuery>, IImportSessionQuery
{
    private CoreImportSessionQuery(IQueryable<ImportSession> q, IScopeManager logManager,
                         ITimeProvider timeProvider,
                         ApplicationDbContext dB,
                         UserProfile currentProfile)
        : base(q, logManager, timeProvider, dB, currentProfile)
    {
    }

    public CoreImportSessionQuery(IScopeManager logManager,
                                  ITimeProvider timeProvider,
                                  ApplicationDbContext dB,
                                  UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
    }

    public IImportSessionQuery ByPublicId(string id) => ByPublicIdHelper(id);

    public IImportSessionQuery ByEntityType(string entityType)
    {
        currentQuery = from s in currentQuery
                       where s.EntityType == entityType
                       select s;
        return this;
    }

    public IImportSessionQuery ByStatus(ImportSessionStatus status)
    {
        currentQuery = from s in currentQuery
                       where s.Status == status
                       select s;
        return this;
    }

    public IImportSessionQuery Clone()
    {
        return new CoreImportSessionQuery(currentQuery, LogManager, TimeProvider, DB, CurrentProfile);
    }

    public IImportSessionQuery Search(string search)
    {
        currentQuery = from s in currentQuery
                       where s.OriginalFileName.IndexOf(search) >= 0 ||
                             s.EntityType.IndexOf(search) >= 0
                       select s;
        return this;
    }

    public IImportSessionQuery Skip(int value) => SkipHelper(value);

    public IImportSessionQuery Take(int value) => TakeHelper(value);

    public IImportSessionQuery SortBy(string column, bool isAsc)
    {
        this.IsAsc = isAsc;

        if (string.Compare(column, "createdate", true) == 0)
        {
            currentQuery = isAsc
                ? (from s in currentQuery orderby s.CreateDate select s)
                : (from s in currentQuery orderby s.CreateDate descending select s);
            SortedBy = "createdate";
        }
        else if (string.Compare(column, "entitytype", true) == 0)
        {
            currentQuery = isAsc
                ? (from s in currentQuery orderby s.EntityType select s)
                : (from s in currentQuery orderby s.EntityType descending select s);
            SortedBy = "entitytype";
        }
        else if (string.Compare(column, "status", true) == 0)
        {
            currentQuery = isAsc
                ? (from s in currentQuery orderby s.Status select s)
                : (from s in currentQuery orderby s.Status descending select s);
            SortedBy = "status";
        }
        else if (string.Compare(column, "originalfilename", true) == 0)
        {
            currentQuery = isAsc
                ? (from s in currentQuery orderby s.OriginalFileName select s)
                : (from s in currentQuery orderby s.OriginalFileName descending select s);
            SortedBy = "originalfilename";
        }
        else
        {
            throw new ArgumentException("Invalid column name");
        }

        return this;
    }
}
