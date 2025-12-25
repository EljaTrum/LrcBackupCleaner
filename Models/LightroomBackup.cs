using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BackupCleaner.Models
{
    /// <summary>
    /// Representeert een Lightroom Classic catalogus backup
    /// </summary>
    public class LightroomBackup : INotifyPropertyChanged
    {
        private bool _isSelected = true;

        /// <summary>
        /// Naam van de backup map (formaat: YYYY-MM-DD HHMM)
        /// </summary>
        public string FolderName { get; set; } = string.Empty;

        /// <summary>
        /// Volledig pad naar de backup map
        /// </summary>
        public string FolderPath { get; set; } = string.Empty;

        /// <summary>
        /// Datum en tijd van de backup
        /// </summary>
        public DateTime BackupDate { get; set; }

        /// <summary>
        /// Naam van het catalogus bestand (.lrcat)
        /// </summary>
        public string CatalogFileName { get; set; } = string.Empty;

        /// <summary>
        /// Volledig pad naar het catalogus bestand
        /// </summary>
        public string CatalogFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Totale grootte van de backup map in bytes
        /// </summary>
        public long TotalSize { get; set; }

        /// <summary>
        /// Aantal bestanden in de backup map
        /// </summary>
        public int FileCount { get; set; }

        /// <summary>
        /// Is deze backup geselecteerd voor verwijdering
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Leeftijd van de backup in dagen
        /// </summary>
        public int AgeInDays => (DateTime.Today - BackupDate.Date).Days;

        /// <summary>
        /// Geformatteerde datum voor weergave
        /// </summary>
        public string BackupDateFormatted => BackupDate.ToString("dd-MM-yyyy HH:mm");

        /// <summary>
        /// Geformatteerde relatieve leeftijd
        /// </summary>
        public string AgeFormatted
        {
            get
            {
                var days = AgeInDays;
                if (days == 0) return "Vandaag";
                if (days == 1) return "Gisteren";
                if (days < 7) return days == 1 ? "1 dag" : $"{days} dagen";
                
                var weeks = days / 7;
                if (days < 30) return weeks == 1 ? "1 week" : $"{weeks} weken";
                
                var months = days / 30;
                if (days < 365) return months == 1 ? "1 maand" : $"{months} maanden";
                
                var years = days / 365;
                return years == 1 ? "1 jaar" : $"{years} jaar";
            }
        }

        /// <summary>
        /// Geformatteerde grootte voor weergave
        /// </summary>
        public string SizeFormatted => FormatBytes(TotalSize);

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

