using Microsoft.Extensions.Localization;

namespace DigitalAssistant.Abstractions.Localization;

public interface IJsonStringLocalizer : IStringLocalizer
{
    bool LoadCompleteJsonIntoCacheByUse { get; set; }

    public List<string> GetTranslationList(string name);

    void ClearCache();
}
