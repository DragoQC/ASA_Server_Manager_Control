using System.Text.Json;

namespace asa_server_controller.Services;

public class LanguageService
{
    private readonly Dictionary<string, JsonElement> _translations = new();
    private string _currentLanguage = "en";

    public LanguageService()
    {
        LoadLanguage("en");
    }

    public void LoadLanguage(string languageCode)
    {
        _currentLanguage = languageCode;
        var assembly = typeof(LanguageService).Assembly;
        var resourceName = $"asa_server_controller.Resources.Lang.{languageCode}.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            if (languageCode != "en")
            {
                return;
            }
            throw new FileNotFoundException($"Language resource not found: {resourceName}");
        }

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("gameusersettings", out var gameUserSettings))
        {
            _translations["gameusersettings"] = gameUserSettings;
        }
    }

    public string GetFieldTitle(string fieldName)
    {
        if (_translations.TryGetValue("gameusersettings", out var translations))
        {
            if (translations.TryGetProperty("fields", out var fields) &&
                fields.TryGetProperty(fieldName, out var field) &&
                field.TryGetProperty("title", out var title))
            {
                return title.GetString() ?? fieldName;
            }
        }
        return fieldName;
    }

    public string GetFieldDescription(string fieldName)
    {
        if (_translations.TryGetValue("gameusersettings", out var translations))
        {
            if (translations.TryGetProperty("fields", out var fields) &&
                fields.TryGetProperty(fieldName, out var field) &&
                field.TryGetProperty("description", out var desc))
            {
                return desc.GetString() ?? string.Empty;
            }
        }
        return string.Empty;
    }

    public string GetSectionTitle(string sectionName)
    {
        if (_translations.TryGetValue("gameusersettings", out var translations))
        {
            if (translations.TryGetProperty("sections", out var sections) &&
                sections.TryGetProperty(sectionName, out var section) &&
                section.TryGetProperty("title", out var title))
            {
                return title.GetString() ?? sectionName;
            }
        }
        return sectionName;
    }

    public string GetSectionDescription(string sectionName)
    {
        if (_translations.TryGetValue("gameusersettings", out var translations))
        {
            if (translations.TryGetProperty("sections", out var sections) &&
                sections.TryGetProperty(sectionName, out var section) &&
                section.TryGetProperty("description", out var desc))
            {
                return desc.GetString() ?? string.Empty;
            }
        }
        return string.Empty;
    }

    public string GetCurrentLanguage() => _currentLanguage;
}
