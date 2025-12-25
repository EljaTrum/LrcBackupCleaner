using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BackupCleaner.Services
{
    /// <summary>
    /// Service voor het detecteren van Adobe Lightroom Classic backup locaties
    /// </summary>
    public static class LightroomDetectionService
    {
        // Lightroom backup bestands extensies (catalogus of gezipt)
        private static readonly string[] BackupExtensions = { ".lrcat", ".zip" };
        
        // Regex voor backup map naam (YYYY-MM-DD HHMM formaat)
        private static readonly Regex BackupFolderPattern = new Regex(
            @"^(\d{4})-(\d{2})-(\d{2})\s+(\d{4})$",
            RegexOptions.Compiled);

        /// <summary>
        /// Zoek automatisch naar Lightroom backup locaties
        /// </summary>
        public static List<string> FindBackupLocations()
        {
            var foundLocations = new List<string>();
            
            // Standaard Lightroom catalogus locaties om te doorzoeken
            var searchPaths = GetPotentialCatalogPaths();
            
            foreach (var searchPath in searchPaths)
            {
                if (!Directory.Exists(searchPath))
                    continue;

                try
                {
                    // Zoek naar "Backups" mappen
                    var backupFolders = FindBackupFoldersInPath(searchPath);
                    foundLocations.AddRange(backupFolders);
                }
                catch
                {
                    // Toegangsfout - negeren en doorgaan
                }
            }

            return foundLocations.Distinct().ToList();
        }

        /// <summary>
        /// Krijg potentiële paden waar Lightroom catalogi kunnen staan
        /// </summary>
        private static List<string> GetPotentialCatalogPaths()
        {
            var paths = new List<string>();
            
            // Standaard Pictures/Lightroom locatie
            var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            if (!string.IsNullOrEmpty(pictures))
            {
                paths.Add(Path.Combine(pictures, "Lightroom"));
                paths.Add(pictures);
            }
            
            // Documenten map
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!string.IsNullOrEmpty(documents))
            {
                paths.Add(Path.Combine(documents, "Lightroom"));
                paths.Add(documents);
            }
            
            // User profile root
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(userProfile))
            {
                paths.Add(Path.Combine(userProfile, "Lightroom"));
            }

            // Alle vaste schijven doorzoeken voor Lightroom mappen
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType == DriveType.Fixed && drive.IsReady)
                {
                    paths.Add(Path.Combine(drive.RootDirectory.FullName, "Lightroom"));
                    paths.Add(Path.Combine(drive.RootDirectory.FullName, "Lightroom Catalog"));
                    paths.Add(Path.Combine(drive.RootDirectory.FullName, "Lightroom Catalogs"));
                    paths.Add(Path.Combine(drive.RootDirectory.FullName, "Adobe", "Lightroom"));
                }
            }
            
            return paths.Where(p => !string.IsNullOrEmpty(p)).Distinct().ToList();
        }

        /// <summary>
        /// Zoek naar backup mappen in een gegeven pad
        /// </summary>
        private static List<string> FindBackupFoldersInPath(string searchPath)
        {
            var backupLocations = new List<string>();
            
            try
            {
                // Zoek eerst naar directe "Backups" submappen
                var backupsDir = Path.Combine(searchPath, "Backups");
                if (Directory.Exists(backupsDir) && IsValidBackupFolder(backupsDir))
                {
                    backupLocations.Add(backupsDir);
                }

                // Zoek naar catalogi en hun Backups mappen (max 2 niveaus diep)
                SearchForBackupFolders(searchPath, 0, 2, backupLocations);
            }
            catch
            {
                // Toegangsfout negeren
            }
            
            return backupLocations;
        }

        /// <summary>
        /// Recursief zoeken naar backup mappen
        /// </summary>
        private static void SearchForBackupFolders(string path, int currentDepth, int maxDepth, List<string> results)
        {
            if (currentDepth >= maxDepth)
                return;

            try
            {
                foreach (var dir in Directory.GetDirectories(path))
                {
                    var dirName = Path.GetFileName(dir);
                    
                    // Check of dit een "Backups" map is met geldige backups
                    if (dirName.Equals("Backups", StringComparison.OrdinalIgnoreCase))
                    {
                        if (IsValidBackupFolder(dir))
                        {
                            results.Add(dir);
                        }
                    }
                    // Check of dit een catalogus map is (bevat .lrcat bestand)
                    else if (ContainsCatalog(dir))
                    {
                        var backupsSubdir = Path.Combine(dir, "Backups");
                        if (Directory.Exists(backupsSubdir) && IsValidBackupFolder(backupsSubdir))
                        {
                            results.Add(backupsSubdir);
                        }
                    }
                    
                    // Recursief verder zoeken
                    SearchForBackupFolders(dir, currentDepth + 1, maxDepth, results);
                }
            }
            catch
            {
                // Toegangsfout negeren
            }
        }

        /// <summary>
        /// Controleer of een map catalogus bestanden bevat
        /// </summary>
        private static bool ContainsCatalog(string path)
        {
            try
            {
                return Directory.GetFiles(path, "*.lrcat").Any();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Controleer of een backup map geldige backup bestanden bevat
        /// </summary>
        private static bool ContainsBackupFiles(string path)
        {
            try
            {
                foreach (var ext in BackupExtensions)
                {
                    if (Directory.GetFiles(path, $"*{ext}").Any())
                        return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Controleer of dit een geldige Lightroom backup map is
        /// </summary>
        public static bool IsValidBackupFolder(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    return false;

                // Check of er submappen zijn met het backup datum formaat
                var subdirs = Directory.GetDirectories(path);
                var hasDateFolders = subdirs.Any(d => IsBackupDateFolder(Path.GetFileName(d)));
                
                if (!hasDateFolders)
                    return false;

                // Check of minstens één datum-map backup bestanden bevat
                foreach (var subdir in subdirs)
                {
                    if (IsBackupDateFolder(Path.GetFileName(subdir)) && ContainsBackupFiles(subdir))
                        return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Controleer of een mapnaam het Lightroom backup formaat heeft (YYYY-MM-DD HHMM)
        /// </summary>
        public static bool IsBackupDateFolder(string folderName)
        {
            return BackupFolderPattern.IsMatch(folderName);
        }

        /// <summary>
        /// Parse de datum uit een backup mapnaam
        /// </summary>
        public static DateTime? ParseBackupFolderDate(string folderName)
        {
            var match = BackupFolderPattern.Match(folderName);
            if (!match.Success)
                return null;

            try
            {
                var year = int.Parse(match.Groups[1].Value);
                var month = int.Parse(match.Groups[2].Value);
                var day = int.Parse(match.Groups[3].Value);
                var timeStr = match.Groups[4].Value;
                var hour = int.Parse(timeStr.Substring(0, 2));
                var minute = int.Parse(timeStr.Substring(2, 2));

                return new DateTime(year, month, day, hour, minute, 0);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Haal de catalogus naam uit het backup pad
        /// </summary>
        public static string GetCatalogNameFromBackupPath(string backupPath)
        {
            try
            {
                // Backups map staat typisch naast de catalogus
                // Pad: .../MyCatalog/Backups of .../Backups (naast catalogus)
                var parentDir = Directory.GetParent(backupPath);
                if (parentDir == null)
                    return "Onbekende Catalogus";

                // Zoek naar .lrcat bestand in de parent
                var catalogFiles = Directory.GetFiles(parentDir.FullName, "*.lrcat");
                if (catalogFiles.Any())
                {
                    return Path.GetFileNameWithoutExtension(catalogFiles.First());
                }

                // Anders gebruik de parent mapnaam
                return parentDir.Name;
            }
            catch
            {
                return "Lightroom Catalogus";
            }
        }

        /// <summary>
        /// Zoek naar "Old Lightroom Catalogs" map nabij de backup locatie
        /// </summary>
        public static OldCatalogsInfo? FindOldLightroomCatalogs(string backupPath)
        {
            try
            {
                if (string.IsNullOrEmpty(backupPath) || !Directory.Exists(backupPath))
                    return null;

                // Zoek in de parent directories van de backup map
                var searchPaths = new List<string>();
                
                var parent = Directory.GetParent(backupPath);
                while (parent != null)
                {
                    searchPaths.Add(parent.FullName);
                    parent = Directory.GetParent(parent.FullName);
                    
                    // Maximaal 3 niveaus omhoog
                    if (searchPaths.Count >= 3) break;
                }
                
                // Zoek ook in dezelfde map als de catalogus
                foreach (var searchPath in searchPaths)
                {
                    try
                    {
                        var oldCatalogsPath = Path.Combine(searchPath, "Old Lightroom Catalogs");
                        if (Directory.Exists(oldCatalogsPath))
                        {
                            var info = AnalyzeOldCatalogsFolder(oldCatalogsPath);
                            if (info != null)
                                return info;
                        }
                    }
                    catch
                    {
                        // Toegangsfout negeren
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Analyseer de "Old Lightroom Catalogs" map
        /// </summary>
        private static OldCatalogsInfo? AnalyzeOldCatalogsFolder(string path)
        {
            try
            {
                var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                if (files.Length == 0)
                    return null;

                long totalSize = 0;
                DateTime oldestFile = DateTime.Now;
                int fileCount = 0;

                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        totalSize += fileInfo.Length;
                        fileCount++;
                        
                        if (fileInfo.LastWriteTime < oldestFile)
                            oldestFile = fileInfo.LastWriteTime;
                    }
                    catch
                    {
                        // Negeer individuele bestandsfouten
                    }
                }

                if (fileCount == 0)
                    return null;

                return new OldCatalogsInfo
                {
                    FolderPath = path,
                    TotalSize = totalSize,
                    FileCount = fileCount,
                    OldestFileDate = oldestFile,
                    AgeInDays = (int)(DateTime.Now - oldestFile).TotalDays
                };
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Informatie over "Old Lightroom Catalogs" map
    /// </summary>
    public class OldCatalogsInfo
    {
        public string FolderPath { get; set; } = "";
        public long TotalSize { get; set; }
        public int FileCount { get; set; }
        public DateTime OldestFileDate { get; set; }
        public int AgeInDays { get; set; }
        
        public bool IsOlderThanOneMonth => AgeInDays >= 30;
        
        public string SizeFormatted
        {
            get
            {
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                double len = TotalSize;
                int order = 0;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len /= 1024;
                }
                return $"{len:0.##} {sizes[order]}";
            }
        }
        
        public string AgeFormatted
        {
            get
            {
                if (AgeInDays < 7)
                    return AgeInDays == 1 ? "1 dag" : $"{AgeInDays} dagen";
                if (AgeInDays < 30)
                {
                    int weeks = AgeInDays / 7;
                    return weeks == 1 ? "1 week" : $"{weeks} weken";
                }
                if (AgeInDays < 365)
                {
                    int months = AgeInDays / 30;
                    return months == 1 ? "1 maand" : $"{months} maanden";
                }
                int years = AgeInDays / 365;
                return years == 1 ? "1 jaar" : $"{years} jaar";
            }
        }
    }
}

