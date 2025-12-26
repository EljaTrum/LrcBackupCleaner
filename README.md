# Lightroom Classic Backup Cleaner

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
- **Opruimen bij Windows start** - Automatisch opschonen bij opstarten PC
- **Oude versie backup detectie** - Detecteert en verwijdert "Old Lightroom Catalogs" mappen
- **System tray** - Draait op de achtergrond
- **Meertalig** - Nederlands en Engels, met automatische taaldetectie
- **Self-contained** - Geen .NET runtime installatie nodig

## 🚀 Installatie

### Voorgecompileerde versie
Download `LightroomBackupCleaner.zip` via onderstaande link, pak de .exe uit en plaats deze waar je wilt. De applicatie is self-contained en heeft geen extra installatie nodig.
Link: https://photofactsacademy.s3.eu-west-1.amazonaws.com/LightroomBackupCleaner.zip

### Bouwen vanuit broncode
```bash
git clone https://github.com/EljaTrum/LrcBackupCleaner.git
cd LrcBackupCleaner
dotnet publish -c Release -o publish
```

De applicatie wordt gebouwd naar de `publish/` map als een enkel exe-bestand.

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

Klik op het backup pad bovenin om de map in Verkenner te openen.

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
4. De app maakt een Windows Scheduled Task aan

### Opruimen bij Windows start
1. Klik op "⚙️ Instellingen"
2. Schakel "Opruimen bij Windows start" in
3. Bij elke Windows start worden oude backups automatisch verwijderd
4. De app sluit daarna weer af (draait niet permanent)

### Oude Lightroom versie backups
Wanneer Lightroom Classic een grote versie-update krijgt, maakt Adobe automatisch een backup van je oude catalogus in een map genaamd "Old Lightroom Catalogs". De app detecteert deze map automatisch en:
- Toont een waarschuwing als de map ouder is dan 1 maand
- Laat je de map met één klik verwijderen
- De maplocatie is klikbaar om te openen in Verkenner

Dit helpt je om na een succesvolle update de oude catalogusbestanden op te ruimen.

## 🎨 Screenshot

De applicatie heeft een donker thema geïnspireerd door Adobe Lightroom Classic:

- Donkere achtergrond (#121212)
- Accent kleur blauw (#0EA5E9)
- Duidelijke status indicatoren (✓ groen / ✕ rood)
- Custom donkere scrollbars

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
```
%APPDATA%\LightroomBackupCleaner\settings.json
```

Startup cleanup logging wordt opgeslagen in:
```
%APPDATA%\LightroomBackupCleaner\startup-cleanup.log
```

## 🔧 Ontwikkeling

### Projectstructuur
```
├── MainWindow.xaml/.cs          # Hoofdvenster
├── SettingsWindow.xaml/.cs      # Instellingen dialoog
├── CleanupPreviewWindow.xaml/.cs # Preview voor verwijderen
├── Models/
│   ├── LightroomBackup.cs       # Backup model
│   └── FileToDelete.cs          # Te verwijderen bestand model
├── Services/
│   ├── BackupService.cs         # Backup scan/delete logica
│   ├── LightroomDetectionService.cs  # Auto-detectie
│   ├── SettingsService.cs       # Settings opslag
│   └── LocalizationService.cs   # Meertaligheid
├── Resources/
│   ├── Strings.resx             # Engelse vertalingen
│   └── Strings.nl.resx          # Nederlandse vertalingen
├── IconGenerator.cs             # App icoon generatie
└── app.ico                      # Ingebakken app icoon
```

### Bouwen
```bash
dotnet build                    # Debug build
dotnet publish -c Release -o publish  # Release single-file exe
```

### Icoon regenereren
Als je het app icoon wilt aanpassen:
1. Wijzig `IconGenerator.cs`
2. Run tijdelijk met `--generate-icon` argument
3. Herbouw het project

## 📄 Licentie

MIT License - zie [LICENSE.md](LICENSE.md)

## 👨‍💻 Credits

Gemaakt door **[Photofacts Academy](https://photofactsacademy.nl)**

## 🙏 Bijdragen

Bijdragen zijn welkom! Open een issue of pull request.

---

*Gemaakt met ❤️ voor fotografen die hun Lightroom backups onder controle willen houden.*
