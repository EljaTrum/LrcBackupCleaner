using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using BackupCleaner.Models;
using BackupCleaner.Services;
using Microsoft.Win32;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;

namespace BackupCleaner
{
    public partial class MainWindow : Window
    {
        private AppSettings _settings = new();
        private ObservableCollection<LightroomBackup> _backups = new();
        private System.Windows.Threading.DispatcherTimer? _autoCleanupTimer;
        private bool _isScanning = false;
        private NotifyIcon? _notifyIcon;
        
        private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string StartupValueName = "LightroomClassicBackupCleaner";

        public MainWindow()
        {
            InitializeComponent();
            
            lstBackups.ItemsSource = _backups;
            
            LoadSettings();
            ApplyLocalization();
            SetupAutoCleanupTimer();
            SetupSystemTray();
            
            // Check voor command line argument voor startup cleanup
            var args = Environment.GetCommandLineArgs();
            _isAutoCleanupMode = args.Contains("--auto-cleanup");
            
            if (_isAutoCleanupMode)
            {
                LogStartupCleanup("App gestart in auto-cleanup mode");
                // Start verborgen voor auto-cleanup
                WindowState = WindowState.Minimized;
                ShowInTaskbar = false;
            }
            
            // ContentRendered event werkt betrouwbaarder dan Loaded
            ContentRendered += async (s, e) =>
            {
                if (_isAutoCleanupMode)
                {
                    LogStartupCleanup("ContentRendered - start cleanup");
                    // Verberg direct na renderen
                    Hide();
                    await PerformStartupCleanupAsync();
                }
                else
                {
                    await InitializeBackupLocationAsync();
                }
            };
        }
        
        private bool _isAutoCleanupMode = false;
        
        private void ApplyLocalization()
        {
            // Window title blijft Engels (productnaam)
            Title = LocalizationService.GetString("AppTitle");
            
            // Header buttons
            btnSettings.Content = LocalizationService.GetString("Settings");
            btnChangeFolder.Content = LocalizationService.GetString("ChangeFolder");
            
            // Toolbar buttons
            btnScan.Content = LocalizationService.GetString("Refresh");
            btnCleanup.Content = LocalizationService.GetString("Cleanup");
            
            // Toolbar labels
            txtMinAgeLabel.Text = LocalizationService.GetString("MinAge");
            txtMinAgeLabel.ToolTip = LocalizationService.GetString("MinAgeTooltip");
            txtMonthsLabel.Text = LocalizationService.GetString("Months");
            txtKeepLabel.Text = LocalizationService.GetString("Keep");
            txtKeepLabel.ToolTip = LocalizationService.GetString("KeepTooltip");
            
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
            
            // Catalog info tooltip
            txtCatalogInfo.ToolTip = LocalizationService.GetString("OpenFolderTooltip");
        }

        private async Task InitializeBackupLocationAsync()
        {
            // Als er een geldige backup map is, scan deze
            if (!string.IsNullOrEmpty(_settings.BackupFolderPath) && 
                Directory.Exists(_settings.BackupFolderPath) &&
                LightroomDetectionService.IsValidBackupFolder(_settings.BackupFolderPath))
            {
                UpdateCatalogInfo();
                await ScanBackupsAsync();
                return;
            }

            // Probeer automatisch te detecteren
            txtStatus.Text = LocalizationService.GetString("SearchingBackups");
            searchingState.Visibility = Visibility.Visible;
            emptyState.Visibility = Visibility.Collapsed;

            var foundLocations = await Task.Run(() => LightroomDetectionService.FindBackupLocations());

            searchingState.Visibility = Visibility.Collapsed;

            if (foundLocations.Any())
            {
                if (foundLocations.Count == 1)
                {
                    // Eén locatie gevonden - gebruik deze
                    _settings.BackupFolderPath = foundLocations.First();
                    _settings.CatalogName = LightroomDetectionService.GetCatalogNameFromBackupPath(_settings.BackupFolderPath);
                    SaveSettings();
                    UpdateCatalogInfo();
                    await ScanBackupsAsync();
                }
                else
                {
                    // Meerdere locaties gevonden - laat gebruiker kiezen
                    ShowBackupLocationChoice(foundLocations);
                }
            }
            else
            {
                // Geen locaties gevonden - toon empty state
                emptyState.Visibility = Visibility.Visible;
                txtEmptyTitle.Text = LocalizationService.GetString("NoBackupsFound");
                txtEmptySubtitle.Text = LocalizationService.GetString("SelectBackupFolder");
                txtStatus.Text = LocalizationService.GetString("NoBackupsFound");
            }
        }

