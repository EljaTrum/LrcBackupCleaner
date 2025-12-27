using System.Runtime.InteropServices;

namespace BackupCleaner.Services.PlatformServices;

/// <summary>
/// Platform-specifieke services factory
/// </summary>
public static class Platform
{
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
}

