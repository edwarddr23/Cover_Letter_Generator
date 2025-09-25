using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CoverLetterGenerator.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase currentViewModel;
    private ISettingsService _iSettingsService;

    public MainWindowViewModel(ISettingsService iSettingsService)
    {
        _iSettingsService = iSettingsService;
        CurrentViewModel = new HomeViewModel();
    }

    [RelayCommand]
    private void NavigateHome() => CurrentViewModel = new HomeViewModel();

    [RelayCommand]
    private void NavigateGenerateCoverLetter() => CurrentViewModel = new GenerateCoverLetterViewModel();

    [RelayCommand]
    private void NavigateSettings() => CurrentViewModel = new SettingsViewModel(_iSettingsService);
}