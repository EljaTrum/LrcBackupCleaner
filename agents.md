# Agents.md - AI Assistant Instructies

Dit bestand bevat context en instructies voor AI assistenten die werken aan dit project.

## Project Overzicht

**Lightroom Backup Cleaner** is een Windows WPF applicatie (.NET 8) voor het automatisch opruimen van oude Adobe Lightroom Classic catalogus backups.

### Belangrijke kenmerken
- WPF met donker Lightroom-geïnspireerd thema
- Automatische detectie van Lightroom backup locaties
- Async/await voor UI responsiviteit tijdens scans
- System tray integratie voor achtergrond draaien
- Windows Task Scheduler integratie voor automatische opruiming
- JSON-based settings opslag

## Architectuur

### UI Layer
- `MainWindow.xaml/.cs` - Hoofdvenster met backup lijst, instellingen, statusbalk
- `CleanupPreviewWindow.xaml/.cs` - Preview van te verwijderen backups
- `SettingsWindow.xaml/.cs` - Instellingen voor automatisch opruimen
- `App.xaml` - Globale resources, donker thema, button/checkbox styles

### Models
- `LightroomBackup` - Representeert een Lightroom backup map (INotifyPropertyChanged)
- `FileToDelete` - Informatie over te verwijderen backup voor preview
- `BackupSet` - (Legacy) Groep bestanden met dezelfde datum

### Services
- `BackupService` - Core logica: scannen Lightroom backups, bepalen wat te verwijderen
- `LightroomDetectionService` - Auto-detectie van Lightroom backup locaties
- `SettingsService` - JSON opslag in %APPDATA%/LightroomBackupCleaner
- `IgnoreService` - (Legacy) Voor negeerlijst, niet actief gebruikt

### Hulpklassen
- `IconGenerator` - Genereert programmatisch het app icoon (Lightroom "Lr" stijl)

## Lightroom Backup Structuur

Adobe Lightroom Classic maakt backups als volgt:
- Backup mappen hebben formaat: `YYYY-MM-DD HHMM` (bijv. "2024-12-25 1430")
- Elke backup map bevat een `.lrcat` catalogus bestand
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
2. Controleren of de map `.lrcat` bestanden bevat
3. Berekenen totale grootte van de backup map

### Verwijdering criteria
Een backup mag verwijderd worden als:
1. Er meer backups zijn dan het ingestelde "bewaren" aantal
2. De backup ouder is dan de ingestelde minimale leeftijd (in maanden)

De nieuwste X backups worden altijd bewaard, ongeacht leeftijd.

### Automatische opruiming
- Timer checkt elke minuut of het ingestelde tijdstip is bereikt
- Windows Scheduled Task wordt aangemaakt voor opruiming wanneer app niet draait
- Bij automatische run: scan backups, bepaal te verwijderen, verwijder

## Code conventies

- Nederlandse UI teksten en comments
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
- BEWAAR (groen): Backup wordt behouden
- WISSEN (rood): Backup wordt verwijderd bij opruiming

## Bekende beperkingen

1. Icoon wordt programmatisch gegenereerd (geen .ico bestand)
2. Scheduled task vereist dat gebruiker ingelogd is
3. Geen logging naar bestand (alleen UI status)
4. Geen undo functionaliteit na verwijderen
5. Ondersteunt slechts één backup locatie tegelijk

## Test scenario's

Bij wijzigingen, test:
1. Eerste start (geen settings) - auto-detectie backup locatie
2. Meerdere backup locaties gevonden - keuze dialoog
3. Scan met veel backups - voortgang en responsiviteit
4. Wijzigen van instellingen - herberekening "te verwijderen"
5. Minimaliseren met auto-cleanup aan - system tray
6. Sluiten met auto-cleanup aan - moet naar tray gaan

## Dependencies

- Newtonsoft.Json (settings serialization)
- System.Drawing.Common (icon generation)
- Windows Forms (FolderBrowserDialog, NotifyIcon)
