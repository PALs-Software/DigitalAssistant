namespace DigitalAssistant.Server.Modules.CacheModule;

public static class Cache
{
    #region Member
    private static readonly SetupCache _SetupCache = new();
    private static readonly UserCache _UserCache = new();
    private static readonly ClientCache _ClientCache = new();
    #endregion

    #region Properties
    public static UserCache UserCache { get { return _UserCache; } }
    public static SetupCache SetupCache { get { return _SetupCache; } }
    public static ClientCache ClientCache { get { return _ClientCache; } }
    #endregion
}
