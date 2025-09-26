using CoverLetterGenerator.Models;
using System;

namespace CoverLetterGenerator.Services;

public interface ISettingsService
{
    AppSettings LoadSettings();
    void SaveSettings(AppSettings settings);
    event EventHandler? SettingsChanged;
}