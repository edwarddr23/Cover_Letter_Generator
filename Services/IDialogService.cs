using System.Threading.Tasks;

namespace CoverLetterGenerator.Services;

public interface IDialogService
{
    Task<bool> ShowConfirmationDialogAsync(string title, string message);
}