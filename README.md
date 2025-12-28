# Lightroom Classic Backup Cleaner

🇳🇱 [Nederlandse versie](README.nl.md)

A cross-platform application for automatically managing and cleaning up Adobe Lightroom Classic catalog backups.

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)
![Windows](https://img.shields.io/badge/Platform-Windows-0078D6)
![macOS](https://img.shields.io/badge/Platform-macOS-000000)
![Avalonia](https://img.shields.io/badge/UI-Avalonia-8B44AC)
![License](https://img.shields.io/badge/License-MIT-green)

## 📸 Features

- **Cross-platform** - Works on Windows and macOS (Intel & Apple Silicon)
- **Automatic detection** - Automatically finds your Lightroom backup location
- **Clear overview** - View all backups with date, age, and size
- **Smart cleanup** - Keep the newest X backups, only delete old ones
- **Minimum age** - Backups younger than X months are never deleted
- **Automatic cleanup** - Daily at a configurable time
- **Cleanup at startup** - Automatic cleanup when your system starts
- **Old version backup detection** - Detects and removes "Old Lightroom Catalogs" folders
- **System tray** - Runs in the background (Windows)
- **Multilingual** - Dutch and English, with automatic language detection
- **Self-contained** - No .NET runtime installation required

## 🚀 Installation

### Pre-compiled version

Download the appropriate version for your platform:

| Platform | Download |
|----------|----------|
| Windows (x64) | [LightroomBackupCleaner-win.zip](https://photofactsacademy.s3.eu-west-1.amazonaws.com/LightroomBackupCleaner-win.zip) |
| macOS (Intel) | [LightroomBackupCleaner-osx-x64.dmg]([https://photofactsacademy.s3.eu-west-1.amazonaws.com/LightroomBackupCleaner-osx-x64.zip](https://photofactsacademy.s3.eu-west-1.amazonaws.com/LrcBackupCleaner-osx-x64.dmg)) |
| macOS (Apple Silicon) | [LightroomBackupCleaner-osx-arm64.dmg]([https://photofactsacademy.s3.eu-west-1.amazonaws.com/LightroomBackupCleaner-osx-arm64.zip](https://photofactsacademy.s3.eu-west-1.amazonaws.com/LrcBackupCleaner-osx-arm64.dmg)) |

Extract and run the application. It's self-contained and requires no additional installation.

### Build from source

#### Windows
```bash
git clone https://github.com/EljaTrum/LrcBackupCleaner.git
cd LrcBackupCleaner
dotnet publish -c Release -r win-x64 -o publish/win-x64
```

#### macOS (with code signing and DMG)
For macOS, use the build script to create a properly signed .app bundle and DMG:

```bash
git clone https://github.com/EljaTrum/LrcBackupCleaner.git
cd LrcBackupCleaner

# Make script executable
chmod +x build-macos.sh

# Build for Apple Silicon (M1/M2/M3)
./build-macos.sh osx-arm64

# Build for Intel Macs
./build-macos.sh osx-x64

# Build with code signing (requires Apple Developer account)
./build-macos.sh osx-arm64 "Developer ID Application: Your Name (TEAM_ID)"
```

The script will:
- Build the application
- Create a proper `.app` bundle with Info.plist
- Code sign the app (if identity provided)
- Create a DMG file for distribution

**Without code signing**: Users will need to remove quarantine attributes:
```bash
xattr -dr com.apple.quarantine LightroomBackupCleaner.app
```

**With code signing**: The app will work without manual steps, and Gatekeeper won't block it.

## 📖 Usage

### First launch
1. Start the application
2. The app automatically searches for Lightroom backup locations
3. If multiple locations are found, choose the correct one
4. Or manually select a backup folder via "Change Folder"

### Backup list
The backup list shows all found backups with:
- **Date** - When the backup was created
- **Age** - How old the backup is
- **Size** - Total size of the backup
- **Status** - ✓ (keep) or ✕ (delete)

Click on the backup path at the top to open the folder in your file manager.

### Settings

| Setting | Description |
|---------|-------------|
| **Keep** | Number of backups that will always be kept (newest first) |
| **Min. age** | Backups younger than X months are never deleted |
| **Language** | Choose Dutch or English (or automatic) |

### Automatic cleanup
1. Click "⚙️ Settings"
2. Enable "Automatic daily cleanup"
3. Set the desired time
4. The app creates a scheduled task (Windows Task Scheduler / macOS launchd)

### Cleanup at startup
1. Click "⚙️ Settings"
2. Enable "Cleanup at startup"
3. At each system startup, old backups are automatically deleted
4. The app then closes (doesn't run permanently)

### Old Lightroom version backups
When Lightroom Classic receives a major version update, Adobe automatically creates a backup of your old catalog in a folder called "Old Lightroom Catalogs". The app automatically detects this folder and:
- Shows a warning if the folder is older than 1 month
- Lets you delete the folder with one click
- The folder location is clickable to open in your file manager

This helps you clean up old catalog files after a successful update.

## 🎨 Screenshot

The application has a dark theme inspired by Adobe Lightroom Classic:

- Dark background (#121212)
- Accent color blue (#0EA5E9)
- Clear status indicators (✓ green / ✕ red)

## 📁 Lightroom Backup Structure

Lightroom Classic creates backups in folders with the format:
```
Backups/
├── 2024-12-01 0800/
│   └── MyCatalog.lrcat (or .zip)
├── 2024-12-15 0800/
│   └── MyCatalog.lrcat (or .zip)
└── 2024-12-25 0800/
    └── MyCatalog.lrcat (or .zip)
```

The app automatically recognizes this format and supports both `.lrcat` and `.zip` backup files.

## ⚙️ Configuration

Settings are stored in:

| Platform | Location |
|----------|----------|
| Windows | `%APPDATA%\LightroomBackupCleaner\settings.json` |
| macOS | `~/Library/Application Support/LightroomBackupCleaner/settings.json` |

## 🔧 Development

### Technology Stack
- **.NET 8** - Cross-platform runtime
- **Avalonia UI** - Cross-platform UI framework
- **SkiaSharp** - Cross-platform graphics for icon generation

### Project structure
```
├── Views/
│   ├── MainWindow.axaml/.cs         # Main window
│   ├── SettingsWindow.axaml/.cs     # Settings dialog
│   └── CleanupPreviewWindow.axaml/.cs # Preview for deletion
├── ViewModels/
│   └── LightroomBackupViewModel.cs  # Backup list item ViewModel
├── Models/
│   ├── LightroomBackup.cs           # Backup model
│   └── FileToDelete.cs              # File to delete model
├── Services/
│   ├── BackupService.cs             # Backup scan/delete logic
│   ├── LightroomDetectionService.cs # Auto-detection
│   ├── SettingsService.cs           # Settings storage
│   ├── LocalizationService.cs       # Multilingual support
│   └── Platform/
│       ├── IPlatformServices.cs     # Platform abstraction
│       ├── WindowsServices.cs       # Windows-specific features
│       └── MacOSServices.cs         # macOS-specific features
├── Styles/
│   └── AppStyles.axaml              # UI styles and colors
├── Resources/
│   ├── Strings.resx                 # English translations
│   └── Strings.nl.resx              # Dutch translations
├── Assets/
│   └── app.ico                      # Application icon
├── App.axaml/.cs                    # Application entry
├── Program.cs                       # Main entry point
└── IconGenerator.cs                 # App icon generation (SkiaSharp)
```

### Building
```bash
dotnet build                                        # Debug build
dotnet publish -c Release -r win-x64 -o publish     # Windows release
dotnet publish -c Release -r osx-x64 -o publish     # macOS Intel release
dotnet publish -c Release -r osx-arm64 -o publish   # macOS ARM release
```

## 📄 License

MIT License - see [LICENSE.md](LICENSE.md)

## 👨‍💻 Credits

Made by **[Photofacts Academy](https://photofactsacademy.nl)**

## 🙏 Contributing

Contributions are welcome! Open an issue or pull request.

---

*Made with ❤️ for photographers who want to keep their Lightroom backups under control.*
