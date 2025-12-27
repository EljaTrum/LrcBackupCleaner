using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using BackupCleaner.Models;
using BackupCleaner.Services;

namespace BackupCleaner;

/// <summary>
/// ViewModel wrapper voor LightroomBackup met UI-specifieke properties
/// </summary>
public class LightroomBackupViewModel : INotifyPropertyChanged
{
    public LightroomBackup Backup { get; }

    public LightroomBackupViewModel(LightroomBackup backup)
    {
        Backup = backup;
        backup.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(LightroomBackup.IsSelected))
            {
                NotifyStatusChanged();
            }
        };
    }

    public string BackupDateFormatted => Backup.BackupDateFormatted;
    public string CatalogFileName => Backup.CatalogFileName;
    public string AgeFormatted => Backup.AgeFormatted;
    public string SizeFormatted => Backup.SizeFormatted;

    public string StatusIcon => Backup.IsSelected ? "✕" : "✓";
    
    public IBrush StatusBackground => Backup.IsSelected 
        ? new SolidColorBrush(Color.Parse("#EF4444"))  // Danger/red
        : new SolidColorBrush(Color.Parse("#22C55E")); // Success/green

    public IBrush RowBackground => Backup.IsSelected
        ? Brushes.Transparent
        : new SolidColorBrush(Color.Parse("#0D2818")); // Subtle green tint

    public string StatusTooltip => Backup.IsSelected
        ? LocalizationService.GetString("WillBeDeleted")
        : LocalizationService.GetString("WillBeKept");

    public void NotifyStatusChanged()
    {
        OnPropertyChanged(nameof(StatusIcon));
        OnPropertyChanged(nameof(StatusBackground));
        OnPropertyChanged(nameof(RowBackground));
        OnPropertyChanged(nameof(StatusTooltip));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

