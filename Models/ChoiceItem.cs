using CommunityToolkit.Mvvm.ComponentModel;
using CoverLetterGenerator.ViewModels;

namespace CoverLetterGenerator.Models;

public partial class ChoiceItem : ObservableObject
{
    [ObservableProperty]
    public string _name = string.Empty;
    [ObservableProperty]
    private bool _isSelected;

    partial void OnIsSelectedChanged(bool value)
    {
        if (value && ParentViewModel != null)
        {
            ParentViewModel.SelectedTemplate = Name;
        }
    }

     public GenerateCoverLetterViewModel? ParentViewModel { get; set; }
}