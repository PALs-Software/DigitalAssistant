namespace DigitalAssistant.Abstractions.Services;

public interface IDataProtectionService
{
    string? Protect(string? data);

    string? Unprotect(string? data);
}