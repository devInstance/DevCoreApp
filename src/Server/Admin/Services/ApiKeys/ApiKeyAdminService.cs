using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.Authentication;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;
using DevInstance.DevCoreApp.Shared.Model.ApiKeys;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using DevInstance.WebServiceToolkit.Common.Model;
using DevInstance.WebServiceToolkit.Common.Tools;
using DevInstance.WebServiceToolkit.Database.Queries.Extensions;
using DevInstance.WebServiceToolkit.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.ApiKeys;

[BlazorService]
public class ApiKeyAdminService : BaseService, IApiKeyAdminService
{
    private IScopeLog log;

    public ApiKeyAdminService(IScopeManager logManager,
                               ITimeProvider timeProvider,
                               IQueryRepository query,
                               IAuthorizationContext authorizationContext)
        : base(logManager, timeProvider, query, authorizationContext)
    {
        log = logManager.CreateLogger(this);
    }

    public async Task<ServiceActionResult<ModelList<ApiKeyItem>>> GetKeysAsync(
        int top, int page, string[]? sortBy = null, string? search = null)
    {
        using var l = log.TraceScope();

        var query = Repository.GetApiKeyQuery(AuthorizationContext.CurrentProfile);

        if (!string.IsNullOrEmpty(search))
            query = query.Search(search);

        var sortField = sortBy?.FirstOrDefault()?.TrimStart('-');
        var isAsc = sortBy?.FirstOrDefault()?.StartsWith("-") != true;

        query = !string.IsNullOrEmpty(sortField)
            ? query.SortBy(sortField, isAsc)
            : query.SortBy("createdate", false);

        var totalCount = await query.Clone().Select().CountAsync();
        var keys = await query.Paginate(top, page).Select()
            .Include(ak => ak.CreatedBy)
            .ToListAsync();

        var items = keys.Select(ak => ak.ToView()).ToArray();

        var modelList = ModelListResult.CreateList(items, totalCount, top, page, sortBy, search);
        return ServiceActionResult<ModelList<ApiKeyItem>>.OK(modelList);
    }

    public async Task<ServiceActionResult<ApiKeyCreateResult>> CreateKeyAsync(ApiKeyItem item)
    {
        using var l = log.TraceScope();

        // Generate the plain-text key: dca_ prefix + 40 random hex chars
        var randomBytes = RandomNumberGenerator.GetBytes(20);
        var plainTextKey = "dca_" + Convert.ToHexStringLower(randomBytes);
        var prefix = plainTextKey[..8];
        var keyHash = HashKey(plainTextKey);

        var query = Repository.GetApiKeyQuery(AuthorizationContext.CurrentProfile);
        var apiKey = query.CreateNew();
        apiKey.ToRecord(item);
        apiKey.KeyHash = keyHash;
        apiKey.Prefix = prefix;
        apiKey.CreatedById = AuthorizationContext.CurrentProfile.Id;

        await query.AddAsync(apiKey);
        l.I($"API key created: {apiKey.Name} ({prefix})");

        var result = new ApiKeyCreateResult
        {
            Key = apiKey.ToView(),
            PlainTextKey = plainTextKey
        };

        return ServiceActionResult<ApiKeyCreateResult>.OK(result);
    }

    public async Task<ServiceActionResult<bool>> RevokeKeyAsync(string id)
    {
        using var l = log.TraceScope();

        var query = Repository.GetApiKeyQuery(AuthorizationContext.CurrentProfile);
        var apiKey = await query.ByPublicId(id).Select().FirstOrDefaultAsync();

        if (apiKey == null)
            throw new RecordNotFoundException("API key not found.");

        if (apiKey.IsRevoked)
            throw new BadRequestException("API key is already revoked.");

        apiKey.IsRevoked = true;
        apiKey.RevokedAt = TimeProvider.CurrentTime;

        await query.UpdateAsync(apiKey);
        l.I($"API key revoked: {apiKey.Name} ({apiKey.Prefix})");

        return ServiceActionResult<bool>.OK(true);
    }

    internal static string HashKey(string plainTextKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainTextKey));
        return Convert.ToHexStringLower(bytes);
    }
}
