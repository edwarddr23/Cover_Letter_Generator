using CoverLetterGenerator.Models;
using System.IO;
using System.Text.Json;

namespace CoverLetterGenerator.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "settings.json");

    public AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // Log error if needed
        }
        return new AppSettings();
    }

    public void SaveSettings(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
        }
        catch
        {
            // Log error if needed
        }
    }
}