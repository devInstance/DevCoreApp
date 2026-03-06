using System.Linq;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.WebServiceToolkit.Database.Queries;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface IApiKeyQuery : IModelQuery<ApiKey, IApiKeyQuery>,
        IQSearchable<IApiKeyQuery>,
        IQPageable<IApiKeyQuery>,
        IQSortable<IApiKeyQuery>
{
    IQueryable<ApiKey> Select();

    IApiKeyQuery ByKeyHash(string keyHash);
    IApiKeyQuery ByCreatedById(System.Guid userId);
    IApiKeyQuery ActiveOnly();
}
