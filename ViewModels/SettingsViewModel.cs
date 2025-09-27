using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoverLetterGenerator.Models;
using CoverLetterGenerator.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

        List<KeyValueItem> parameterMap = new List<KeyValueItem>
        {
            new KeyValueItem("TemplatesPath", "Templates Path"),
            new KeyValueItem("OutputPath", "Output Path"),
            new KeyValueItem("FirstName", "First Name"),
            new KeyValueItem("LastName", "Last Name"),
        };
        // Populate the saved sattings list.
        foreach (var prop in settings.GetType().GetProperties())
        {
            object? value = prop.GetValue(settings);
            SettingsList.Add(new KeyValueItem(parameterMap.FirstOrDefault(kv => kv.Key == prop.Name).Value, value?.ToString() ?? string.Empty));
        }
    }

    [RelayCommand]
    private async Task SaveSettings()
    {
        Errors.Clear();
        Warnings.Clear();
        bool outputPathExists = true;

        // Trim inputs.
        TemplatesPath = TemplatesPath.Trim();
        OutputPath = OutputPath.Trim();
        FirstName = FirstName.Trim();
        LastName = LastName.Trim();

        // Validate TemplatesPath
        if (string.IsNullOrWhiteSpace(TemplatesPath))
        {
            Errors.Add(new KeyValueItem("TemplatesPath", "Template directory must be specified." ));
        }
        else if (!Path.IsPathFullyQualified(TemplatesPath))
        {
            Errors.Add(new KeyValueItem("TemplatesPath", "Template directory must be an absolute path." ));
        }
        else
        {
            try
            {
                Path.GetFullPath(TemplatesPath);
                if (!Directory.Exists(TemplatesPath))
                {
                    Errors.Add(new KeyValueItem("TemplatesPath", "Template directory does not exist."));
                }
            }
            catch (Exception ex)
            {
                Errors.Add(new KeyValueItem("TemplatesPath", $"Invalid path format: {ex.Message}"));
            }
        }

        // Validate OutputPath
        if (string.IsNullOrWhiteSpace(OutputPath))
        {
            Errors.Add(new KeyValueItem("OutputPath", "Output directory is not specified."));
        }
        else if (!Path.IsPathFullyQualified(OutputPath))
        {
            Errors.Add(new KeyValueItem("OutputPath", "Output directory must be an absolute path."));
        }
        else
        {
            try
            {
                Path.GetFullPath(OutputPath);
                if (!Directory.Exists(OutputPath))
                {
                    outputPathExists = false;
                    Warnings.Add(new KeyValueItem("OutputPath", "Output directory does not exist and will be created if confirmed."));
                }
            }
            catch (Exception ex)
            {
                Errors.Add(new KeyValueItem("OutputPath", $"Invalid path format: {ex.Message}"));
            }
        }

        // Validate first name.
        if (string.IsNullOrWhiteSpace(FirstName))
        {
            Errors.Add(new KeyValueItem("FirstName", "First Name must be specified"));
        }
        else if (!FirstName.All(c => Char.IsLetter(c) || Char.IsWhiteSpace(c)))
        {
            Errors.Add(new KeyValueItem("FirstName", "First Name must be alphabetic"));
        }

        // Validate last name.
        if (string.IsNullOrWhiteSpace(LastName))
        {
            Errors.Add(new KeyValueItem("LastName", "Last Name must be specified"));
        }
        else if (!LastName.All(c => Char.IsLetter(c) || Char.IsWhiteSpace(c)))
        {
            Errors.Add(new KeyValueItem("LastName", "Last Name must be alphabetic"));
        }

        // If there was an error, do not continue to save to settings.
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
                Errors.Add(new KeyValueItem("OutputPath", $"Failed to create output directory: {ex.Message}"));
                return;
            }
        }

        // Normalize the First and Last name properties.
        TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
        FirstName = textInfo.ToTitleCase(FirstName);
        FirstName = Regex.Replace(FirstName, @"\s+", " ");
        LastName = textInfo.ToTitleCase(LastName);
        LastName = Regex.Replace(LastName, @"\s+", " ");

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
