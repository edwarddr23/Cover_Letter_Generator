using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoverLetterGenerator.Services;

namespace CoverLetterGenerator.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentViewModel;

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