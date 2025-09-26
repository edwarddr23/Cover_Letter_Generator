using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoverLetterGenerator.Models;
using CoverLetterGenerator.Services;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office2016.Drawing.Command;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    private string _firstName = string.Empty;
    [ObservableProperty]
    private string _lastName = string.Empty;

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
        Errors.Clear();
        Warnings.Clear();
        SettingsList.Clear();
        var settings = _settingsService.LoadSettings();
        // TemplatesPath = settings.TemplatesPath;
        // OutputPath = settings.OutputPath;

        // Populate our local variables with what's saved in settings.
        var targetProps = this.GetType().GetProperties();
        foreach (var settProp in settings.GetType().GetProperties())
        {
            var target = targetProps.FirstOrDefault(p =>
                p.Name == settProp.Name &&
                p.PropertyType.IsAssignableFrom(settProp.PropertyType) &&
                p.CanWrite
            );
            if (target != null)
            {
                var value = settProp.GetValue(settings);
                target.SetValue(this, value);
            }
        }

        // Populate the saved sattings list.
        foreach (var prop in settings.GetType().GetProperties())
        {
            object? value = prop.GetValue(settings);
            SettingsList.Add(new KeyValueItem
            {
                Key = prop.Name,
                Value = value?.ToString() ?? string.Empty
            });
        }
    }

    [RelayCommand]
    private async Task SaveSettings()
    {
        Errors.Clear();
        Warnings.Clear();
        bool outputPathExists = true;

        // Validate TemplatesPath
        if (string.IsNullOrWhiteSpace(TemplatesPath))
        {
            Errors.Add(new KeyValueItem { Key = "TemplatesPath", Value = "Template directory must be specified." });
        }
        else if (!Path.IsPathFullyQualified(TemplatesPath))
        {
            Errors.Add(new KeyValueItem { Key = "TemplatesPath", Value = "Template directory must be an absolute path." });
        }
        else
        {
            try
            {
                Path.GetFullPath(TemplatesPath);
                if (!Directory.Exists(TemplatesPath))
                {
                    Errors.Add(new KeyValueItem { Key = "TemplatesPath", Value = "Template directory does not exist." });
                }
            }
            catch (Exception ex)
            {
                Errors.Add(new KeyValueItem { Key = "TemplatesPath", Value = $"Invalid path format: {ex.Message}" });
            }
        }

        // Validate OutputPath
        if (string.IsNullOrWhiteSpace(OutputPath))
        {
            Errors.Add(new KeyValueItem { Key = "OutputPath", Value = "Output directory is not specified." });
        }
        else if (!Path.IsPathFullyQualified(OutputPath))
        {
            Errors.Add(new KeyValueItem { Key = "OutputPath", Value = "Output directory must be an absolute path." });
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
            }
        }

        // Validate first name.
        if (string.IsNullOrWhiteSpace(FirstName))
        {
            Errors.Add(new KeyValueItem
            {
                Key = "FirstName",
                Value = "First Name must be specified"
            });
        }

        // Validate last name.
        if (string.IsNullOrWhiteSpace(LastName))
        {
            Errors.Add(new KeyValueItem
            {
                Key = "LastName",
                Value = "Last Name must be specified"
            });
        }

        if (Errors.Count > 0)
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

        // Populate the saved sattings list.
        var newSettings = new AppSettings();
        var targetProps = this.GetType().GetProperties();
        foreach (var newSettProp in newSettings.GetType().GetProperties())
        {
            var sourceProp = targetProps.FirstOrDefault(p =>
                p.Name == newSettProp.Name &&
                newSettProp.PropertyType.IsAssignableFrom(newSettProp.PropertyType) &&
                p.CanRead
            );
            if (sourceProp != null)
            {
                var value = sourceProp.GetValue(this);
                newSettProp.SetValue(newSettings, value);
            }
        }

        Debug.WriteLine($"Saving settings: TemplatesPath={newSettings.TemplatesPath}, OutputPath={newSettings.OutputPath}");
        _settingsService.SaveSettings(newSettings);
        LoadSettings();
    }
}