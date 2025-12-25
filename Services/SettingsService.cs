using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace BackupCleaner.Services
{
    public class AppSettings
    {
        /// <summary>
        /// Pad naar de Lightroom backup map
        /// </summary>
        public string? BackupFolderPath { get; set; }
        
        /// <summary>
        /// Naam van de catalogus (voor weergave)
        /// </summary>
        public string? CatalogName { get; set; }
        
        /// <summary>
        /// Is automatisch opruimen ingeschakeld
        /// </summary>
        public bool AutoCleanupEnabled { get; set; }
        
        /// <summary>
        /// Tijdstip voor automatische opruiming (uur van de dag, 0-23)
        /// </summary>
        public int AutoCleanupHour { get; set; } = 2;
        
        /// <summary>
        /// Laatste keer dat automatische opruiming is uitgevoerd
        /// </summary>
        public DateTime? LastAutoCleanup { get; set; }
        
        /// <summary>
        /// Aantal backups om te bewaren
        /// </summary>
        public int BackupsToKeep { get; set; } = 5;
        
        /// <summary>
        /// Minimum leeftijd in maanden voordat een backup verwijderd mag worden
        /// </summary>
        public int MinimumAgeMonths { get; set; } = 1;
    }

    public static class SettingsService
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LightroomBackupCleaner",
            "settings.json"
        );

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                    if (settings != null)
                    {
                        // Valideer instellingen
                        if (settings.BackupsToKeep < 1) settings.BackupsToKeep = 5;
                        if (settings.MinimumAgeMonths < 0) settings.MinimumAgeMonths = 1;
                        if (settings.AutoCleanupHour < 0 || settings.AutoCleanupHour > 23) 
                            settings.AutoCleanupHour = 2;
                        return settings;
                    }
                }
            }
            catch
            {
                // Bij fout, return default settings
            }
            return new AppSettings();
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
                // Silently fail on save errors
            }
        }

        /// <summary>
        /// Krijg het pad naar de settings map
        /// </summary>
        public static string GetSettingsDirectory()
        {
            return Path.GetDirectoryName(SettingsPath) ?? string.Empty;
        }
    }
}
