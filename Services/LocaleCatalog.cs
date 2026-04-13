using System.Text.Json;
using MusicStoreApp.Models;

namespace MusicStoreApp.Services;

public sealed class LocaleCatalog
{
    private readonly IReadOnlyDictionary<string, LocaleDefinition> _locales;

    public LocaleCatalog(IWebHostEnvironment environment)
    {
        var localeDirectory = Path.Combine(environment.ContentRootPath, "Data", "Locales");
        var locales = new Dictionary<string, LocaleDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var filePath in Directory.EnumerateFiles(localeDirectory, "*.json"))
        {
            var json = File.ReadAllText(filePath);
            var locale = JsonSerializer.Deserialize<LocaleDefinition>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (locale is not null)
            {
                locales[locale.Code] = locale;
            }
        }

        _locales = locales;
    }

    public IReadOnlyCollection<LocaleDefinition> GetAll()
    {
        return _locales.Values.OrderBy(locale => locale.DisplayName).ToArray();
    }

    public LocaleDefinition Get(string code)
    {
        if (_locales.TryGetValue(code, out var locale))
        {
            return locale;
        }

        return _locales["en-US"];
    }
}
