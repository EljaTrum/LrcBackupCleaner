# Lightroom Classic Backup Cleaner

🇳🇱 [Nederlandse versie](README.nl.md)

A Windows application for automatically managing and cleaning up Adobe Lightroom Classic catalog backups.

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)
![Windows](https://img.shields.io/badge/Platform-Windows-0078D6)
![License](https://img.shields.io/badge/License-MIT-green)

## 📸 Features

- **Automatic detection** - Automatically finds your Lightroom backup location
- **Clear overview** - View all backups with date, age, and size
- **Smart cleanup** - Keep the newest X backups, only delete old ones
- **Minimum age** - Backups younger than X months are never deleted
- **Automatic cleanup** - Daily at a configurable time
- **Cleanup at Windows startup** - Automatic cleanup when your PC starts
- **Old version backup detection** - Detects and removes "Old Lightroom Catalogs" folders
- **System tray** - Runs in the background
- **Multilingual** - Dutch and English, with automatic language detection
- **Self-contained** - No .NET runtime installation required

## 🚀 Installation

### Pre-compiled version
Download `LightroomBackupCleaner.zip` via the link below, extract the .exe and place it wherever you like. The application is self-contained and requires no additional installation.
Link: https://photofactsacademy.s3.eu-west-1.amazonaws.com/LightroomBackupCleaner.zip

### Build from source
```bash
git clone https://github.com/EljaTrum/LrcBackupCleaner.git
cd LrcBackupCleaner
dotnet publish -c Release -o publish
```

The application is built to the `publish/` folder as a single exe file.

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

Click on the backup path at the top to open the folder in Explorer.

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
4. The app creates a Windows Scheduled Task

### Cleanup at Windows startup
1. Click "⚙️ Settings"
2. Enable "Cleanup at Windows startup"
3. At each Windows startup, old backups are automatically deleted
4. The app then closes (doesn't run permanently)

### Old Lightroom version backups
When Lightroom Classic receives a major version update, Adobe automatically creates a backup of your old catalog in a folder called "Old Lightroom Catalogs". The app automatically detects this folder and:
- Shows a warning if the folder is older than 1 month
- Lets you delete the folder with one click
- The folder location is clickable to open in Explorer

This helps you clean up old catalog files after a successful update.

## 🎨 Screenshot

The application has a dark theme inspired by Adobe Lightroom Classic:

- Dark background (#121212)
- Accent color blue (#0EA5E9)
- Clear status indicators (✓ green / ✕ red)
- Custom dark scrollbars

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
```
%APPDATA%\LightroomBackupCleaner\settings.json
```

Startup cleanup logging is stored in:
```
%APPDATA%\LightroomBackupCleaner\startup-cleanup.log
```

## 🔧 Development

### Project structure
```
├── MainWindow.xaml/.cs          # Main window
├── SettingsWindow.xaml/.cs      # Settings dialog
├── CleanupPreviewWindow.xaml/.cs # Preview for deletion
├── Models/
│   ├── LightroomBackup.cs       # Backup model
│   └── FileToDelete.cs          # File to delete model
├── Services/
│   ├── BackupService.cs         # Backup scan/delete logic
│   ├── LightroomDetectionService.cs  # Auto-detection
│   ├── SettingsService.cs       # Settings storage
│   └── LocalizationService.cs   # Multilingual support
├── Resources/
│   ├── Strings.resx             # English translations
│   └── Strings.nl.resx          # Dutch translations
├── IconGenerator.cs             # App icon generation
└── app.ico                      # Embedded app icon
```

### Building
```bash
dotnet build                    # Debug build
dotnet publish -c Release -o publish  # Release single-file exe
```

### Regenerate icon
If you want to customize the app icon:
1. Modify `IconGenerator.cs`
2. Temporarily run with `--generate-icon` argument
3. Rebuild the project

## 📄 License

MIT License - see [LICENSE.md](LICENSE.md)

## 👨‍💻 Credits

Made by **[Photofacts Academy](https://photofactsacademy.nl)**

## 🙏 Contributing

Contributions are welcome! Open an issue or pull request.

---

*Made with ❤️ for photographers who want to keep their Lightroom backups under control.*
