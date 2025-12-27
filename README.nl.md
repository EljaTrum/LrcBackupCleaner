# Lightroom Classic Backup Cleaner

🇬🇧 [English version](README.md)

Een cross-platform applicatie voor het automatisch beheren en opruimen van Adobe Lightroom Classic catalogus backups.

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)
![Windows](https://img.shields.io/badge/Platform-Windows-0078D6)
![macOS](https://img.shields.io/badge/Platform-macOS-000000)
![Avalonia](https://img.shields.io/badge/UI-Avalonia-8B44AC)
![License](https://img.shields.io/badge/License-MIT-green)

## 📸 Functionaliteiten

- **Cross-platform** - Werkt op Windows en macOS (Intel & Apple Silicon)
- **Automatische detectie** - Vindt automatisch je Lightroom backup locatie
- **Overzichtelijke lijst** - Bekijk al je backups met datum, leeftijd en grootte
- **Slimme opruiming** - Behoud de nieuwste X backups, verwijder alleen oude
- **Minimale leeftijd** - Backups jonger dan X maanden worden nooit verwijderd
- **Automatische opruiming** - Dagelijks op een instelbaar tijdstip
- **Opruimen bij opstarten** - Automatisch opschonen bij opstarten systeem
- **Oude versie backup detectie** - Detecteert en verwijdert "Old Lightroom Catalogs" mappen
- **System tray** - Draait op de achtergrond (Windows)
- **Meertalig** - Nederlands en Engels, met automatische taaldetectie
- **Self-contained** - Geen .NET runtime installatie nodig

## 🚀 Installatie

### Voorgecompileerde versie

Download de juiste versie voor je platform:

| Platform | Download |
|----------|----------|
| Windows (x64) | [LightroomBackupCleaner-win.zip](https://photofactsacademy.s3.eu-west-1.amazonaws.com/LightroomBackupCleaner-win.zip) |
| macOS (Intel) | [LightroomBackupCleaner-osx-x64.zip](https://photofactsacademy.s3.eu-west-1.amazonaws.com/LightroomBackupCleaner-osx-x64.zip) |
| macOS (Apple Silicon) | [LightroomBackupCleaner-osx-arm64.zip](https://photofactsacademy.s3.eu-west-1.amazonaws.com/LightroomBackupCleaner-osx-arm64.zip) |

Pak uit en start de applicatie. De applicatie is self-contained en heeft geen extra installatie nodig.

### Bouwen vanuit broncode
```bash
git clone https://github.com/EljaTrum/LrcBackupCleaner.git
cd LrcBackupCleaner

# Windows
dotnet publish -c Release -r win-x64 -o publish/win-x64

# macOS Intel
dotnet publish -c Release -r osx-x64 -o publish/osx-x64

# macOS Apple Silicon
dotnet publish -c Release -r osx-arm64 -o publish/osx-arm64
```

## 📖 Gebruik

### Eerste start
1. Start de applicatie
2. De app zoekt automatisch naar Lightroom backup locaties
3. Als meerdere locaties worden gevonden, kies de juiste
4. Of selecteer handmatig een backup map via "Map Wijzigen"

### Backup lijst
De backup lijst toont alle gevonden backups met:
- **Datum** - Wanneer de backup is gemaakt
- **Leeftijd** - Hoe oud de backup is
- **Grootte** - Totale grootte van de backup
- **Status** - ✓ (behouden) of ✕ (verwijderen)

Klik op het backup pad bovenin om de map te openen in je bestandsbeheerder.

### Instellingen

| Instelling | Beschrijving |
|------------|--------------|
| **Bewaren** | Aantal backups dat altijd behouden blijft (nieuwste eerst) |
| **Min. leeftijd** | Backups jonger dan X maanden worden nooit verwijderd |
| **Taal** | Kies Nederlands of Engels (of automatisch) |

### Automatisch opruimen
1. Klik op "⚙️ Instellingen"
2. Schakel "Automatisch dagelijks opruimen" in
3. Stel het gewenste tijdstip in
4. De app maakt een geplande taak aan (Windows Task Scheduler / macOS launchd)

### Opruimen bij opstarten
1. Klik op "⚙️ Instellingen"
2. Schakel "Opruimen bij opstarten" in
3. Bij elke systeemstart worden oude backups automatisch verwijderd
4. De app sluit daarna weer af (draait niet permanent)

### Oude Lightroom versie backups
Wanneer Lightroom Classic een grote versie-update krijgt, maakt Adobe automatisch een backup van je oude catalogus in een map genaamd "Old Lightroom Catalogs". De app detecteert deze map automatisch en:
- Toont een waarschuwing als de map ouder is dan 1 maand
- Laat je de map met één klik verwijderen
- De maplocatie is klikbaar om te openen in je bestandsbeheerder

Dit helpt je om na een succesvolle update de oude catalogusbestanden op te ruimen.

## 🎨 Screenshot

De applicatie heeft een donker thema geïnspireerd door Adobe Lightroom Classic:

- Donkere achtergrond (#121212)
- Accent kleur blauw (#0EA5E9)
- Duidelijke status indicatoren (✓ groen / ✕ rood)

## 📁 Lightroom Backup Structuur

Lightroom Classic maakt backups in mappen met formaat:
```
Backups/
├── 2024-12-01 0800/
│   └── MyCatalog.lrcat (of .zip)
├── 2024-12-15 0800/
│   └── MyCatalog.lrcat (of .zip)
└── 2024-12-25 0800/
    └── MyCatalog.lrcat (of .zip)
```

De app herkent dit formaat automatisch en ondersteunt zowel `.lrcat` als `.zip` backup bestanden.

## ⚙️ Configuratie

Instellingen worden opgeslagen in:

| Platform | Locatie |
|----------|---------|
| Windows | `%APPDATA%\LightroomBackupCleaner\settings.json` |
| macOS | `~/Library/Application Support/LightroomBackupCleaner/settings.json` |

## 🔧 Ontwikkeling

### Technologie Stack
- **.NET 8** - Cross-platform runtime
- **Avalonia UI** - Cross-platform UI framework
- **SkiaSharp** - Cross-platform graphics voor icoon generatie

### Projectstructuur
```
├── Views/
│   ├── MainWindow.axaml/.cs         # Hoofdvenster
│   ├── SettingsWindow.axaml/.cs     # Instellingen dialoog
│   └── CleanupPreviewWindow.axaml/.cs # Preview voor verwijderen
├── ViewModels/
│   └── LightroomBackupViewModel.cs  # Backup lijst item ViewModel
├── Models/
│   ├── LightroomBackup.cs           # Backup model
│   └── FileToDelete.cs              # Te verwijderen bestand model
├── Services/
│   ├── BackupService.cs             # Backup scan/delete logica
│   ├── LightroomDetectionService.cs # Auto-detectie
│   ├── SettingsService.cs           # Settings opslag
│   ├── LocalizationService.cs       # Meertaligheid
│   └── Platform/
│       ├── IPlatformServices.cs     # Platform abstractie
│       ├── WindowsServices.cs       # Windows-specifieke features
│       └── MacOSServices.cs         # macOS-specifieke features
├── Styles/
│   └── AppStyles.axaml              # UI styles en kleuren
├── Resources/
│   ├── Strings.resx                 # Engelse vertalingen
│   └── Strings.nl.resx              # Nederlandse vertalingen
├── Assets/
│   └── app.ico                      # Applicatie icoon
├── App.axaml/.cs                    # Applicatie entry
├── Program.cs                       # Main entry point
└── IconGenerator.cs                 # App icoon generatie (SkiaSharp)
```

### Bouwen
```bash
dotnet build                                        # Debug build
dotnet publish -c Release -r win-x64 -o publish     # Windows release
dotnet publish -c Release -r osx-x64 -o publish     # macOS Intel release
dotnet publish -c Release -r osx-arm64 -o publish   # macOS ARM release
```

## 📄 Licentie

MIT License - zie [LICENSE.md](LICENSE.md)

## 👨‍💻 Credits

Gemaakt door **[Photofacts Academy](https://photofactsacademy.nl)**

## 🙏 Bijdragen

Bijdragen zijn welkom! Open een issue of pull request.

---

*Gemaakt met ❤️ voor fotografen die hun Lightroom backups onder controle willen houden.*
