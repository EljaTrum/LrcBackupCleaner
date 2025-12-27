using System.Windows;
using BackupCleaner.Services;

namespace BackupCleaner
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Laad de taalinstellingen en initialiseer localization
            var settings = SettingsService.Load();
            LocalizationService.Initialize(settings.Language);
        }
    }
}

