# Agents.md - AI Assistant Instructies

Dit bestand bevat context en instructies voor AI assistenten die werken aan dit project.

## Project Overzicht

**Lightroom Classic Backup Cleaner** is een Windows WPF applicatie (.NET 8) voor het automatisch opruimen van oude Adobe Lightroom Classic catalogus backups.

### Belangrijke kenmerken
- WPF met donker Lightroom-geïnspireerd thema
- Automatische detectie van Lightroom backup locaties
- Async/await voor UI responsiviteit tijdens scans
- System tray integratie voor achtergrond draaien
- Windows Startup integratie (via Registry) voor automatische opruiming bij PC start
- Windows Task Scheduler integratie voor dagelijkse automatische opruiming
- JSON-based settings opslag
- Meertalig (Nederlands/Engels) met automatische systeemtaal detectie
- Self-contained single-file executable (geen .NET runtime nodig)

## Architectuur

### UI Layer
- `MainWindow.xaml/.cs` - Hoofdvenster met backup lijst, instellingen, statusbalk
- `CleanupPreviewWindow.xaml/.cs` - Preview van te verwijderen backups
- `SettingsWindow.xaml/.cs` - Instellingen voor automatisch opruimen, taal, Windows startup
- `App.xaml` - Globale resources, donker thema, button/checkbox/scrollbar styles

### Models
- `LightroomBackup` - Representeert een Lightroom backup map (INotifyPropertyChanged)
- `FileToDelete` - Informatie over te verwijderen backup voor preview
- `BackupSet` - (Legacy) Groep bestanden met dezelfde datum
- `CustomerFolder` - (Legacy) Niet gebruikt

### Services
- `BackupService` - Core logica: scannen Lightroom backups, bepalen wat te verwijderen
- `LightroomDetectionService` - Auto-detectie van Lightroom backup locaties
- `SettingsService` - JSON opslag in %APPDATA%/LightroomBackupCleaner
- `LocalizationService` - Meertaligheid met resource files
- `IgnoreService` - (Legacy) Voor negeerlijst, niet actief gebruikt

### Hulpklassen
- `IconGenerator` - Genereert programmatisch het app icoon ("Lrc" stijl met vinkje)

### Resources
- `Resources/Strings.resx` - Engelse UI teksten
- `Resources/Strings.nl.resx` - Nederlandse UI teksten
- `app.ico` - Ingebakken applicatie icoon

## Lightroom Backup Structuur

Adobe Lightroom Classic maakt backups als volgt:
- Backup mappen hebben formaat: `YYYY-MM-DD HHMM` (bijv. "2024-12-25 1430")
- Elke backup map bevat een `.lrcat` catalogus bestand OF een `.zip` gecomprimeerde backup
- Backups worden typisch opgeslagen in een "Backups" submap naast de catalogus

### Auto-detectie locaties
De app zoekt automatisch in:
- Afbeeldingen/Lightroom
- Documenten/Lightroom
- Alle vaste schijven: /Lightroom, /Lightroom Catalog, /Adobe/Lightroom
- Mappen die een `Backups` submap bevatten met datum-format submappen

## Belangrijke logica

### Backup detectie
Backups worden gevonden door:
1. Zoeken naar mappen met patroon `YYYY-MM-DD HHMM`
2. Controleren of de map `.lrcat` OF `.zip` bestanden bevat
3. Berekenen totale grootte van de backup map

### Verwijdering criteria
Een backup mag verwijderd worden als:
1. Er meer backups zijn dan het ingestelde "bewaren" aantal
2. De backup ouder is dan de ingestelde minimale leeftijd (in maanden)

De nieuwste X backups worden altijd bewaard, ongeacht leeftijd.

### Automatische opruiming
- **Dagelijks**: Timer checkt elke minuut of het ingestelde tijdstip is bereikt. Windows Scheduled Task kan ook worden aangemaakt.
- **Bij Windows start**: Via Registry (`HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`) met `--auto-cleanup` argument. App start verborgen, voert cleanup uit, sluit af.

