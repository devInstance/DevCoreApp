using System;
using System.Linq;
using System.Threading.Tasks;
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
    IApiKeyQuery ByCreatedById(Guid userId);
    IApiKeyQuery ActiveOnly();

    /// <summary>Sets LastUsedAt on the key with the given PublicId and saves (no-op if missing).</summary>
    Task TouchLastUsedAsync(string publicId, DateTime lastUsedAt);
}
