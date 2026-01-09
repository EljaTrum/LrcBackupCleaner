using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BackupCleaner.Models;
using BackupCleaner.Services;

namespace BackupCleaner;

public partial class MainWindow : Window
{
    private AppSettings _settings = new();
    private ObservableCollection<LightroomBackupViewModel> _backups = new();
    private DispatcherTimer? _autoCleanupTimer;
    private bool _isScanning = false;
    private OldCatalogsInfo? _oldCatalogsInfo;
    private bool _isAutoCleanupMode;

    public MainWindow() : this(false) { }

    public MainWindow(bool isAutoCleanupMode)
    {
        InitializeComponent();
        _isAutoCleanupMode = isAutoCleanupMode;
        
        // Set window icon
        Icon = IconGenerator.CreateWindowIcon();
        
        // Load app logo
        var logoBitmap = IconGenerator.CreateBitmap(48);
        if (logoBitmap != null)
        {
            appLogo.Source = logoBitmap;
        }
        
        lstBackups.ItemsSource = _backups;
        
        LoadSettings();
        ApplyLocalization();
        SetupAutoCleanupTimer();
        
        if (_isAutoCleanupMode)
        {
            LogStartupCleanup("App gestart in auto-cleanup mode");
            WindowState = WindowState.Minimized;
            ShowInTaskbar = false;
        }
        
        // Loaded event voor initialisatie
        Loaded += async (s, e) =>
        {
            if (_isAutoCleanupMode)
            {
                LogStartupCleanup("Loaded - start cleanup");
                Hide();
                await PerformStartupCleanupAsync();
            }
            else
            {
                await InitializeBackupLocationAsync();
            }
        };
    }

    private void ApplyLocalization()
    {
        Title = LocalizationService.GetString("AppTitle");
        
        // Header buttons
        btnSettings.Content = LocalizationService.GetString("Settings");
        btnChangeFolder.Content = LocalizationService.GetString("ChangeFolder");
        
        // Toolbar buttons
        btnScan.Content = LocalizationService.GetString("Refresh");
        btnCleanup.Content = LocalizationService.GetString("Cleanup");
        
        // Toolbar labels
        txtMinAgeLabel.Text = LocalizationService.GetString("MinAge");
        txtMonthsLabel.Text = LocalizationService.GetString("Months");
        txtKeepLabel.Text = LocalizationService.GetString("Keep");
        
        // List headers
        txtHeaderBackupDate.Text = LocalizationService.GetString("BackupDate");
        txtHeaderAge.Text = LocalizationService.GetString("Age");
        txtHeaderSize.Text = LocalizationService.GetString("Size");
        
        // Empty state
        btnEmptySelectFolder.Content = LocalizationService.GetString("SelectBackupFolderButton");
        txtEmptyTitle.Text = LocalizationService.GetString("NoBackupsFound");
        txtEmptySubtitle.Text = LocalizationService.GetString("SelectBackupFolder");
        
        // Searching state
        txtSearchingMessage.Text = LocalizationService.GetString("SearchingBackups");
        
        // Status bar
        txtStatus.Text = LocalizationService.GetString("Ready");
        txtMadeByLabel.Text = LocalizationService.GetString("MadeBy") + " ";
        txtTotalLabel.Text = LocalizationService.GetString("Total") + " ";
        txtToDeleteLabel.Text = LocalizationService.GetString("ToDelete") + " ";
        txtSpaceLabel.Text = LocalizationService.GetString("SpaceToFree") + " ";
    }

