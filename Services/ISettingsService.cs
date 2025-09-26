using CoverLetterGenerator.Models;

namespace CoverLetterGenerator.Services;

public interface ISettingsService
{
    AppSettings LoadSettings();
    void SaveSettings(AppSettings settings);
}