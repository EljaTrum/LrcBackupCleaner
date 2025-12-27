# Agents.md - AI Assistant Instructies

Dit bestand bevat context en instructies voor AI assistenten die werken aan dit project.

## Project Overzicht

**Lightroom Classic Backup Cleaner** is een cross-platform Avalonia UI applicatie (.NET 8) voor het automatisch opruimen van oude Adobe Lightroom Classic catalogus backups.

### Belangrijke kenmerken
- Avalonia UI met donker Lightroom-geïnspireerd thema
- Cross-platform: Windows en macOS (Intel + Apple Silicon)
- Automatische detectie van Lightroom backup locaties
- Async/await voor UI responsiviteit tijdens scans
- System tray integratie voor achtergrond draaien (Windows)
- Windows Startup integratie (via Registry) voor automatische opruiming bij PC start
- Windows Task Scheduler integratie voor dagelijkse automatische opruiming
- JSON-based settings opslag
- Meertalig (Nederlands/Engels) met automatische systeemtaal detectie
- Self-contained single-file executable (geen .NET runtime nodig)

## Architectuur

### UI Layer (Avalonia)
- `Views/MainWindow.axaml/.cs` - Hoofdvenster met backup lijst, instellingen, statusbalk
- `Views/CleanupPreviewWindow.axaml/.cs` - Preview van te verwijderen backups
- `Views/SettingsWindow.axaml/.cs` - Instellingen voor automatisch opruimen, taal, Windows startup
- `App.axaml/.cs` - Applicatie entry point, globale resources
- `Styles/AppStyles.axaml` - Donker thema, button/checkbox styles, kleuren

### ViewModels
- `LightroomBackupViewModel` - ViewModel voor backup items met status, kleuren, tooltips

### Models
- `LightroomBackup` - Representeert een Lightroom backup map (INotifyPropertyChanged)
- `FileToDelete` - Informatie over te verwijderen backup voor preview
- `AppSettings` - Applicatie instellingen model

### Services
- `BackupService` - Core logica: scannen Lightroom backups, bepalen wat te verwijderen
- `LightroomDetectionService` - Auto-detectie van Lightroom backup locaties (Windows + macOS paden)
- `SettingsService` - JSON opslag in platform-specifieke locatie
- `LocalizationService` - Meertaligheid met resource files

### Platform Services
- `Services/Platform/IPlatformServices.cs` - Interface voor platform-specifieke functionaliteit
- `Services/Platform/WindowsServices.cs` - Windows implementatie (Registry, Task Scheduler, NotifyIcon)
- `Services/Platform/MacOSServices.cs` - macOS implementatie (launchd, preferences)

### Hulpklassen
- `IconGenerator` - Genereert programmatisch het app icoon ("Lrc" stijl) met SkiaSharp

### Resources
- `Resources/Strings.resx` - Engelse UI teksten
- `Resources/Strings.nl.resx` - Nederlandse UI teksten
- `Assets/app.ico` - Ingebakken applicatie icoon

## Lightroom Backup Structuur

Adobe Lightroom Classic maakt backups als volgt:
- Backup mappen hebben formaat: `YYYY-MM-DD HHMM` (bijv. "2024-12-25 1430")
- Elke backup map bevat een `.lrcat` catalogus bestand OF een `.zip` gecomprimeerde backup
- Backups worden typisch opgeslagen in een "Backups" submap naast de catalogus

### Auto-detectie locaties

**Windows:**
- Afbeeldingen/Lightroom
- Documenten/Lightroom
- Alle vaste schijven: /Lightroom, /Lightroom Catalog, /Adobe/Lightroom

**macOS:**
- ~/Library/Application Support/Adobe/Lightroom
- ~/Pictures/Lightroom
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
**Windows:**
- **Dagelijks**: Windows Scheduled Task op ingesteld tijdstip
- **Bij Windows start**: Via Registry (`HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`) met `--auto-cleanup` argument

**macOS:**
- **Dagelijks**: launchd met `StartCalendarInterval` in `~/Library/LaunchAgents/`
- **Bij login**: launchd met `RunAtLoad` in `~/Library/LaunchAgents/`

### Old Lightroom Catalogs detectie
Bij grote Lightroom versie updates maakt Adobe een "Old Lightroom Catalogs" map aan met een backup van de oude catalogus. De app:
1. Zoekt automatisch naar deze map in de buurt van de backup locatie
2. Als gevonden én ouder dan 1 maand, toont een waarschuwingsblokje
3. Gebruiker kan de map met één klik verwijderen
4. De map is klikbaar om in Verkenner/Finder te openen

### Startup Cleanup Flow
1. App start met `--auto-cleanup` argument
2. Window wordt verborgen
3. Backups worden gescand en verouderde backups verwijderd
4. Resultaat wordt gelogd naar settings directory
5. App sluit af

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
- Avalonia XAML met `.axaml` extensie

## UI/UX richtlijnen

### Kleuren (donker thema - Lightroom stijl)
Gedefinieerd in `Styles/AppStyles.axaml`:
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

### Avalonia Styles
- `Button.primary`, `Button.secondary`, `Button.danger`, `Button.small`, `Button.icon`
- `Border.card` - Card styling voor secties
- `CheckBox.modern` - Moderne checkbox styling
- `TextBlock.clickable`, `TextBlock.link` - Klikbare teksten

## Bekende beperkingen

1. System tray alleen op Windows
2. Geen undo functionaliteit na verwijderen
3. Ondersteunt slechts één backup locatie tegelijk
4. Taalwisseling vereist herstart van instellingen venster

## Test scenario's

Bij wijzigingen, test:
1. Eerste start (geen settings) - auto-detectie backup locatie
2. Meerdere backup locaties gevonden - keuze dialoog
3. Scan met veel backups - voortgang en responsiviteit
4. Wijzigen van instellingen - herberekening "te verwijderen"
5. Windows startup cleanup - start met `--auto-cleanup`, check log file
6. Taalwisseling - Nederlands ↔ Engels
7. Klik op backup pad - opent Verkenner/Finder
8. Preview window - toont correct icoon, één bevestiging
9. Test op zowel Windows als macOS

## Dependencies

- **Avalonia** (11.x) - Cross-platform UI framework
- **Avalonia.Desktop** - Desktop platform support
- **Avalonia.Themes.Fluent** - Fluent theme
- **Newtonsoft.Json** - Settings serialization
- **SkiaSharp** (2.88.9) - Cross-platform icon generation

## Publishing

Self-contained single-file executables:

```bash
# Windows
dotnet publish -c Release -r win-x64 -o publish/win-x64

# macOS Intel
dotnet publish -c Release -r osx-x64 -o publish/osx-x64

# macOS Apple Silicon
dotnet publish -c Release -r osx-arm64 -o publish/osx-arm64
```

Resultaat: Platform-specifieke executable (~80-100MB, bevat .NET runtime)

Project settings in `.csproj`:
- `PublishSingleFile` - Alles in één executable
- `SelfContained` - .NET runtime inbegrepen
- `IncludeNativeLibrariesForSelfExtract` - Native libraries inbegrepen
- `EnableCompressionInSingleFile` - Kleinere bestandsgrootte
- `ApplicationIcon` - app.ico voor Windows Verkenner icoon