    private async Task InitializeBackupLocationAsync()
    {
        if (!string.IsNullOrEmpty(_settings.BackupFolderPath) && 
            Directory.Exists(_settings.BackupFolderPath) &&
            LightroomDetectionService.IsValidBackupFolder(_settings.BackupFolderPath))
        {
            UpdateCatalogInfo();
            await ScanBackupsAsync();
            return;
        }

        txtStatus.Text = LocalizationService.GetString("SearchingBackups");
        searchingState.IsVisible = true;
        emptyState.IsVisible = false;

        // Stap 1: Snelle zoektocht op standaard locaties
        var foundLocations = await Task.Run(() => LightroomDetectionService.FindBackupLocations());

        // Stap 2: Als niets gevonden, diepe scan met progress reporting
        if (!foundLocations.Any())
        {
            txtStatus.Text = LocalizationService.GetString("DeepSearching");
            
            var cts = new System.Threading.CancellationTokenSource();
            
            foundLocations = await Task.Run(() => 
                LightroomDetectionService.DeepScanForBackupLocationsWithProgress(
                    progress => Dispatcher.UIThread.Post(() => 
                    {
                        UpdateStatus(progress, progress); // Tooltip met volledige tekst voor lange paden
                    }),
                    cts.Token
                ));
            
            // Wis de tooltip na het zoeken
            Avalonia.Controls.ToolTip.SetTip(txtStatus, null);
        }

        searchingState.IsVisible = false;

        if (foundLocations.Any())
        {
            if (foundLocations.Count == 1)
            {
                _settings.BackupFolderPath = foundLocations.First();
                _settings.CatalogName = LightroomDetectionService.GetCatalogNameFromBackupPath(_settings.BackupFolderPath);
                SaveSettings();
                UpdateCatalogInfo();
                await ScanBackupsAsync();
            }
            else
            {
                await ShowBackupLocationChoice(foundLocations);
            }
        }
        else
        {
            emptyState.IsVisible = true;
            txtEmptyTitle.Text = LocalizationService.GetString("NoBackupsFound");
            txtEmptySubtitle.Text = LocalizationService.GetString("SelectBackupFolder");
            txtStatus.Text = LocalizationService.GetString("NoBackupsFound");
        }
    }

    private async Task ShowBackupLocationChoice(List<string> locations)
    {
        var locationsList = "";
        for (int i = 0; i < locations.Count; i++)
        {
            var catalogName = LightroomDetectionService.GetCatalogNameFromBackupPath(locations[i]);
            locationsList += $"{i + 1}. {catalogName}\n   {locations[i]}\n\n";
        }
        
        var message = LocalizationService.GetString("MultipleLocationsMessage", locationsList);
        var title = LocalizationService.GetString("MultipleLocationsFound");

        var dialog = new Window
        {
            Title = title,
            Width = 500,
            Height = 300,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Avalonia.Media.Brushes.Transparent
        };
        
        // Use first location for now (dialog implementation can be enhanced later)
        _settings.BackupFolderPath = locations.First();
        _settings.CatalogName = LightroomDetectionService.GetCatalogNameFromBackupPath(_settings.BackupFolderPath);
        SaveSettings();
        UpdateCatalogInfo();
        await ScanBackupsAsync();
    }

    private void UpdateCatalogInfo()
    {
        if (!string.IsNullOrEmpty(_settings.BackupFolderPath))
        {
            var catalogName = _settings.CatalogName ?? 
                LightroomDetectionService.GetCatalogNameFromBackupPath(_settings.BackupFolderPath);
            var displayText = $"{catalogName} • {_settings.BackupFolderPath}";
            txtCatalogInfo.Text = displayText;
            
            // Tooltip met volledige tekst voor lange paden
            Avalonia.Controls.ToolTip.SetTip(txtCatalogInfo, 
                $"{catalogName}\n{_settings.BackupFolderPath}\n\n{LocalizationService.GetString("OpenFolderTooltip")}");
        }
        else
        {
            txtCatalogInfo.Text = LocalizationService.GetString("NoCatalogSelected");
            Avalonia.Controls.ToolTip.SetTip(txtCatalogInfo, null);
        }
    }

    private async Task CheckForOldCatalogsAsync()
    {
        if (string.IsNullOrEmpty(_settings.BackupFolderPath))
        {
            oldCatalogsAlert.IsVisible = false;
            return;
        }

        _oldCatalogsInfo = await Task.Run(() => 
            LightroomDetectionService.FindOldLightroomCatalogs(_settings.BackupFolderPath!));

        if (_oldCatalogsInfo != null && _oldCatalogsInfo.IsOlderThanOneMonth)
        {
            UpdateOldCatalogsUI();
            oldCatalogsAlert.IsVisible = true;
        }
        else
        {
            oldCatalogsAlert.IsVisible = false;
        }
    }

    private void UpdateOldCatalogsUI()
    {
        if (_oldCatalogsInfo == null) return;

        txtOldCatalogsTitle.Text = LocalizationService.GetString("OldCatalogsTitle");
        
        var infoText = LocalizationService.GetString("OldCatalogsInfo", 
            _oldCatalogsInfo.FileCount, 
            _oldCatalogsInfo.SizeFormatted,
            _oldCatalogsInfo.AgeFormatted);
        txtOldCatalogsInfo.Text = infoText;
        
        txtOldCatalogsPath.Text = _oldCatalogsInfo.FolderPath;
        
        btnDeleteOldCatalogs.Content = LocalizationService.GetString("DeleteOldCatalogs");
    }

