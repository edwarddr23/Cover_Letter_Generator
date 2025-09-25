public interface ISettingsService
{
    void SaveSettings(AppSettings settings);
    AppSettings LoadSettings();
}