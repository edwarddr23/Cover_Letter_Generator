using System;
using System.Diagnostics;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Templates;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CoverLetterGenerator.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    public string PageTitle => "Settings";
    private AppSettings _savedSettings;
    public AppSettings SavedSettings
    {
        get => _savedSettings;
        private set
        {
            SetProperty(ref _savedSettings, value);
        }
    }

    [ObservableProperty]
    private string _templatesPath = string.Empty;
    private readonly ISettingsService _iSettingsService;

    public SettingsViewModel(ISettingsService iSettingsService)
    {
        _iSettingsService = iSettingsService;
        SavedSettings = _iSettingsService.LoadSettings();
        TemplatesPath = SavedSettings.TemplatesPath;
    }

    [RelayCommand]
    public void SaveSettings()
    {
        var new_settings = new AppSettings
        {
            TemplatesPath = TemplatesPath
        };

        Debug.WriteLine($"Saving settings: {new_settings}...");

        _iSettingsService.SaveSettings(new_settings);

        SavedSettings = new_settings;
    }

    [RelayCommand]
    public void DiscardChanges()
    {
        TemplatesPath = SavedSettings.TemplatesPath;
    }
}