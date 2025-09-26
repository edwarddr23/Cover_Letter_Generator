using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoverLetterGenerator.Models;
using CoverLetterGenerator.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CoverLetterGenerator.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _pageTitle = "Settings";

    [ObservableProperty]
    private string _templatesPath = string.Empty;

    [ObservableProperty]
    private string _outputPath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<KeyValueItem> _errors = new();

    [ObservableProperty]
    private ObservableCollection<KeyValueItem> _warnings = new();

    public ObservableCollection<KeyValueItem> SettingsList { get; } = new();

    private readonly ISettingsService _settingsService;
    private readonly IDialogService _dialogService;

    public SettingsViewModel(ISettingsService settingsService, IDialogService dialogService)
    {
        _settingsService = settingsService;
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = _settingsService.LoadSettings();
        TemplatesPath = settings.TemplatesPath;
        OutputPath = settings.OutputPath;
        Errors.Clear();
        Warnings.Clear();

        SettingsList.Clear();
        SettingsList.Add(new KeyValueItem { Key = "TemplatesPath", Value = TemplatesPath });
        SettingsList.Add(new KeyValueItem { Key = "OutputPath", Value = OutputPath });
    }

    [RelayCommand]
    private async Task SaveSettings()
    {
        Errors.Clear();
        Warnings.Clear();
        bool isValid = true;
        bool outputPathExists = true;

        // Validate TemplatesPath
        if (string.IsNullOrWhiteSpace(TemplatesPath))
        {
            Errors.Add(new KeyValueItem { Key = "TemplatesPath", Value = "Template directory is required." });
            isValid = false;
        }
        else if (!Path.IsPathFullyQualified(TemplatesPath))
        {
            Errors.Add(new KeyValueItem { Key = "TemplatesPath", Value = "Template directory must be an absolute path." });
            isValid = false;
        }
        else
        {
            try
            {
                Path.GetFullPath(TemplatesPath);
                if (!Directory.Exists(TemplatesPath))
                {
                    Errors.Add(new KeyValueItem { Key = "TemplatesPath", Value = "Template directory does not exist." });
                    isValid = false;
                }
            }
            catch (Exception ex)
            {
                Errors.Add(new KeyValueItem { Key = "TemplatesPath", Value = $"Invalid path format: {ex.Message}" });
                isValid = false;
            }
        }

        // Validate OutputPath
        if (string.IsNullOrWhiteSpace(OutputPath))
        {
            Errors.Add(new KeyValueItem { Key = "OutputPath", Value = "Output directory cannot be empty." });
            isValid = false;
        }
        else if (!Path.IsPathFullyQualified(OutputPath))
        {
            Errors.Add(new KeyValueItem { Key = "OutputPath", Value = "Output directory must be an absolute path." });
            isValid = false;
        }
        else
        {
            try
            {
                Path.GetFullPath(OutputPath);
                if (!Directory.Exists(OutputPath))
                {
                    outputPathExists = false;
                    Warnings.Add(new KeyValueItem { Key = "OutputPath", Value = "Output directory does not exist and will be created if confirmed." });
                }
            }
            catch (Exception ex)
            {
                Errors.Add(new KeyValueItem { Key = "OutputPath", Value = $"Invalid path format: {ex.Message}" });
                isValid = false;
            }
        }

        if (!isValid)
        {
            return;
        }

        if (!outputPathExists)
        {
            bool confirm = await _dialogService.ShowConfirmationDialogAsync(
                "Confirm Output Directory Creation",
                $"Output directory at \"{OutputPath}\" does not exist. Do you want it to be created?"
            );
            if (!confirm)
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(OutputPath);
            }
            catch (Exception ex)
            {
                Errors.Add(new KeyValueItem { Key = "OutputPath", Value = $"Failed to create output directory: {ex.Message}" });
                return;
            }
        }

        var newSettings = new AppSettings
        {
            TemplatesPath = Path.GetFullPath(TemplatesPath),
            OutputPath = Path.GetFullPath(OutputPath)
        };

        Debug.WriteLine($"Saving settings: TemplatesPath={newSettings.TemplatesPath}, OutputPath={newSettings.OutputPath}");
        _settingsService.SaveSettings(newSettings);
        LoadSettings();
    }
}