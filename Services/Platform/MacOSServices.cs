using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace BackupCleaner.Services.PlatformServices;

/// <summary>
/// macOS-specifieke functionaliteit
/// </summary>
public static class MacOS
{
    private const string LaunchAgentName = "nl.photofactsacademy.lightroombackupcleaner";

    /// <summary>
    /// Update de macOS Login Item voor auto-cleanup bij login
    /// </summary>
    public static void UpdateLoginItem(AppSettings settings)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return;

        try
        {
            var launchAgentsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library", "LaunchAgents");

            var plistPath = Path.Combine(launchAgentsPath, $"{LaunchAgentName}.plist");

            if (settings.RunAtStartup)
            {
                // Maak de LaunchAgents directory als deze niet bestaat
                if (!Directory.Exists(launchAgentsPath))
                {
                    Directory.CreateDirectory(launchAgentsPath);
                }

                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath)) return;

                var plistContent = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    <key>Label</key>
    <string>{LaunchAgentName}</string>
    <key>ProgramArguments</key>
    <array>
        <string>{exePath}</string>
        <string>--auto-cleanup</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
    <key>StartInterval</key>
    <integer>86400</integer>
</dict>
</plist>";

                File.WriteAllText(plistPath, plistContent);

                // Laad de launch agent
                RunCommand("launchctl", $"load \"{plistPath}\"");
            }
            else
            {
                if (File.Exists(plistPath))
                {
                    // Unload en verwijder de launch agent
                    RunCommand("launchctl", $"unload \"{plistPath}\"");
                    File.Delete(plistPath);
                }
            }
        }
        catch
        {
            // Silently fail
        }
    }

    /// <summary>
    /// Update de macOS launchd voor dagelijkse cleanup
    /// </summary>
    public static void UpdateScheduledTask(AppSettings settings)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return;

        try
        {
            var launchAgentsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library", "LaunchAgents");

            var plistPath = Path.Combine(launchAgentsPath, $"{LaunchAgentName}.daily.plist");

            if (settings.AutoCleanupEnabled)
            {
                if (!Directory.Exists(launchAgentsPath))
                {
                    Directory.CreateDirectory(launchAgentsPath);
                }

                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath)) return;

                var plistContent = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    <key>Label</key>
    <string>{LaunchAgentName}.daily</string>
    <key>ProgramArguments</key>
    <array>
        <string>{exePath}</string>
        <string>--auto-cleanup</string>
    </array>
    <key>StartCalendarInterval</key>
    <dict>
        <key>Hour</key>
        <integer>{settings.AutoCleanupHour}</integer>
        <key>Minute</key>
        <integer>0</integer>
    </dict>
</dict>
</plist>";

                File.WriteAllText(plistPath, plistContent);
                RunCommand("launchctl", $"load \"{plistPath}\"");
            }
            else
            {
                if (File.Exists(plistPath))
                {
                    RunCommand("launchctl", $"unload \"{plistPath}\"");
                    File.Delete(plistPath);
                }
            }
        }
        catch
        {
            // Silently fail
        }
    }

    private static void RunCommand(string command, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            process?.WaitForExit(5000);
        }
        catch
        {
            // Ignore errors
        }
    }
}

