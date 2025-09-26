using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace CoverLetterGenerator.Services;

public class DialogService : IDialogService
{
    private readonly Window? _parentWindow;

    // parentWindow is optional; you can pass the MainWindow when constructing the service,
    // or let the service resolve the MainWindow at runtime.
    public DialogService(Window? parentWindow = null)
    {
        _parentWindow = parentWindow;
    }

    private Window? GetOwnerWindow()
    {
        // Prefer the injected parent window if it's visible
        if (_parentWindow is { IsVisible: true })
            return _parentWindow;

        // Otherwise try to get the application's main window
        return (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
    }

    public async Task<bool> ShowConfirmationDialogAsync(string title, string message)
    {
        // Build dialog
        var messageText = new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        };

        var yesButton = new Button { Content = "Yes", IsDefault = true };
        var noButton = new Button { Content = "No", IsCancel = true };

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 10,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Children = { yesButton, noButton }
        };

        var contentPanel = new StackPanel
        {
            Spacing = 10,
            Margin = new Thickness(20),
            Children = { messageText, buttonPanel }
        };

        var dialog = new Window
        {
            Title = title,
            Width = 420,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = contentPanel
        };

        var tcs = new TaskCompletionSource<bool>();

        yesButton.Click += (_, _) =>
        {
            tcs.TrySetResult(true);
            dialog.Close();
        };

        noButton.Click += (_, _) =>
        {
            tcs.TrySetResult(false);
            dialog.Close();
        };

        dialog.Closed += (_, _) =>
        {
            // If closed by some other means, ensure we return false (or whatever default).
            if (!tcs.Task.IsCompleted)
                tcs.TrySetResult(false);
        };

        var owner = GetOwnerWindow();
        if (owner != null && owner.IsVisible)
        {
            // modal
            await dialog.ShowDialog(owner);
        }
        else
        {
            // fallback to non-modal if no visible owner (prevents crash)
            dialog.Show();
        }

        // wait for user choice (or dialog close)
        return await tcs.Task;
    }
}
