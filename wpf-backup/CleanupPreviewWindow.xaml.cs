using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BackupCleaner.Models;
using BackupCleaner.Services;

namespace BackupCleaner
{
    public partial class CleanupPreviewWindow : Window
    {
        private readonly List<FileToDelete> _filesToDelete;

        public CleanupPreviewWindow(List<FileToDelete> filesToDelete)
        {
            InitializeComponent();
            _filesToDelete = filesToDelete;
            
            // Stel het venster icoon in (zelfde als hoofdvenster)
            var appIcon = IconGenerator.CreateAppIcon();
            Icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                appIcon.Handle,
                System.Windows.Int32Rect.Empty,
                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            
            lstFiles.ItemsSource = _filesToDelete.OrderBy(f => f.BackupDate);
            
            var totalSize = _filesToDelete.Sum(f => f.Size);
            
            // Pas lokalisatie toe
            Title = LocalizationService.GetString("PreviewTitle");
            txtSummary.Text = LocalizationService.GetString("PreviewSummary", _filesToDelete.Count, FormatBytes(totalSize));
            txtWarning.Text = LocalizationService.GetString("CannotBeUndone");
            btnCancel.Content = LocalizationService.GetString("Cancel");
            btnConfirm.Content = LocalizationService.GetString("DeletePermanently");
        }

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

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            // Direct bevestigen - het preview venster zelf is al de controle
            DialogResult = true;
            Close();
        }
    }
}