        private void ShowBackupLocationChoice(List<string> locations)
        {
            var locationsList = "";
            for (int i = 0; i < locations.Count; i++)
            {
                var catalogName = LightroomDetectionService.GetCatalogNameFromBackupPath(locations[i]);
                locationsList += $"{i + 1}. {catalogName}\n   {locations[i]}\n\n";
            }
            
            var message = LocalizationService.GetString("MultipleLocationsMessage", locationsList);
            var title = LocalizationService.GetString("MultipleLocationsFound");

            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _settings.BackupFolderPath = locations.First();
                _settings.CatalogName = LightroomDetectionService.GetCatalogNameFromBackupPath(_settings.BackupFolderPath);
                SaveSettings();
                UpdateCatalogInfo();
                _ = ScanBackupsAsync();
            }
            else
            {
                SelectFolder();
            }
        }

        private void UpdateCatalogInfo()
        {
            if (!string.IsNullOrEmpty(_settings.BackupFolderPath))
            {
                var catalogName = _settings.CatalogName ?? 
                    LightroomDetectionService.GetCatalogNameFromBackupPath(_settings.BackupFolderPath);
                txtCatalogInfo.Text = $"📍 {catalogName} • {_settings.BackupFolderPath}";
            }
            else
            {
                txtCatalogInfo.Text = LocalizationService.GetString("NoCatalogSelected");
            }
        }

