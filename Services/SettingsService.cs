using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Avalonia.Metadata;

public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;

    public SettingsService()
    {
        var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CoverLetterGenerator");
        Directory.CreateDirectory(appDataFolder);

        _settingsPath = Path.Combine(appDataFolder, "settings.json");
    }
    public void SaveSettings(AppSettings settings)
    {
        var settings_json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsPath, settings_json);
        Debug.WriteLine("[SettingsService] Saved settings successfully!");
    }

    public AppSettings LoadSettings()
    {
        if (!File.Exists(_settingsPath))
        {
            return new AppSettings();
        }
        var json = File.ReadAllText(_settingsPath);
        return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
    }
}