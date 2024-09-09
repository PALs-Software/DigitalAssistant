using Microsoft.AspNetCore.DataProtection;

namespace DigitalAssistant.Abstractions.Services;

public class DataProtectionService : IDataProtectionService
{
    #region Injects
    protected readonly IDataProtector Protector;
    #endregion

    public DataProtectionService(IDataProtectionProvider provider)
    {
        Protector = provider.CreateProtector(GetType().FullName ?? nameof(DataProtectionService));
    }

    public string? Protect(string? data)
    {
        if (data == null)
            return null;

        return Protector.Protect(data);
    }

    public string? Unprotect(string? data)
    {
        if (data == null)
            return null;

        return Protector.Unprotect(data);
    }
}