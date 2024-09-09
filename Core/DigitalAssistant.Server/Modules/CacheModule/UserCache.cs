using BlazorBase.Abstractions.CRUD.Interfaces;
using DigitalAssistant.Server.Modules.Users;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace DigitalAssistant.Server.Modules.CacheModule;

public class UserCache
{
    public ConcurrentDictionary<string, User> Users { get; set; } = null!;

    public Task InitUserCacheAsync(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<IBaseDbContext>();
        return dbContext.SetAsync((IQueryable<User> query) =>
        {
            Users = new(query.AsNoTracking()
                             .ToDictionary(entry => entry.Email, entry => entry));
        });
    }
}
