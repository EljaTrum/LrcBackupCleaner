using System;
using System.Diagnostics;
using System.Windows;
using BackupCleaner.Services;

namespace BackupCleaner
{
    public partial class SettingsWindow : Window
    {
        private AppSettings _settings;
        private bool _isInitialized = false;
        
        public event EventHandler? AutoCleanupChanged;

        public SettingsWindow(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            
            // Initialiseer UI
            chkAutoCleanup.IsChecked = _settings.AutoCleanupEnabled;
            UpdateCleanupHourDisplay();
            UpdateAutoCleanupInfo();
            
            _isInitialized = true;
        }

        private void ChkAutoCleanup_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            
            _settings.AutoCleanupEnabled = chkAutoCleanup.IsChecked == true;
            UpdateAutoCleanupInfo();
            
            AutoCleanupChanged?.Invoke(this, EventArgs.Empty);
        }

        private void BtnDecreaseHour_Click(object sender, RoutedEventArgs e)
        {
            _settings.AutoCleanupHour--;
            if (_settings.AutoCleanupHour < 0)
                _settings.AutoCleanupHour = 23;
            
            UpdateCleanupHourDisplay();
            UpdateAutoCleanupInfo();
            AutoCleanupChanged?.Invoke(this, EventArgs.Empty);
        }

        private void BtnIncreaseHour_Click(object sender, RoutedEventArgs e)
        {
            _settings.AutoCleanupHour++;
            if (_settings.AutoCleanupHour > 23)
                _settings.AutoCleanupHour = 0;
            
            UpdateCleanupHourDisplay();
            UpdateAutoCleanupInfo();
            AutoCleanupChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateCleanupHourDisplay()
        {
            txtCleanupHour.Text = $"{_settings.AutoCleanupHour:D2}:00";
        }

        private void UpdateAutoCleanupInfo()
        {
            if (chkAutoCleanup.IsChecked == true)
            {
                timeSettings.Visibility = Visibility.Visible;
                txtAutoCleanupInfo.Visibility = Visibility.Visible;
                
                var now = DateTime.Now;
                var cleanupHour = _settings.AutoCleanupHour;
                
                if (_settings.LastAutoCleanup?.Date == DateTime.Today)
                {
                    // Vandaag al uitgevoerd
                    txtAutoCleanupInfo.Text = $"✓ Laatste opruiming: {_settings.LastAutoCleanup:HH:mm} vandaag • Volgende: morgen {cleanupHour:D2}:00";
                    txtAutoCleanupInfo.Foreground = FindResource("SuccessBrush") as System.Windows.Media.SolidColorBrush;
                }
                else
                {
                    // Nog niet uitgevoerd vandaag
                    string nextRunText;
                    if (now.Hour < cleanupHour)
                    {
                        nextRunText = $"vandaag om {cleanupHour:D2}:00";
                    }
                    else
                    {
                        nextRunText = $"morgen om {cleanupHour:D2}:00";
                    }
                    
                    txtAutoCleanupInfo.Text = $"⏱ Volgende opruiming: {nextRunText}";
                    txtAutoCleanupInfo.Foreground = FindResource("TextSecondaryBrush") as System.Windows.Media.SolidColorBrush;
                }
            }
            else
            {
                timeSettings.Visibility = Visibility.Collapsed;
                txtAutoCleanupInfo.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
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
    }
}
