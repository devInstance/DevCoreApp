using System.Linq;
using DevInstance.DevCoreApp.Server.Database.Core.Models.ImportExport;
using DevInstance.DevCoreApp.Shared.Model.ImportExport;
using DevInstance.WebServiceToolkit.Database.Queries;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface IImportSessionQuery : IModelQuery<ImportSession, IImportSessionQuery>,
        IQSearchable<IImportSessionQuery>,
        IQPageable<IImportSessionQuery>,
        IQSortable<IImportSessionQuery>
{
    IQueryable<ImportSession> Select();

    IImportSessionQuery ByEntityType(string entityType);
    IImportSessionQuery ByStatus(ImportSessionStatus status);
}