    private void OldCatalogsPath_Click(object? sender, PointerPressedEventArgs e)
    {
        if (_oldCatalogsInfo != null && Directory.Exists(_oldCatalogsInfo.FolderPath))
        {
            OpenFolder(_oldCatalogsInfo.FolderPath);
        }
    }

    private async void BtnDeleteOldCatalogs_Click(object? sender, RoutedEventArgs e)
    {
        if (_oldCatalogsInfo == null || !Directory.Exists(_oldCatalogsInfo.FolderPath))
            return;

        // Simple confirmation via dialog (can be enhanced)
        try
        {
            btnDeleteOldCatalogs.IsEnabled = false;
            txtStatus.Text = LocalizationService.GetString("DeletingOldCatalogs");

            var folderPath = _oldCatalogsInfo.FolderPath;
            var freedSpace = _oldCatalogsInfo.TotalSize;

            await Task.Run(() =>
            {
                Directory.Delete(folderPath, recursive: true);
            });

            oldCatalogsAlert.IsVisible = false;
            _oldCatalogsInfo = null;
            txtStatus.Text = LocalizationService.GetString("Ready");
        }
        catch (Exception ex)
        {
            txtStatus.Text = $"Fout: {ex.Message}";
        }
        finally
        {
            btnDeleteOldCatalogs.IsEnabled = true;
        }
    }

    private void SetupAutoCleanupTimer()
    {
        _autoCleanupTimer = new DispatcherTimer();
        _autoCleanupTimer.Interval = TimeSpan.FromMinutes(1);
        _autoCleanupTimer.Tick += AutoCleanupTimer_Tick;
        
        if (_settings.AutoCleanupEnabled)
        {
            _autoCleanupTimer.Start();
        }
    }

    private async void AutoCleanupTimer_Tick(object? sender, EventArgs e)
    {
        if (!_settings.AutoCleanupEnabled) return;
        if (_settings.LastAutoCleanup?.Date == DateTime.Today) return;
        
        var now = DateTime.Now;
        if (now.Hour != _settings.AutoCleanupHour) return;
        
        await PerformAutomaticCleanupAsync();
    }

    private async Task PerformStartupCleanupAsync()
    {
        LogStartupCleanup("PerformStartupCleanupAsync gestart");
        
        await Task.Delay(500);
        
        if (string.IsNullOrEmpty(_settings.BackupFolderPath) || 
            !Directory.Exists(_settings.BackupFolderPath))
        {
            LogStartupCleanup("Geen geldige backup map gevonden, afsluiten");
            ExitApplication();
            return;
        }

        try
        {
            LogStartupCleanup($"Start cleanup voor: {_settings.BackupFolderPath}");
            
            var allBackups = await Task.Run(() => 
                BackupService.GetLightroomBackups(_settings.BackupFolderPath!));
            
            LogStartupCleanup($"Gevonden backups: {allBackups.Count}");
            
            var backupsToDelete = BackupService.GetBackupsToDelete(
                allBackups, 
                _settings.BackupsToKeep, 
                _settings.MinimumAgeMonths);

            LogStartupCleanup($"Te verwijderen: {backupsToDelete.Count}");

            if (backupsToDelete.Any())
            {
                var (deleted, freed, errors) = await Task.Run(() => BackupService.DeleteBackups(backupsToDelete));
                LogStartupCleanup($"Verwijderd: {deleted} backups, {freed} bytes vrijgemaakt");
            }
            else
            {
                LogStartupCleanup("Geen backups om te verwijderen");
            }

            _settings.LastAutoCleanup = DateTime.Now;
            SaveSettings();
        }
        catch (Exception ex)
        {
            LogStartupCleanup($"Fout: {ex.Message}");
        }
        finally
        {
            LogStartupCleanup("Cleanup voltooid, afsluiten");
            ExitApplication();
        }
    }

    private static void LogStartupCleanup(string message)
    {
        try
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LightroomBackupCleaner",
                "startup-cleanup.log");
            