        private void SetupSystemTray()
        {
            var appIcon = IconGenerator.CreateAppIcon();
            
            _notifyIcon = new NotifyIcon
            {
                Icon = appIcon,
                Visible = false,
                Text = "Lightroom Classic Backup Cleaner"
            };
            
            Icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                appIcon.Handle,
                System.Windows.Int32Rect.Empty,
                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            
            var contextMenu = new ContextMenuStrip();
            
            // Tray menu items - lokaliseren
            var openText = LocalizationService.IsDutch ? "Openen" : "Open";
            var cleanupText = LocalizationService.IsDutch ? "Opruimen starten" : "Start Cleanup";
            var exitText = LocalizationService.IsDutch ? "Afsluiten" : "Exit";
            
            contextMenu.Items.Add(openText, null, (s, e) => ShowWindow());
            contextMenu.Items.Add(cleanupText, null, async (s, e) => await PerformAutomaticCleanupAsync());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add(exitText, null, (s, e) => ExitApplication());
            
            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, e) => ShowWindow();
        }

        private void ShowWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            _notifyIcon!.Visible = false;
        }

        private void ExitApplication()
        {
            _notifyIcon?.Dispose();
            Application.Current.Shutdown();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            
            if (WindowState == WindowState.Minimized && _settings.AutoCleanupEnabled)
            {
                Hide();
                _notifyIcon!.Visible = true;
                _notifyIcon.ShowBalloonTip(2000, "Lightroom Classic Backup Cleaner", 
                    LocalizationService.GetString("BackgroundRunning"), ToolTipIcon.Info);
            }
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

        private void SetupAutoCleanupTimer()
        {
            _autoCleanupTimer = new System.Windows.Threading.DispatcherTimer();
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

        /// <summary>
        /// Voert cleanup uit bij Windows startup en sluit dan af
        /// </summary>
        private async Task PerformStartupCleanupAsync()
        {
            LogStartupCleanup("PerformStartupCleanupAsync gestart");
            
            // Kleine vertraging om zeker te zijn dat alles geladen is
            await Task.Delay(500);
            
            if (string.IsNullOrEmpty(_settings.BackupFolderPath) || 
                !Directory.Exists(_settings.BackupFolderPath))
            {
                // Geen geldige backup map - sluit af
                LogStartupCleanup("Geen geldige backup map gevonden, afsluiten");
                ExitApplication();
                return;
            }

            try
            {
                LogStartupCleanup($"Start cleanup voor: {_settings.BackupFolderPath}");
                
                // Scan backups
                var allBackups = await Task.Run(() => 
                    BackupService.GetLightroomBackups(_settings.BackupFolderPath!));
                
                LogStartupCleanup($"Gevonden backups: {allBackups.Count}");
                
                // Bepaal welke te verwijderen
                var backupsToDelete = BackupService.GetBackupsToDelete(
                    allBackups, 
                    _settings.BackupsToKeep, 
                    _settings.MinimumAgeMonths);

                LogStartupCleanup($"Te verwijderen: {backupsToDelete.Count} (bewaren: {_settings.BackupsToKeep}, min leeftijd: {_settings.MinimumAgeMonths} maanden)");

                if (backupsToDelete.Any())
                {
                    var (deleted, freed, errors) = await Task.Run(() => BackupService.DeleteBackups(backupsToDelete));
                    LogStartupCleanup($"Verwijderd: {deleted} backups, {freed} bytes vrijgemaakt, {errors.Count} fouten");
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
                // Altijd afsluiten na startup cleanup
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
                // Negeer logging fouten
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
                
                // Scan backups
                var allBackups = await Task.Run(() => 
                    BackupService.GetLightroomBackups(_settings.BackupFolderPath!));
                
                // Bepaal welke te verwijderen
                var backupsToDelete = BackupService.GetBackupsToDelete(
                    allBackups, 
                    _settings.BackupsToKeep, 
                    _settings.MinimumAgeMonths);

                if (backupsToDelete.Any())
                {
                    var (deleted, freed, errors) = await Task.Run(() => 
                        BackupService.DeleteBackups(backupsToDelete));
                    
                    txtStatus.Text = LocalizationService.GetString("AutoCleanupComplete", deleted, FormatBytes(freed));
                    
                    if (_notifyIcon != null && _notifyIcon.Visible)
                    {
                        _notifyIcon.ShowBalloonTip(3000, 
                            LocalizationService.GetString("CleanupNotification"),
                            LocalizationService.GetString("CleanupNotificationMessage", deleted, FormatBytes(freed)),
                            errors.Any() ? ToolTipIcon.Warning : ToolTipIcon.Info);
                    }
                }
                else
                {
                    txtStatus.Text = LocalizationService.GetString("NoBackupsToClean");
                }

                _settings.LastAutoCleanup = DateTime.Now;
                SaveSettings();
                
                // Ververs de lijst
                await ScanBackupsAsync();
            }
            catch (Exception ex)
            {
                txtStatus.Text = LocalizationService.IsDutch 
                    ? $"Automatische opruiming mislukt: {ex.Message}"
                    : $"Automatic cleanup failed: {ex.Message}";
            }
        }

        private void BtnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            SelectFolder();
        }

        private void SelectFolder()
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = LocalizationService.GetString("SelectFolderDescription"),
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };
            
            if (!string.IsNullOrEmpty(_settings.BackupFolderPath) && 
                Directory.Exists(_settings.BackupFolderPath))
            {
                dialog.SelectedPath = _settings.BackupFolderPath;
            }

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Valideer dat dit een geldige Lightroom backup map is
                if (!LightroomDetectionService.IsValidBackupFolder(dialog.SelectedPath))
                {
                    var result = MessageBox.Show(
                        LocalizationService.GetString("InvalidBackupFolderMessage"),
                        LocalizationService.GetString("InvalidBackupFolder"),
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    
                    if (result != MessageBoxResult.Yes)
                        return;
                }

                _settings.BackupFolderPath = dialog.SelectedPath;
                _settings.CatalogName = LightroomDetectionService.GetCatalogNameFromBackupPath(dialog.SelectedPath);
                SaveSettings();
                
                UpdateCatalogInfo();
                _backups.Clear();
                _ = ScanBackupsAsync();
            }
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(_settings);
            settingsWindow.Owner = this;
            
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
                UpdateStartupRegistry();
                SaveSettings();
            };
            
            settingsWindow.LanguageChanged += (s, newLanguage) =>
            {
                SaveSettings();
            };
            
            settingsWindow.ShowDialog();
        }

        private void BtnScan_Click(object sender, RoutedEventArgs e)
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
                emptyState.Visibility = Visibility.Visible;
                txtStatus.Text = LocalizationService.GetString("SelectBackupFolder");
                return;
            }

            _isScanning = true;
            btnScan.IsEnabled = false;
            btnCleanup.IsEnabled = false;
            emptyState.Visibility = Visibility.Collapsed;
            searchingState.Visibility = Visibility.Collapsed;
            
            txtStatus.Text = LocalizationService.GetString("ScanningBackups");
            _backups.Clear();

            try
            {
                var allBackups = await Task.Run(() => 
                    BackupService.GetLightroomBackups(_settings.BackupFolderPath!));
                
                if (!allBackups.Any())
                {
                    emptyState.Visibility = Visibility.Visible;
                    txtEmptyTitle.Text = LocalizationService.GetString("NoBackupsFound");
                    txtEmptySubtitle.Text = LocalizationService.GetString("NoBackupsInFolder");
                    txtStatus.Text = LocalizationService.GetString("NoBackupsInFolder");
                    UpdateStats();
                    return;
                }

                // Bepaal welke backups verwijderd gaan worden
                var backupsToDelete = BackupService.GetBackupsToDelete(
                    allBackups, 
                    _settings.BackupsToKeep, 
                    _settings.MinimumAgeMonths);
                
                var backupsToDeletePaths = new HashSet<string>(backupsToDelete.Select(b => b.FolderPath));

                // Voeg alle backups toe aan de lijst
                foreach (var backup in allBackups)
                {
                    // IsSelected = true betekent "wordt verwijderd"
                    backup.IsSelected = backupsToDeletePaths.Contains(backup.FolderPath);
                    _backups.Add(backup);
                }

                UpdateStats();
                txtStatus.Text = LocalizationService.GetString("ScanComplete", allBackups.Count);
            }
            catch (Exception ex)
            {
                var errorTitle = LocalizationService.IsDutch ? "Fout bij scannen" : "Error during scan";
                txtStatus.Text = $"{errorTitle}: {ex.Message}";
                emptyState.Visibility = Visibility.Visible;
                txtEmptyTitle.Text = errorTitle;
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
            var toDelete = _backups.Where(b => b.IsSelected).ToList();
            var spaceToFree = toDelete.Sum(b => b.TotalSize);

            txtTotalBackups.Text = $"{total} backup(s)";
            txtToDelete.Text = toDelete.Count.ToString();
            txtSpaceToFree.Text = FormatBytes(spaceToFree);
        }

        private void BtnCleanup_Click(object sender, RoutedEventArgs e)
        {
            var backupsToDelete = _backups.Where(b => b.IsSelected).ToList();
            
            if (!backupsToDelete.Any())
            {
                MessageBox.Show(
                    LocalizationService.GetString("NoBackupsToDelete"),
                    LocalizationService.GetString("NoBackupsTitle"), 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
                return;
            }

            // Converteer naar FileToDelete voor preview window
            var catalogName = _settings.CatalogName ?? "Lightroom";
            var filesToDelete = BackupService.ConvertToFilesToDelete(backupsToDelete, catalogName);
            
            var previewWindow = new CleanupPreviewWindow(filesToDelete);
            previewWindow.Owner = this;
            
            if (previewWindow.ShowDialog() == true)
            {
                // Verwijder de backup mappen
                var (deleted, freed, errors) = BackupService.DeleteBackups(backupsToDelete);
                
                if (errors.Any())
                {
                    var errorList = string.Join("\n", errors.Take(5));
                    MessageBox.Show(
                        LocalizationService.GetString("CleanupCompleteWithErrorsMessage", deleted, FormatBytes(freed), errors.Count) + "\n" + errorList,
                        LocalizationService.GetString("CleanupCompleteWithErrorsTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show(
                        LocalizationService.GetString("CleanupCompleteMessage", deleted, FormatBytes(freed)),
                        LocalizationService.GetString("CleanupCompleteTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                
                // Ververs de lijst
                _ = ScanBackupsAsync();
            }
        }

        private void BtnDecreaseMinAge_Click(object sender, RoutedEventArgs e)
        {
            if (_settings.MinimumAgeMonths > 0)
            {
                _settings.MinimumAgeMonths--;
                txtMinAge.Text = _settings.MinimumAgeMonths.ToString();
                SaveSettings();
                RecalculateBackupsToDelete();
            }
        }

        private void BtnIncreaseMinAge_Click(object sender, RoutedEventArgs e)
        {
            if (_settings.MinimumAgeMonths < 24)
            {
                _settings.MinimumAgeMonths++;
                txtMinAge.Text = _settings.MinimumAgeMonths.ToString();
                SaveSettings();
                RecalculateBackupsToDelete();
            }
        }

        private void BtnDecreaseKeep_Click(object sender, RoutedEventArgs e)
        {
            if (_settings.BackupsToKeep > 1)
            {
                _settings.BackupsToKeep--;
                txtBackupsToKeep.Text = _settings.BackupsToKeep.ToString();
                SaveSettings();
                RecalculateBackupsToDelete();
            }
        }

        private void BtnIncreaseKeep_Click(object sender, RoutedEventArgs e)
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

            // Bepaal opnieuw welke backups verwijderd gaan worden
            var allBackups = _backups.ToList();
            var backupsToDelete = BackupService.GetBackupsToDelete(
                allBackups, 
                _settings.BackupsToKeep, 
                _settings.MinimumAgeMonths);
            
            var backupsToDeletePaths = new HashSet<string>(backupsToDelete.Select(b => b.FolderPath));

            foreach (var backup in _backups)
            {
                backup.IsSelected = backupsToDeletePaths.Contains(backup.FolderPath);
            }

            UpdateStats();
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

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_settings.AutoCleanupEnabled)
            {
                e.Cancel = true;
                WindowState = WindowState.Minimized;
                return;
            }
            
            SaveSettings();
            _notifyIcon?.Dispose();
            base.OnClosing(e);
        }

        private void PhotofactsLink_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://photofactsacademy.nl",
                    UseShellExecute = true
                });
            }
            catch
            {
                // Negeer fouten bij openen van browser
            }
        }

        private void CatalogInfo_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!string.IsNullOrEmpty(_settings.BackupFolderPath) && Directory.Exists(_settings.BackupFolderPath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _settings.BackupFolderPath,
                        UseShellExecute = true
                    });
                }
                catch
                {
                    // Negeer fouten bij openen van map
                }
            }
        }

        #region Windows Task Scheduler

        private void UpdateScheduledTask()
        {
            if (_settings.AutoCleanupEnabled)
            {
                CreateScheduledTask();
            }
            else
            {
                RemoveScheduledTask();
            }
        }

        private void CreateScheduledTask()
        {
            try
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath)) return;

                var taskName = "LightroomBackupCleanerDaily";
                var cleanupHour = _settings.AutoCleanupHour;
                
                var script = $@"
