using BlazorBase.Abstractions.CRUD.Interfaces;
using DigitalAssistant.Server.Modules.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace DigitalAssistant.Server.Modules.CacheModule;

public class ApiCache
{
    #region Access Tokens

    public ConcurrentDictionary<string, AccessToken> AccessTokens { get; set; } = null!;

    public Task InitAccessTokenCacheAsync(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<IBaseDbContext>();
        return dbContext.SetAsync((IQueryable<AccessToken> query) =>
        {
            AccessTokens = new(query.AsNoTracking()
                                    .ToDictionary(entry => entry.TokenHash, entry => entry));
        });
    }

    #endregion

}
