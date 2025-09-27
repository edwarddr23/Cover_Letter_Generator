using CommunityToolkit.Mvvm.ComponentModel;

namespace CoverLetterGenerator.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _pageTitle = "Home";
}