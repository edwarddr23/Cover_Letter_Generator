using CommunityToolkit.Mvvm.ComponentModel;

namespace CoverLetterGenerator.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase currentViewModel;

    public MainWindowViewModel()
    {
        CurrentViewModel = new HomeViewModel();
    }
}
