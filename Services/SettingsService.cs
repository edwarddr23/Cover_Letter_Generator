using CoverLetterGenerator.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace CoverLetterGenerator.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;

    public event EventHandler? SettingsChanged;

    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appData, "CoverLetterGenerator");

        if (!Directory.Exists(appFolder))
            Directory.CreateDirectory(appFolder);

        _settingsFilePath = Path.Combine(appFolder, "settings.json");
    }

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
            Debug.WriteLine("Failed to load settings.");
        }
        return new AppSettings();
    }

    public void SaveSettings(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            Debug.WriteLine("Failed to save settings.");
        }
    }
}
