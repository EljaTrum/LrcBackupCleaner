using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using BackupCleaner.Services;

namespace BackupCleaner;

public partial class SettingsWindow : Window
{
    private AppSettings _settings;
    private bool _isInitialized = false;
    private string _originalLanguage = "";
    
    public event EventHandler? AutoCleanupChanged;
    public event EventHandler? RunAtStartupChanged;
    public event EventHandler<string>? LanguageChanged;

    public SettingsWindow() : this(new AppSettings()) { }

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        _originalLanguage = settings.Language;
        
        // Set window icon
        Icon = IconGenerator.CreateWindowIcon();
        
        // Initialiseer UI
        chkAutoCleanup.IsChecked = _settings.AutoCleanupEnabled;
        chkRunAtStartup.IsChecked = _settings.RunAtStartup;
        
        // Stel de juiste taal radiobutton in
        switch (_settings.Language)
        {
            case LocalizationService.LanguageEnglish:
                rbLanguageEnglish.IsChecked = true;
                break;
            case LocalizationService.LanguageDutch:
                rbLanguageDutch.IsChecked = true;
                break;
            default:
                rbLanguageAuto.IsChecked = true;
                break;
        }
        
        UpdateCleanupHourDisplay();
        UpdateAutoCleanupInfo();
        UpdateStartupInfo();
        ApplyLocalization();
        
        _isInitialized = true;
    }
    
    private void ApplyLocalization()
    {
        Title = LocalizationService.GetString("SettingsTitle");
        
        txtAutoCleanupTitle.Text = LocalizationService.GetString("AutoCleanupTitle");
        txtAutoCleanupDescription.Text = LocalizationService.GetString("AutoCleanupDescription");
        chkAutoCleanup.Content = LocalizationService.GetString("AutoCleanupCheckbox");
        txtCleanupAtLabel.Text = LocalizationService.GetString("CleanupAt");
        txtHourLabel.Text = LocalizationService.GetString("Hour");
        
        txtStartupTitle.Text = LocalizationService.GetString("StartupTitle");
        txtStartupDescription.Text = LocalizationService.GetString("StartupDescription");
        chkRunAtStartup.Content = LocalizationService.GetString("StartupCheckbox");
        txtStartupInfo.Text = LocalizationService.GetString("StartupInfo");
        
        txtLanguageTitle.Text = LocalizationService.GetString("LanguageTitle");
        txtLanguageDescription.Text = LocalizationService.GetString("LanguageDescription");
        txtLangAuto.Text = LocalizationService.GetString("LanguageAuto");
        
        txtAboutTitle.Text = LocalizationService.GetString("AboutTitle");
        txtAboutDescription1.Text = LocalizationService.GetString("AboutDescription1");
        txtMadeBy.Text = LocalizationService.GetString("MadeBy") + " ";
        txtVersion.Text = LocalizationService.GetString("Version");
        
        btnClose.Content = LocalizationService.GetString("Close");
    }

    private void ChkAutoCleanup_Changed(object? sender, RoutedEventArgs e)
    {
        if (!_isInitialized) return;
        
        _settings.AutoCleanupEnabled = chkAutoCleanup.IsChecked == true;
        UpdateAutoCleanupInfo();
        
        AutoCleanupChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ChkRunAtStartup_Changed(object? sender, RoutedEventArgs e)
    {
        if (!_isInitialized) return;
        
        _settings.RunAtStartup = chkRunAtStartup.IsChecked == true;
        UpdateStartupInfo();
        
        RunAtStartupChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateStartupInfo()
    {
        txtStartupInfo.IsVisible = chkRunAtStartup.IsChecked == true;
    }

    private void BtnDecreaseHour_Click(object? sender, RoutedEventArgs e)
    {
        _settings.AutoCleanupHour--;
        if (_settings.AutoCleanupHour < 0)
            _settings.AutoCleanupHour = 23;
        
        UpdateCleanupHourDisplay();
        UpdateAutoCleanupInfo();
        AutoCleanupChanged?.Invoke(this, EventArgs.Empty);
    }

    private void BtnIncreaseHour_Click(object? sender, RoutedEventArgs e)
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
            timeSettings.IsVisible = true;
            txtAutoCleanupInfo.IsVisible = true;
            
            var now = DateTime.Now;
            var cleanupHour = _settings.AutoCleanupHour;
            
            if (_settings.LastAutoCleanup?.Date == DateTime.Today)
            {
                txtAutoCleanupInfo.Text = $"✓ Laatste opruiming: {_settings.LastAutoCleanup:HH:mm} vandaag";
            }
            else
            {
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
            }
        }
        else
        {
            timeSettings.IsVisible = false;
            txtAutoCleanupInfo.IsVisible = false;
        }
    }

    private void BtnClose_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void PhotofactsLink_Click(object? sender, PointerPressedEventArgs e)
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
            // Ignore errors
        }
    }
    
    private void LanguageOption_Changed(object? sender, RoutedEventArgs e)
    {
        if (!_isInitialized) return;
        
        string newLanguage;
        
        if (rbLanguageEnglish.IsChecked == true)
            newLanguage = LocalizationService.LanguageEnglish;
        else if (rbLanguageDutch.IsChecked == true)
            newLanguage = LocalizationService.LanguageDutch;
        else
            newLanguage = LocalizationService.LanguageAuto;
        
        if (newLanguage != _settings.Language)
        {
            _settings.Language = newLanguage;
            
            if (newLanguage != _originalLanguage)
            {
                txtLanguageInfo.Text = LocalizationService.IsDutch 
                    ? "⚠️ Sluit en open de app opnieuw om de taalwijziging toe te passen."
                    : "⚠️ Close and reopen the app to apply the language change.";
                txtLanguageInfo.IsVisible = true;
            }
            else
            {
                txtLanguageInfo.IsVisible = false;
            }
            
            LanguageChanged?.Invoke(this, newLanguage);
        }
    }
}

