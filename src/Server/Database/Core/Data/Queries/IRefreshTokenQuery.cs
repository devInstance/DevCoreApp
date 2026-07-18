using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.WebServiceToolkit.Database.Queries;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface IRefreshTokenQuery : IModelQuery<RefreshToken, IRefreshTokenQuery>
{
    IQueryable<RefreshToken> Select();

    IRefreshTokenQuery ByTokenHash(string tokenHash);
    IRefreshTokenQuery ByUserId(Guid userId);
    IRefreshTokenQuery ActiveOnly();

    /// <summary>
    /// Revokes (stamps RevokedAt/RevokedByIp on) every not-yet-revoked token for the user
    /// in a single save. Returns the number of tokens revoked.
    /// </summary>
    Task<int> RevokeAllActiveForUserAsync(Guid userId, string? revokedByIp, DateTime revokedAt);
}