$taskName = '{taskName}'
$existingTask = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if ($existingTask) {{
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
}}

$action = New-ScheduledTaskAction -Execute '{exePath}' -Argument '--auto-cleanup'
$trigger = New-ScheduledTaskTrigger -Daily -At {cleanupHour}:00
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable
$principal = New-ScheduledTaskPrincipal -UserId $env:USERNAME -LogonType Interactive -RunLevel Limited

Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Description 'Dagelijkse Lightroom backup opruiming'
";

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"")}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(psi);
                process?.WaitForExit(10000);
            }
            catch
            {
                // Silently fail
            }
        }

        private void RemoveScheduledTask()
        {
            try
            {
                var taskName = "LightroomBackupCleanerDaily";
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"Unregister-ScheduledTask -TaskName '{taskName}' -Confirm:$false -ErrorAction SilentlyContinue\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                process?.WaitForExit(5000);
            }
            catch
            {
                // Silently fail
            }
        }

        #endregion

        #region Windows Startup Registry

        private void UpdateStartupRegistry()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, writable: true);
                if (key == null) return;

                if (_settings.RunAtStartup)
                {
                    var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        // Voeg toe aan startup met --auto-cleanup argument
                        key.SetValue(StartupValueName, $"\"{exePath}\" --auto-cleanup");
                    }
                }
                else
                {
                    // Verwijder uit startup
                    key.DeleteValue(StartupValueName, throwOnMissingValue: false);
                }
            }
            catch
            {
                // Silently fail bij registry fouten
            }
        }

        #endregion
    }
}
