# Lightroom Backup Cleaner

Een Windows applicatie voor het automatisch beheren en opruimen van Adobe Lightroom Classic catalogus backups.

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)
![Windows](https://img.shields.io/badge/Platform-Windows-0078D6)
![License](https://img.shields.io/badge/License-MIT-green)

## 📸 Functionaliteiten

- **Automatische detectie** - Vindt automatisch je Lightroom backup locatie
- **Overzichtelijke lijst** - Bekijk al je backups met datum, leeftijd en grootte
- **Slimme opruiming** - Behoud de nieuwste X backups, verwijder alleen oude
- **Minimale leeftijd** - Backups jonger dan X maanden worden nooit verwijderd
- **Automatische opruiming** - Dagelijks op een instelbaar tijdstip
- **System tray** - Draait op de achtergrond

## 🚀 Installatie

### Vereisten
- Windows 10/11
- .NET 8.0 Runtime

### Bouwen vanuit broncode
```bash
git clone https://github.com/yourusername/LightroomBackupCleaner.git
cd LightroomBackupCleaner
dotnet build -c Release
```

De applicatie wordt gebouwd naar `bin/Release/net8.0-windows/`.

## 📖 Gebruik

### Eerste start
1. Start de applicatie
2. De app zoekt automatisch naar Lightroom backup locaties
3. Als meerdere locaties worden gevonden, kies de juiste
4. Of selecteer handmatig een backup map via "Map Wijzigen"

### Instellingen

| Instelling | Beschrijving |
|------------|--------------|
| **Bewaren** | Aantal backups dat altijd behouden blijft (nieuwste eerst) |
| **Min. leeftijd** | Backups jonger dan X maanden worden nooit verwijderd |

### Automatisch opruimen
1. Klik op "⚙️ Instellingen"
2. Schakel "Automatisch dagelijks opruimen" in
3. Stel het gewenste tijdstip in
4. De app maakt een Windows Scheduled Task aan

## 🎨 Screenshot

De applicatie heeft een donker thema geïnspireerd door Adobe Lightroom Classic:

- Donkere achtergrond (#121212)
- Accent kleur blauw (#0EA5E9)
- Duidelijke status indicatoren (BEWAAR/WISSEN)

## 📁 Lightroom Backup Structuur

Lightroom Classic maakt backups in mappen met formaat:
```
Backups/
├── 2024-12-01 0800/
│   └── MyCatalog.lrcat
├── 2024-12-15 0800/
│   └── MyCatalog.lrcat
└── 2024-12-25 0800/
    └── MyCatalog.lrcat
```

De app herkent dit formaat automatisch en berekent de grootte van elke backup.

## ⚙️ Configuratie

Instellingen worden opgeslagen in:
```
%APPDATA%\LightroomBackupCleaner\settings.json
```

## 🔧 Ontwikkeling

### Projectstructuur
```
├── MainWindow.xaml/.cs      # Hoofdvenster
├── SettingsWindow.xaml/.cs  # Instellingen dialoog
├── CleanupPreviewWindow/    # Preview voor verwijderen
├── Models/
│   └── LightroomBackup.cs   # Backup model
├── Services/
│   ├── BackupService.cs     # Backup scan/delete logica
│   ├── LightroomDetectionService.cs  # Auto-detectie
│   └── SettingsService.cs   # Settings opslag
└── IconGenerator.cs         # App icoon generatie
```

### Bouwen
```bash
dotnet build
dotnet run
```

## 📄 Licentie

MIT License - zie [LICENSE.md](LICENSE.md)

## 🙏 Bijdragen

Bijdragen zijn welkom! Open een issue of pull request.

---

*Gemaakt met ❤️ voor fotografen die hun Lightroom backups onder controle willen houden.*
