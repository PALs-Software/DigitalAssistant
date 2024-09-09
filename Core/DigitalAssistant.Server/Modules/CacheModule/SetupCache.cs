using BlazorBase.Abstractions.CRUD.Interfaces;
using DigitalAssistant.Server.Modules.Setups.Models;

namespace DigitalAssistant.Server.Modules.CacheModule;

public class SetupCache
{
    #region Member
    private ReaderWriterLockSlim CacheLock = new();

    protected Setup? _Setup;
    #endregion

    public Setup? Setup
    {
        get
        {
            CacheLock.EnterReadLock();
            try { return _Setup; } finally { CacheLock.ExitReadLock(); };
        }
    }

    public async Task RefreshSetupCacheAsync(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<IBaseDbContext>();
        var newSetup = await dbContext.FirstOrDefaultAsync<Setup>(asNoTracking: true);

        CacheLock.EnterWriteLock();
        try
        {
            _Setup = newSetup;
        }
        finally
        {
            CacheLock.ExitWriteLock();
        }
    }

}
