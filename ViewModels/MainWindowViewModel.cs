using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoverLetterGenerator.Models;
using CoverLetterGenerator.Services;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2013.PowerPoint.Roaming;

namespace CoverLetterGenerator.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isPaneOpen = true;
    [ObservableProperty]
    private ListItemTemplate _selectedMenuEntry;
    partial void OnSelectedMenuEntryChanged(ListItemTemplate value)
    {
        if (value is null)
        {
            return;
        }
        if (value.ModelType == typeof(HomeViewModel))
            CurrentViewModel = _homeViewModel;
        else if (value.ModelType == typeof(GenerateCoverLetterViewModel))
            CurrentViewModel = _generateCoverLetterViewModel;
        else if (value.ModelType == typeof(SettingsViewModel))
            CurrentViewModel = _settingsViewModel;
    }
    [ObservableProperty]
    private ViewModelBase _currentViewModel;
    // public ObservableCollection<MenuEntry> MenuEntries { get; }
    public ObservableCollection<ListItemTemplate> MenuEntries { get; } = new()
    {
        new ListItemTemplate(typeof(HomeViewModel), "Home", "home_regular"),
        new ListItemTemplate(typeof(GenerateCoverLetterViewModel), "Generate Cover Letter", "document_edit_regular"),
        new ListItemTemplate(typeof(SettingsViewModel), "Settings", "settings_regular"),
    };

    private readonly HomeViewModel _homeViewModel;
    private readonly GenerateCoverLetterViewModel _generateCoverLetterViewModel;
    private readonly SettingsViewModel _settingsViewModel;

    public MainWindowViewModel(ISettingsService settingsService, IDialogService dialogService)
    {
        _homeViewModel = new HomeViewModel();
        _generateCoverLetterViewModel = new GenerateCoverLetterViewModel(settingsService);
        _settingsViewModel = new SettingsViewModel(settingsService, dialogService);
        CurrentViewModel = _homeViewModel;
    }

    [RelayCommand]
    private void OpenPane()
    {
        IsPaneOpen = !IsPaneOpen;
    }
    [RelayCommand]
    private void NavigateToHome()
    {
        CurrentViewModel = _homeViewModel;
    }

    [RelayCommand]
    private void NavigateToGenerateCoverLetter()
    {
        CurrentViewModel = _generateCoverLetterViewModel;
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        CurrentViewModel = _settingsViewModel;
    }
}

public class ListItemTemplate
{
    public ListItemTemplate(Type type, string label, string iconKey)
    {
        ModelType = type;
        Label = label;
        Application.Current!.TryFindResource(iconKey, out var res);
        Icon = (StreamGeometry)res!;
    }
    public string Label { get; }
    public Type ModelType { get; }
    public StreamGeometry Icon { get; }
}