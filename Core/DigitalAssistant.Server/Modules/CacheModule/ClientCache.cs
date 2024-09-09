using BlazorBase.Abstractions.CRUD.Interfaces;
using DigitalAssistant.Server.Modules.Clients.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace DigitalAssistant.Server.Modules.CacheModule;

public class ClientCache
{
    #region Access Tokens

    public ConcurrentDictionary<string, Client> Clients { get; set; } = null!;

    public Task InitClientCacheAsync(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<IBaseDbContext>();
        return dbContext.SetAsync((IQueryable<Client> query) =>
        {
            Clients = new(query.AsNoTracking()
                                    .ToDictionary(entry => entry.TokenHash, entry => entry));
        });
    }

    #endregion

}
