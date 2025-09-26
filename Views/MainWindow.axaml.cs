using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Logging;
using CoverLetterGenerator.Services;
using CoverLetterGenerator.ViewModels;

namespace CoverLetterGenerator.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        Logger.TryGet(LogEventLevel.Fatal, LogArea.Control)?.Log(this, "Avalonia Infrastructure");
        InitializeComponent();
        var settingsService = new SettingsService();
        var dialogService = new DialogService(this);
        this.DataContext = new MainWindowViewModel(settingsService, dialogService);
    }
}