### Old Lightroom Catalogs detectie
Bij grote Lightroom versie updates maakt Adobe een "Old Lightroom Catalogs" map aan met een backup van de oude catalogus. De app:
1. Zoekt automatisch naar deze map in de buurt van de backup locatie
2. Als gevonden én ouder dan 1 maand, toont een waarschuwingsblokje
3. Gebruiker kan de map met één klik verwijderen
4. De map is klikbaar om in Verkenner te openen

### Startup Cleanup Flow
1. App start met `--auto-cleanup` argument
2. Window wordt verborgen (`Visibility.Hidden`, `ShowInTaskbar = false`)
3. Na `ContentRendered` event wordt cleanup gestart
4. Backups worden gescand en verouderde backups verwijderd
5. Resultaat wordt gelogd naar `%APPDATA%\LightroomBackupCleaner\startup-cleanup.log`
6. App sluit af

## Meertaligheid

### Implementatie
- Resource files: `Resources/Strings.resx` (Engels) en `Resources/Strings.nl.resx` (Nederlands)
- `LocalizationService.GetString(key)` haalt vertaling op
- Taalvoorkeur wordt opgeslagen in settings
- Automatische detectie van systeemtaal bij eerste start

### Nieuwe teksten toevoegen
1. Voeg key-value toe aan beide `.resx` bestanden
2. Gebruik `LocalizationService.GetString("KeyName")` in code
3. Of bind in XAML via code-behind `ApplyLocalization()`

## Code conventies

- Nederlandse UI teksten (standaard) en Engelse alternatief
- C# 12 features (.NET 8)
- Async void alleen voor event handlers
- `_` prefix voor private fields
- Null-coalescing en null-conditional operators waar passend

## UI/UX richtlijnen

### Kleuren (donker thema - Lightroom stijl)
- Primary: #1A1A1A (zeer donker)
- Secondary: #2D2D2D (donkergrijs)
- Accent: #0EA5E9 (Lightroom blauw)
- Background: #121212 (zwart-achtig)
- Card: #1E1E1E (donkergrijs)
- Text Primary: #F5F5F5 (wit)
- Text Secondary: #A3A3A3 (grijs)
- Success: #22C55E (groen)
- Warning: #F59E0B (oranje)
- Danger: #EF4444 (rood)
- Border: #404040 (donkergrijs)

### Backup status indicatoren
- ✓ (wit op groen): Backup wordt behouden
- ✕ (wit op rood): Backup wordt verwijderd bij opruiming

### Custom styles
- `DarkScrollBar` - Donkere scrollbar passend bij thema
- Alle vensters gebruiken consistent hetzelfde app icoon

## Bekende beperkingen

1. Scheduled task vereist dat gebruiker ingelogd is
2. Geen undo functionaliteit na verwijderen
3. Ondersteunt slechts één backup locatie tegelijk
4. Taalwisseling vereist herstart van instellingen venster

## Test scenario's

Bij wijzigingen, test:
1. Eerste start (geen settings) - auto-detectie backup locatie
2. Meerdere backup locaties gevonden - keuze dialoog
3. Scan met veel backups - voortgang en responsiviteit
4. Wijzigen van instellingen - herberekening "te verwijderen"
5. Minimaliseren met auto-cleanup aan - system tray
6. Sluiten met auto-cleanup aan - moet naar tray gaan
7. Windows startup cleanup - start met `--auto-cleanup`, check log file
8. Taalwisseling - Nederlands ↔ Engels
9. Klik op backup pad - opent Verkenner
10. Preview window - toont correct icoon, één bevestiging

## Dependencies

- Newtonsoft.Json (settings serialization)
- System.Drawing.Common (icon generation)
- Windows Forms (FolderBrowserDialog, NotifyIcon)

## Publishing

Self-contained single-file executable:
```bash
dotnet publish -c Release -o publish
```

Resultaat: `publish/LightroomBackupCleaner.exe` (~80MB, bevat .NET runtime)

Project settings in `.csproj`:
- `PublishSingleFile` - Alles in één exe
- `SelfContained` - .NET runtime inbegrepen
- `IncludeNativeLibrariesForSelfExtract` - Native DLLs inbegrepen
- `EnableCompressionInSingleFile` - Kleinere bestandsgrootte
- `ApplicationIcon` - app.ico voor Verkenner icoon
