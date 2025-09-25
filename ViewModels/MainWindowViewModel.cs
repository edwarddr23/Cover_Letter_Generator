using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CoverLetterGenerator.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase currentViewModel;

    public MainWindowViewModel()
    {
        CurrentViewModel = new HomeViewModel();
    }

    [RelayCommand]
    private void NavigateHome() => CurrentViewModel = new HomeViewModel();

    [RelayCommand]
    private void NavigateGenerateCoverLetter() => CurrentViewModel = new GenerateCoverLetterViewModel();

    [RelayCommand]
    private void NavigateSettings() => CurrentViewModel = new SettingsViewModel();
}