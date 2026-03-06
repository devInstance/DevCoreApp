using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Model.ApiKeys;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;

public static class ApiKeyDecorators
{
    public static ApiKeyItem ToView(this ApiKey key)
    {
        return new ApiKeyItem
        {
            Id = key.PublicId,
            Name = key.Name,
            Prefix = key.Prefix,
            Scopes = key.Scopes,
            ExpiresAt = key.ExpiresAt,
            LastUsedAt = key.LastUsedAt,
            IsRevoked = key.IsRevoked,
            RevokedAt = key.RevokedAt,
            CreatedByName = key.CreatedBy != null
                ? $"{key.CreatedBy.FirstName} {key.CreatedBy.LastName}".Trim()
                : null,
            CreateDate = key.CreateDate
        };
    }

    public static ApiKey ToRecord(this ApiKey key, ApiKeyItem item)
    {
        key.Name = item.Name;
        key.Scopes = item.Scopes;
        key.ExpiresAt = item.ExpiresAt;
        return key;
    }
}
