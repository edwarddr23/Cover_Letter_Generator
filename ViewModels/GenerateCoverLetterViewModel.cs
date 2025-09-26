using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoverLetterGenerator.Models;
using CoverLetterGenerator.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace CoverLetterGenerator.ViewModels;

public partial class GenerateCoverLetterViewModel : ViewModelBase
{
    // [ObservableProperty]
    public ObservableCollection<ChoiceItem> TemplateNames { get; } = new();

    // [ObservableProperty]
    private string _selectedTemplate = string.Empty;
    public string SelectedTemplate
    {
        get => _selectedTemplate;
        set => SetProperty(ref _selectedTemplate, value);
    }

    [ObservableProperty]
    private string? _jobSource;

    [ObservableProperty]
    private string _pageTitle = "Generate Cover Letter";

    private readonly ISettingsService _settingsService;

    private void SettingsService_SettingsChanged(object? sender, EventArgs e)
    {
        LoadTemplates();
    }

    public GenerateCoverLetterViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _settingsService.SettingsChanged += SettingsService_SettingsChanged;
        LoadTemplates();
    }

    private void LoadTemplates()
    {
        var settings = _settingsService.LoadSettings();
        string? templatePath = settings.TemplatesPath;
        if (!string.IsNullOrEmpty(templatePath) && Directory.Exists(templatePath))
        {
            TemplateNames.Clear();
            foreach (var dir in Directory.GetDirectories(templatePath))
            {
                TemplateNames.Add(new ChoiceItem
                {
                    Name = Path.GetFileName(dir),
                    IsSelected = false,
                    ParentViewModel = this
                });
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