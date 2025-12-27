using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BackupCleaner.Models;
using BackupCleaner.Services;

namespace BackupCleaner;

public partial class CleanupPreviewWindow : Window
{
    private readonly List<FileToDelete> _filesToDelete;

    public CleanupPreviewWindow() : this(new List<FileToDelete>()) { }

    public CleanupPreviewWindow(List<FileToDelete> filesToDelete)
    {
        InitializeComponent();
        _filesToDelete = filesToDelete;
        
        // Set window icon
        Icon = IconGenerator.CreateWindowIcon();
        
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

    private void BtnCancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void BtnConfirm_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }
}

