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

        public MainWindow()
        {
            InitializeComponent();
            
            lstBackups.ItemsSource = _backups;
            
            LoadSettings();
            SetupAutoCleanupTimer();
            SetupSystemTray();
            
            // Check voor command line argument voor scheduled task
            var args = Environment.GetCommandLineArgs();
            if (args.Contains("--auto-cleanup"))
            {
                WindowState = WindowState.Minimized;
                Hide();
                _ = PerformAutomaticCleanupAsync();
            }
            else
            {
                // Normale startup - check backup locatie
                _ = InitializeBackupLocationAsync();
            }
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
            txtStatus.Text = "Zoeken naar Lightroom backups...";
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
                txtEmptyTitle.Text = "Geen Lightroom backups gevonden";
                txtEmptySubtitle.Text = "Selecteer handmatig een Lightroom backup map";
                txtStatus.Text = "Geen backup locatie gevonden";
            }
        }

        private void ShowBackupLocationChoice(List<string> locations)
        {
            var message = "Er zijn meerdere Lightroom backup locaties gevonden:\n\n";
            for (int i = 0; i < locations.Count; i++)
            {
                var catalogName = LightroomDetectionService.GetCatalogNameFromBackupPath(locations[i]);
                message += $"{i + 1}. {catalogName}\n   {locations[i]}\n\n";
            }
            message += "Wil je de eerste locatie gebruiken?\n\nKlik 'Nee' om zelf een map te selecteren.";

            var result = MessageBox.Show(message, "Meerdere backup locaties", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
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
                txtCatalogInfo.Text = "Geen catalogus geselecteerd";
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
            contextMenu.Items.Add("Openen", null, (s, e) => ShowWindow());
            contextMenu.Items.Add("Opruimen starten", null, async (s, e) => await PerformAutomaticCleanupAsync());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Afsluiten", null, (s, e) => ExitApplication());
            
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
                    "Applicatie draait in de achtergrond", ToolTipIcon.Info);
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

        private async Task PerformAutomaticCleanupAsync()
        {
            if (string.IsNullOrEmpty(_settings.BackupFolderPath) || 
                !Directory.Exists(_settings.BackupFolderPath))
                return;

            try
            {
                txtStatus.Text = "Automatische opruiming gestart...";
                
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
                    
                    txtStatus.Text = $"Automatisch opgeruimd: {deleted} backup(s) verwijderd ({FormatBytes(freed)})";
                    
                    if (_notifyIcon != null && _notifyIcon.Visible)
                    {
                        _notifyIcon.ShowBalloonTip(3000, "Opruiming voltooid",
                            $"{deleted} backup(s) verwijderd, {FormatBytes(freed)} vrijgemaakt",
                            errors.Any() ? ToolTipIcon.Warning : ToolTipIcon.Info);
                    }
                }
                else
                {
                    txtStatus.Text = "Automatische opruiming: geen backups om te verwijderen";
                }

                _settings.LastAutoCleanup = DateTime.Now;
                SaveSettings();
                
                // Ververs de lijst
                await ScanBackupsAsync();
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"Automatische opruiming mislukt: {ex.Message}";
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
                Description = "Selecteer de Lightroom backup map (bevat mappen met datum formaat)",
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
                        "Deze map lijkt geen geldige Lightroom backup locatie te zijn.\n\n" +
                        "Lightroom backup mappen bevatten submappen met formaat 'YYYY-MM-DD HHMM'.\n\n" +
                        "Wil je deze map toch gebruiken?",
                        "Ongeldige backup map",
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
                txtStatus.Text = "Selecteer eerst een backup map";
                return;
            }

            _isScanning = true;
            btnScan.IsEnabled = false;
            btnCleanup.IsEnabled = false;
            emptyState.Visibility = Visibility.Collapsed;
            searchingState.Visibility = Visibility.Collapsed;
            
            txtStatus.Text = "Backups scannen...";
            _backups.Clear();

            try
            {
                var allBackups = await Task.Run(() => 
                    BackupService.GetLightroomBackups(_settings.BackupFolderPath!));
                
                if (!allBackups.Any())
                {
                    emptyState.Visibility = Visibility.Visible;
                    txtEmptyTitle.Text = "Geen backups gevonden";
                    txtEmptySubtitle.Text = "Deze map bevat geen Lightroom backup mappen";
                    txtStatus.Text = "Geen backups in geselecteerde map";
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
                txtStatus.Text = $"Scan voltooid - {allBackups.Count} backup(s) gevonden";
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"Fout bij scannen: {ex.Message}";
                emptyState.Visibility = Visibility.Visible;
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
                    "Er zijn geen backups geselecteerd om te verwijderen.\n\n" +
                    "Pas de instellingen aan (meer backups bewaren of kortere minimale leeftijd) " +
                    "als je meer backups wilt verwijderen.",
                    "Geen backups", 
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
                    MessageBox.Show(
                        $"Er zijn {deleted} backup(s) verwijderd ({FormatBytes(freed)} vrijgemaakt).\n\n" +
                        $"Er waren {errors.Count} fouten:\n{string.Join("\n", errors.Take(5))}",
                        "Opruimen voltooid met fouten",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show(
                        $"Er zijn {deleted} backup(s) verwijderd.\n{FormatBytes(freed)} schijfruimte vrijgemaakt.",
                        "Opruimen voltooid",
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
    }
}
