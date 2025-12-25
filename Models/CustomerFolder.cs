using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BackupCleaner.Models
{
    public class CustomerFolder : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        private int _backupsToKeep = 5;
        private int _totalBackups;
        private int _filesToDelete;
        private long _sizeToFree;
        private bool _isNew;

        public string FolderName { get; set; } = string.Empty;
        public string FolderPath { get; set; } = string.Empty;

        public bool IsNew
        {
            get => _isNew;
            set { _isNew = value; OnPropertyChanged(); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public int BackupsToKeep
        {
            get => _backupsToKeep;
            set { _backupsToKeep = value; OnPropertyChanged(); }
        }

        public int TotalBackups
        {
            get => _totalBackups;
            set { _totalBackups = value; OnPropertyChanged(); OnPropertyChanged(nameof(BackupsToDeleteCount)); }
        }

        public int FilesToDelete
        {
            get => _filesToDelete;
            set { _filesToDelete = value; OnPropertyChanged(); }
        }

        public long SizeToFree
        {
            get => _sizeToFree;
            set { _sizeToFree = value; OnPropertyChanged(); OnPropertyChanged(nameof(SizeToFreeFormatted)); }
        }

        public string SizeToFreeFormatted => FormatBytes(SizeToFree);

        public int BackupsToDeleteCount => TotalBackups > BackupsToKeep ? TotalBackups - BackupsToKeep : 0;

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

