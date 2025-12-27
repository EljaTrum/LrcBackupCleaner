using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BackupCleaner.Services;

namespace BackupCleaner;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Laad de taalinstellingen en initialiseer localization
        var settings = SettingsService.Load();
        LocalizationService.Initialize(settings.Language);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Check voor command line argument voor startup cleanup
            var args = desktop.Args ?? [];
            var isAutoCleanupMode = args.Contains("--auto-cleanup");

            desktop.MainWindow = new MainWindow(isAutoCleanupMode);
        }

        base.OnFrameworkInitializationCompleted();
    }
}

