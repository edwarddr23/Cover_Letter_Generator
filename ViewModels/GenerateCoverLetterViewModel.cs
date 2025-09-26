using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoverLetterGenerator.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;

namespace CoverLetterGenerator.ViewModels;

public partial class GenerateCoverLetterViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<string> _templateNames;

    [ObservableProperty]
    private string _selectedTemplate;

    [ObservableProperty]
    private string _jobSource;

    [ObservableProperty]
    private string _pageTitle = "Generate Cover Letter";

    private readonly ISettingsService _settingsService;

    public GenerateCoverLetterViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        TemplateNames = new ObservableCollection<string>();
        LoadTemplates();
    }

    private void LoadTemplates()
    {
        var settings = _settingsService.LoadSettings();
        string templatePath = settings.TemplatesPath;
        if (!string.IsNullOrEmpty(templatePath) && Directory.Exists(templatePath))
        {
            TemplateNames.Clear();
            foreach (var dir in Directory.GetDirectories(templatePath))
            {
                TemplateNames.Add(Path.GetFileName(dir));
            }
        }
    }

    [RelayCommand]
    private void GenerateCoverLetter()
    {
        if (!string.IsNullOrEmpty(SelectedTemplate) && !string.IsNullOrEmpty(JobSource))
        {
            System.Diagnostics.Debug.WriteLine($"Selected Template: {SelectedTemplate}, Job Source: {JobSource}");
            // Add cover letter generation logic here
        }
    }
}