using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace BackupCleaner.Services.PlatformServices;

/// <summary>
/// Windows-specifieke functionaliteit
/// </summary>
public static class Windows
{
    private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupValueName = "LightroomClassicBackupCleaner";

    /// <summary>
    /// Update de Windows startup registry voor auto-cleanup bij login
    /// </summary>
    public static void UpdateStartupRegistry(AppSettings settings)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, writable: true);
            if (key == null) return;

            if (settings.RunAtStartup)
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(StartupValueName, $"\"{exePath}\" --auto-cleanup");
                }
            }
            else
            {
                key.DeleteValue(StartupValueName, throwOnMissingValue: false);
            }
        }
        catch
        {
            // Silently fail bij registry fouten
        }
    }

    /// <summary>
    /// Update de Windows Task Scheduler voor dagelijkse cleanup
    /// </summary>
    public static void UpdateScheduledTask(AppSettings settings)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        if (settings.AutoCleanupEnabled)
        {
            CreateScheduledTask(settings);
        }
        else
        {
            RemoveScheduledTask();
        }
    }

    private static void CreateScheduledTask(AppSettings settings)
    {
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath)) return;

            var taskName = "LightroomBackupCleanerDaily";
            var cleanupHour = settings.AutoCleanupHour;
            
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

    private static void RemoveScheduledTask()
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
}

