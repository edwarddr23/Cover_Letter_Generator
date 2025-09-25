using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Logging;

namespace CoverLetterGenerator.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        Logger.TryGet(LogEventLevel.Fatal, LogArea.Control)?.Log(this, "Avalonia Infrastructure");
        InitializeComponent();
    }
}