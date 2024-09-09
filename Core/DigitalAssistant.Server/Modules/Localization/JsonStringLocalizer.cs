using DigitalAssistant.Abstractions.Localization;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Globalization;

namespace DigitalAssistant.Server.Modules.Localization;

public class JsonStringLocalizer<T> : IJsonStringLocalizer
{
    protected readonly ConcurrentDictionary<string, List<string>> Cache = new();
    protected readonly JsonSerializer Serializer = new();

    public bool LoadCompleteJsonIntoCacheByUse { get; set; }
    protected ConcurrentDictionary<string, bool> AllJsonLoadedIntoCache { get; set; } = [];
    protected readonly object LoadCacheLock = new();

    public LocalizedString this[string name]
    {
        get
        {
            var value = GetString(name)[0];
            return new LocalizedString(name, value ?? name, value == null);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var actualValue = this[name];
            return !actualValue.ResourceNotFound
                ? new LocalizedString(name, string.Format(actualValue.Value, arguments), false)
                : actualValue;
        }
    }

    public List<string> GetTranslationList(string name)
    {
        return GetString(name);
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var translationRessourceFilePath = GetTranslationRessourceFilePath();
        if (!File.Exists(translationRessourceFilePath))
            yield break;

        using var fileStream = new FileStream(translationRessourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var streamReader = new StreamReader(fileStream);
        using var jsonReader = new JsonTextReader(streamReader);
        while (jsonReader.Read())
        {
            if (jsonReader.TokenType != JsonToken.PropertyName)
                continue;

            var key = jsonReader.Value as string;
            jsonReader.Read();

            string?[]? values = null;
            if (jsonReader.TokenType == JsonToken.String)
                values = [Serializer.Deserialize<string>(jsonReader)];
            else if (jsonReader.TokenType == JsonToken.StartArray)
            {
                var array = JArray.Load(jsonReader);
                if (array.Count > 0)
                {
                    if (array.First!.Type == JTokenType.String)
                        values = array.ToObject<string[]>();
                    else if (array.First!.Type == JTokenType.Object)
                        values = [array.ToString()];
                }
                else
                    values = [key];
            }

            if (key == null || values == null || values.Length == 0)
                continue;

            foreach (var value in values)
                if (value != null)
                    yield return new LocalizedString(key, value, false);
        }
    }

    protected List<string> GetString(string key)
    {
        var cacheKey = $"locale_{CultureInfo.CurrentUICulture.Name}_{key}";
        Cache.TryGetValue(cacheKey, out var cacheValue);
        if (cacheValue != null && cacheValue.Count > 0 && !String.IsNullOrEmpty(cacheValue[0]))
            return cacheValue;

        List<string> result = [key];
        if (LoadCompleteJsonIntoCacheByUse)
        {
            lock (LoadCacheLock)
            {
                AllJsonLoadedIntoCache.TryGetValue(CultureInfo.CurrentUICulture.Name, out bool cacheAlreadyLoaded);
                if (!cacheAlreadyLoaded)
                {
                    var allTranslations = GetAllStrings(true);
                    foreach (var translation in allTranslations)
                    {
                        var translationCacheKey = $"locale_{CultureInfo.CurrentUICulture.Name}_{translation.Name}";
                        if (Cache.ContainsKey(translationCacheKey))
                            Cache[translationCacheKey].Add(translation.Value);
                        else
                            Cache[translationCacheKey] = [translation.Value];
                    }

                    if (Cache.TryGetValue(cacheKey, out var value))
                        result = value;

                    AllJsonLoadedIntoCache[CultureInfo.CurrentUICulture.Name] = true;
                }
            }
        }
        else
        {
            var translationRessourceFilePath = GetTranslationRessourceFilePath();
            if (File.Exists(translationRessourceFilePath))
                result = [GetValueFromJSON(key, translationRessourceFilePath)];
        }

        Cache[cacheKey] = result;
        return result;
    }

    protected string GetValueFromJSON(string propertyName, string filePath)
    {
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var streamReader = new StreamReader(fileStream);
        using var jsonReader = new JsonTextReader(streamReader);
        while (jsonReader.Read())
        {
            if (jsonReader.TokenType == JsonToken.PropertyName && jsonReader.Value as string == propertyName)
            {
                jsonReader.Read();
                return Serializer.Deserialize<string>(jsonReader) ?? String.Empty;
            }
        }

        return propertyName;
    }

    protected string? GetTranslationRessourceFilePath()
    {
        var type = typeof(T);
        var rootNameSpace = type.Assembly.GetName().Name;
        var baseNameSpace = type.Namespace
            ?.Replace($"{rootNameSpace}.", String.Empty)
            ?.Replace($"{rootNameSpace}", String.Empty)
            ?.Replace('.', Path.DirectorySeparatorChar);
        var relativePath = @$"{Path.Combine(baseNameSpace ?? String.Empty, type.Name)}.{CultureInfo.CurrentUICulture.TwoLetterISOLanguageName}.json";

        return Path.Combine(Path.GetDirectoryName(type.Assembly.Location)!, relativePath);
    }

    public void ClearCache()
    {
        Cache.Clear();
    }
}
