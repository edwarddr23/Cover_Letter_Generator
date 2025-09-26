using CommunityToolkit.Mvvm.ComponentModel;
using CoverLetterGenerator.ViewModels;

namespace CoverLetterGenerator.Models;

public partial class ChoiceItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isSelected;

    public GenerateCoverLetterViewModel? ParentViewModel { get; set; }

    partial void OnIsSelectedChanged(bool value)
    {
        if (value && ParentViewModel != null)
        {
            ParentViewModel.SelectedTemplate = Name;
        }
    }
}