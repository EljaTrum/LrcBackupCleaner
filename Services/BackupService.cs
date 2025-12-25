using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BackupCleaner.Models;

namespace BackupCleaner.Services
{
    public static class BackupService
    {
        // Lightroom backup bestands extensies (catalogus of gezipt)
        private static readonly string[] BackupExtensions = { ".lrcat", ".zip" };

        /// <summary>
        /// Scan de backup map en vind alle Lightroom backup sets
        /// </summary>
        public static List<LightroomBackup> GetLightroomBackups(string backupPath)
        {
            var backups = new List<LightroomBackup>();

            if (!Directory.Exists(backupPath))
                return backups;

            try
            {
                // Lightroom maakt submappen met formaat "YYYY-MM-DD HHMM"
                var directories = Directory.GetDirectories(backupPath);

                foreach (var dir in directories)
                {
                    var folderName = Path.GetFileName(dir);
                    
                    // Check of dit een backup datum map is
                    if (!LightroomDetectionService.IsBackupDateFolder(folderName))
                        continue;

                    var backupDate = LightroomDetectionService.ParseBackupFolderDate(folderName);
                    if (!backupDate.HasValue)
                        continue;

                    // Zoek naar backup bestanden (.lrcat of .zip)
                    var backupFiles = GetBackupFiles(dir);
                    if (!backupFiles.Any())
                        continue;

                    // Bereken totale grootte van de backup map
                    var totalSize = GetDirectorySize(dir);
                    var primaryFile = backupFiles.First();

                    backups.Add(new LightroomBackup
                    {
                        FolderName = folderName,
                        FolderPath = dir,
                        BackupDate = backupDate.Value,
                        CatalogFileName = Path.GetFileName(primaryFile),
                        CatalogFilePath = primaryFile,
                        TotalSize = totalSize,
                        FileCount = Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Length
                    });
                }
            }
            catch
            {
                // Fout bij lezen - return lege lijst
            }

            return backups.OrderByDescending(b => b.BackupDate).ToList();
        }

        /// <summary>
        /// Zoek naar Lightroom backup bestanden in een map
        /// </summary>
        private static List<string> GetBackupFiles(string directory)
        {
            var files = new List<string>();
            
            try
            {
                foreach (var ext in BackupExtensions)
                {
                    files.AddRange(Directory.GetFiles(directory, $"*{ext}"));
                }
            }
            catch
            {
                // Negeer fouten
            }
            
            return files;
        }

        /// <summary>
        /// Bereken de grootte van een map inclusief alle bestanden
        /// </summary>
        private static long GetDirectorySize(string path)
        {
            try
            {
                return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                    .Sum(f => new FileInfo(f).Length);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Bepaal welke backups verwijderd moeten worden
        /// </summary>
        public static List<LightroomBackup> GetBackupsToDelete(
            List<LightroomBackup> allBackups, 
            int backupsToKeep, 
            int minimumAgeMonths)
        {
            var cutoffDate = DateTime.Today.AddMonths(-minimumAgeMonths);

            // Stap 1: De nieuwste X backups worden altijd bewaard
            var backupsToKeepList = allBackups.Take(backupsToKeep).ToList();
            
            // Stap 2: Van de rest, alleen backups die ouder zijn dan minimum leeftijd verwijderen
            var backupsToDelete = allBackups
                .Skip(backupsToKeep)
                .Where(b => b.BackupDate.Date < cutoffDate)
                .ToList();

            return backupsToDelete;
        }

        /// <summary>
        /// Verwijder backup mappen
        /// </summary>
        public static (int deletedBackups, long freedSpace, List<string> errors) DeleteBackups(List<LightroomBackup> backups)
        {
            int deletedCount = 0;
            long freedSpace = 0;
            var errors = new List<string>();

            foreach (var backup in backups)
            {
                try
                {
                    if (Directory.Exists(backup.FolderPath))
                    {
                        var size = backup.TotalSize;
                        Directory.Delete(backup.FolderPath, recursive: true);
                        deletedCount++;
                        freedSpace += size;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Kon backup '{backup.FolderName}' niet verwijderen: {ex.Message}");
                }
            }

            return (deletedCount, freedSpace, errors);
        }

        /// <summary>
        /// Converteer backups naar FileToDelete objecten voor de preview
        /// </summary>
        public static List<FileToDelete> ConvertToFilesToDelete(List<LightroomBackup> backups, string catalogName)
        {
            return backups.Select(b => new FileToDelete
            {
                CustomerName = catalogName,
                FileName = b.FolderName,
                FilePath = b.FolderPath,
                BackupDate = b.BackupDate,
                Size = b.TotalSize
            }).ToList();
        }
    }
}
