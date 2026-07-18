using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public class CoreRefreshTokenQuery : CoreBaseQuery<RefreshToken, CoreRefreshTokenQuery>, IRefreshTokenQuery
{
    private CoreRefreshTokenQuery(IQueryable<RefreshToken> q, IScopeManager logManager,
                         ITimeProvider timeProvider,
                         ApplicationDbContext dB,
                         UserProfile currentProfile)
        : base(q, logManager, timeProvider, dB, currentProfile)
    {
    }

    public CoreRefreshTokenQuery(IScopeManager logManager,
                             ITimeProvider timeProvider,
                             ApplicationDbContext dB,
                             UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
    }

    public IRefreshTokenQuery ByTokenHash(string tokenHash)
    {
        currentQuery = from rt in currentQuery
                       where rt.TokenHash == tokenHash
                       select rt;
        return this;
    }

    public IRefreshTokenQuery ByUserId(Guid userId)
    {
        currentQuery = from rt in currentQuery
                       where rt.UserId == userId
                       select rt;
        return this;
    }

    public IRefreshTokenQuery ActiveOnly()
    {
        currentQuery = from rt in currentQuery
                       where rt.RevokedAt == null
                       select rt;
        return this;
    }

    public IRefreshTokenQuery Clone()
    {
        return new CoreRefreshTokenQuery(currentQuery, LogManager, TimeProvider, DB, CurrentProfile);
    }

    public RefreshToken CreateNew()
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
        };
    }

    public async Task<int> RevokeAllActiveForUserAsync(Guid userId, string? revokedByIp, DateTime revokedAt)
    {
        var activeTokens = await (from rt in DB.RefreshTokens
                                  where rt.UserId == userId && rt.RevokedAt == null
                                  select rt).ToListAsync();

        foreach (var token in activeTokens)
        {
            token.RevokedAt = revokedAt;
            token.RevokedByIp = revokedByIp;
        }

        await DB.SaveChangesAsync();

        return activeTokens.Count;
    }
}