            var directory = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var logLine = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n";
            File.AppendAllText(logPath, logLine);
        }
        catch
        {
            // Ignore logging errors
        }
    }

    private async Task PerformAutomaticCleanupAsync()
    {
        if (string.IsNullOrEmpty(_settings.BackupFolderPath) || 
            !Directory.Exists(_settings.BackupFolderPath))
            return;

        try
        {
            txtStatus.Text = LocalizationService.GetString("AutoCleanupStarted");
            
            var allBackups = await Task.Run(() => 
                BackupService.GetLightroomBackups(_settings.BackupFolderPath!));
            
            var backupsToDelete = BackupService.GetBackupsToDelete(
                allBackups, 
                _settings.BackupsToKeep, 
                _settings.MinimumAgeMonths);

            if (backupsToDelete.Any())
            {
                var (deleted, freed, errors) = await Task.Run(() => 
                    BackupService.DeleteBackups(backupsToDelete));
                
                txtStatus.Text = LocalizationService.GetString("AutoCleanupComplete", deleted, FormatBytes(freed));
            }
            else
            {
                txtStatus.Text = LocalizationService.GetString("NoBackupsToClean");
            }

            _settings.LastAutoCleanup = DateTime.Now;
            SaveSettings();
            
            await ScanBackupsAsync();
        }
        catch (Exception ex)
        {
            txtStatus.Text = $"Automatische opruiming mislukt: {ex.Message}";
        }
    }

    private void BtnSelectFolder_Click(object? sender, RoutedEventArgs e)
    {
        _ = SelectFolderAsync();
    }

    private async Task SelectFolderAsync()
    {
        var folder = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = LocalizationService.GetString("SelectFolderDescription"),
            AllowMultiple = false
        });

        if (folder.Count > 0)
        {
            var selectedPath = folder[0].Path.LocalPath;
            
            if (!LightroomDetectionService.IsValidBackupFolder(selectedPath))
            {
                // Show warning but continue
            }

            _settings.BackupFolderPath = selectedPath;
            _settings.CatalogName = LightroomDetectionService.GetCatalogNameFromBackupPath(selectedPath);
            SaveSettings();
            
            UpdateCatalogInfo();
            _backups.Clear();
            await ScanBackupsAsync();
        }
    }

    private async void BtnSettings_Click(object? sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(_settings);
        
        settingsWindow.AutoCleanupChanged += (s, args) =>
        {
            if (_autoCleanupTimer != null)
            {
                if (_settings.AutoCleanupEnabled)
                    _autoCleanupTimer.Start();
                else
                    _autoCleanupTimer.Stop();
            }
            UpdateScheduledTask();
            SaveSettings();
        };
        
        settingsWindow.RunAtStartupChanged += (s, args) =>
        {
            UpdateStartupRegistration();
            SaveSettings();
        };
        
        settingsWindow.LanguageChanged += (s, newLanguage) =>
        {
            SaveSettings();
        };
        
        settingsWindow.ForgetFolderRequested += async (s, args) =>
        {
            // Reset de UI
            _backups.Clear();
            txtCatalogInfo.Text = LocalizationService.GetString("NoCatalogSelected");
            oldCatalogsAlert.IsVisible = false;
            
            // Reset statistieken
            txtTotalBackups.Text = "0 backup(s)";
            txtToDelete.Text = "0";
            txtSpaceToFree.Text = "0 B";
            
            // Herlaad settings (folder is al null gezet door SettingsWindow)
            LoadSettings();
            
            // Start de detectie opnieuw
            await InitializeBackupLocationAsync();
        };
        
        await settingsWindow.ShowDialog(this);
    }

    private void BtnScan_Click(object? sender, RoutedEventArgs e)
    {
        if (!_isScanning)
        {
            _ = ScanBackupsAsync();
        }
    }

    private async Task ScanBackupsAsync()
    {
        if (string.IsNullOrEmpty(_settings.BackupFolderPath) || 
            !Directory.Exists(_settings.BackupFolderPath))
        {
            emptyState.IsVisible = true;
            txtStatus.Text = LocalizationService.GetString("SelectBackupFolder");
            return;
        }

        _isScanning = true;
        btnScan.IsEnabled = false;
        btnCleanup.IsEnabled = false;
        emptyState.IsVisible = false;
        searchingState.IsVisible = false;
        
        txtStatus.Text = LocalizationService.GetString("ScanningBackups");
        _backups.Clear();

        try
        {
            var allBackups = await Task.Run(() => 
                BackupService.GetLightroomBackups(_settings.BackupFolderPath!));
            
            if (!allBackups.Any())
            {
                emptyState.IsVisible = true;
                txtEmptyTitle.Text = LocalizationService.GetString("NoBackupsFound");
                txtEmptySubtitle.Text = LocalizationService.GetString("NoBackupsInFolder");
                txtStatus.Text = LocalizationService.GetString("NoBackupsInFolder");
                UpdateStats();
                return;
            }

            var backupsToDelete = BackupService.GetBackupsToDelete(
                allBackups, 
                _settings.BackupsToKeep, 
                _settings.MinimumAgeMonths);
            
            var backupsToDeletePaths = new HashSet<string>(backupsToDelete.Select(b => b.FolderPath));

            foreach (var backup in allBackups)
            {
                backup.IsSelected = backupsToDeletePaths.Contains(backup.FolderPath);
                _backups.Add(new LightroomBackupViewModel(backup));
            }

            UpdateStats();
            txtStatus.Text = LocalizationService.GetString("ScanComplete", allBackups.Count);
            
            await CheckForOldCatalogsAsync();
        }
        catch (Exception ex)
        {
            txtStatus.Text = $"Fout bij scannen: {ex.Message}";
            emptyState.IsVisible = true;
            txtEmptyTitle.Text = "Fout bij scannen";
            txtEmptySubtitle.Text = ex.Message;
        }
        finally
        {
            _isScanning = false;
            btnScan.IsEnabled = true;
            btnCleanup.IsEnabled = true;
        }
    }

    private void UpdateStats()
    {
        var total = _backups.Count;
        var toDelete = _backups.Where(b => b.Backup.IsSelected).ToList();
        var spaceToFree = toDelete.Sum(b => b.Backup.TotalSize);

        txtTotalBackups.Text = $"{total} backup(s)";
        txtToDelete.Text = toDelete.Count.ToString();
        txtSpaceToFree.Text = FormatBytes(spaceToFree);
    }

    private async void BtnCleanup_Click(object? sender, RoutedEventArgs e)
    {
        var backupsToDelete = _backups.Where(b => b.Backup.IsSelected).Select(b => b.Backup).ToList();
        
        if (!backupsToDelete.Any())
        {
            return;
        }

        var catalogName = _settings.CatalogName ?? "Lightroom";
        var filesToDelete = BackupService.ConvertToFilesToDelete(backupsToDelete, catalogName);
        
        var previewWindow = new CleanupPreviewWindow(filesToDelete);
        var result = await previewWindow.ShowDialog<bool>(this);
        
        if (result)
        {
            btnCleanup.IsEnabled = false;
            txtStatus.Text = LocalizationService.GetString("DeletingBackups");
            
            var (deleted, freed, errors) = await Task.Run(() => BackupService.DeleteBackups(backupsToDelete));
            
            if (errors.Any())
            {
                // Er waren fouten bij het verwijderen
                var errorMessage = LocalizationService.GetString("CleanupCompleteWithErrorsMessage", 
                    deleted, FormatBytes(freed), errors.Count);
                
                // Voeg de eerste paar foutmeldingen toe (max 5 voor leesbaarheid)
                var errorDetails = string.Join("\n", errors.Take(5));
                if (errors.Count > 5)
                {
                    errorDetails += $"\n... (+{errors.Count - 5} {LocalizationService.GetString("MoreErrors")})";
                }
                
                var fullMessage = errorMessage + "\n\n" + errorDetails;
                
                // Log alle errors naar het log bestand
                LogCleanupErrors(errors);
                
                // Toon foutmelding aan gebruiker
                await ShowErrorDialogAsync(
                    LocalizationService.GetString("CleanupCompleteWithErrorsTitle"), 
                    fullMessage);
                
                txtStatus.Text = LocalizationService.GetString("CleanupCompletedWithErrors", deleted, errors.Count);
            }
            else if (deleted == 0 && backupsToDelete.Any())
            {
                // Geen enkele backup verwijderd terwijl er wel geselecteerd waren
                var message = LocalizationService.GetString("NoBackupsDeleted");
                await ShowErrorDialogAsync(LocalizationService.GetString("Error"), message);
                txtStatus.Text = LocalizationService.GetString("CleanupFailed");
            }
            else
            {
                txtStatus.Text = LocalizationService.GetString("CleanupCompleteMessage", deleted, FormatBytes(freed));
            }
            
            btnCleanup.IsEnabled = true;
            await ScanBackupsAsync();
        }
    }

    private static void LogCleanupErrors(List<string> errors)
    {
        try
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LightroomBackupCleaner",
                "cleanup-errors.log");
            
            var directory = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var logLines = $"\n--- {DateTime.Now:yyyy-MM-dd HH:mm:ss} ---\n" + 
                           string.Join("\n", errors) + "\n";
            File.AppendAllText(logPath, logLines);
        }
        catch
        {
            // Ignore logging errors
        }
    }

    private async Task ShowErrorDialogAsync(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 500,
            Height = 300,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Icon = IconGenerator.CreateWindowIcon()
        };
        
        var panel = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 15
        };
        
        var textBlock = new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            FontSize = 13
        };
        
        var button = new Button
        {
            Content = LocalizationService.GetString("Close"),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Padding = new Thickness(20, 8)
        };
        button.Click += (s, e) => dialog.Close();
        
        panel.Children.Add(textBlock);
        panel.Children.Add(button);
        
        dialog.Content = panel;
        
        await dialog.ShowDialog(this);
    }

    private void BtnDecreaseMinAge_Click(object? sender, RoutedEventArgs e)
    {
        if (_settings.MinimumAgeMonths > 0)
        {
            _settings.MinimumAgeMonths--;
            txtMinAge.Text = _settings.MinimumAgeMonths.ToString();
            SaveSettings();
            RecalculateBackupsToDelete();
        }
    }

    private void BtnIncreaseMinAge_Click(object? sender, RoutedEventArgs e)
    {
        if (_settings.MinimumAgeMonths < 24)
        {
            _settings.MinimumAgeMonths++;
            txtMinAge.Text = _settings.MinimumAgeMonths.ToString();
            SaveSettings();
            RecalculateBackupsToDelete();
        }
    }

    private void BtnDecreaseKeep_Click(object? sender, RoutedEventArgs e)
    {
        if (_settings.BackupsToKeep > 1)
        {
            _settings.BackupsToKeep--;
            txtBackupsToKeep.Text = _settings.BackupsToKeep.ToString();
            SaveSettings();
            RecalculateBackupsToDelete();
        }
    }

    private void BtnIncreaseKeep_Click(object? sender, RoutedEventArgs e)
    {
        if (_settings.BackupsToKeep < 50)
        {
            _settings.BackupsToKeep++;
            txtBackupsToKeep.Text = _settings.BackupsToKeep.ToString();
            SaveSettings();
            RecalculateBackupsToDelete();
        }
    }

    private void RecalculateBackupsToDelete()
    {
        if (_backups.Count == 0) return;

        var allBackups = _backups.Select(vm => vm.Backup).ToList();
        var backupsToDelete = BackupService.GetBackupsToDelete(
            allBackups, 
            _settings.BackupsToKeep, 
            _settings.MinimumAgeMonths);
        
        var backupsToDeletePaths = new HashSet<string>(backupsToDelete.Select(b => b.FolderPath));

        foreach (var vm in _backups)
        {
            vm.Backup.IsSelected = backupsToDeletePaths.Contains(vm.Backup.FolderPath);
            vm.NotifyStatusChanged();
        }

        UpdateStats();
    }

    private void LoadSettings()
    {
        _settings = SettingsService.Load();
        txtBackupsToKeep.Text = _settings.BackupsToKeep.ToString();
        txtMinAge.Text = _settings.MinimumAgeMonths.ToString();
    }

    private void SaveSettings()
    {
        SettingsService.Save(_settings);
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

    /// <summary>
    /// Update de statusbalk tekst en tooltip
    /// </summary>
    private void UpdateStatus(string text, string? tooltip = null)
    {
        txtStatus.Text = text;
        Avalonia.Controls.ToolTip.SetTip(txtStatus, tooltip);
    }

    private void PhotofactsLink_Click(object? sender, PointerPressedEventArgs e)
    {
        OpenUrl("https://photofactsacademy.nl");
    }

    private void CatalogInfo_Click(object? sender, PointerPressedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_settings.BackupFolderPath) && Directory.Exists(_settings.BackupFolderPath))
        {
            OpenFolder(_settings.BackupFolderPath);
        }
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore errors
        }
    }

    private void OpenFolder(string path)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start("explorer.exe", path);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", path);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", path);
            }
        }
        catch
        {
            // Ignore errors
        }
    }

    private void ExitApplication()
    {
        if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private void UpdateScheduledTask()
    {
        // Platform-specific implementation
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Services.PlatformServices.Windows.UpdateScheduledTask(_settings);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Services.PlatformServices.MacOS.UpdateScheduledTask(_settings);
        }
    }

    private void UpdateStartupRegistration()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Services.PlatformServices.Windows.UpdateStartupRegistry(_settings);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Services.PlatformServices.MacOS.UpdateLoginItem(_settings);
        }
    }
}

