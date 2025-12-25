using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BackupCleaner.Services
{
    /// <summary>
    /// Service voor het negeren van bepaalde mappen/bestanden (niet gebruikt voor Lightroom versie)
    /// Behouden voor backward compatibility
    /// </summary>
    public static class IgnoreService
    {
        private static readonly string IgnoreFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LightroomBackupCleaner",
            "ignore.txt"
        );

        private static List<string> _patterns = new();
        private static List<Regex> _regexPatterns = new();

        /// <summary>
        /// Laad ignore patterns uit het bestand
        /// </summary>
        public static List<string> Load()
        {
            try
            {
                if (!File.Exists(IgnoreFilePath))
                {
                    _patterns = new List<string>();
                    _regexPatterns = new List<Regex>();
                    return _patterns;
                }

                var lines = File.ReadAllLines(IgnoreFilePath)
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("#"))
                    .ToList();

                _patterns = lines;
                _regexPatterns = lines.Select(PatternToRegex).ToList();

                return _patterns;
            }
            catch
            {
                _patterns = new List<string>();
                _regexPatterns = new List<Regex>();
                return _patterns;
            }
        }

        /// <summary>
        /// Controleer of een mapnaam genegeerd moet worden
        /// </summary>
        public static bool ShouldIgnoreFolder(string folderName)
        {
            if (_patterns.Count == 0) return false;

            foreach (var regex in _regexPatterns)
            {
                if (regex.IsMatch(folderName))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Controleer of een bestandsnaam genegeerd moet worden
        /// </summary>
        public static bool ShouldIgnoreFile(string fileName)
        {
            if (_patterns.Count == 0) return false;

            foreach (var regex in _regexPatterns)
            {
                if (regex.IsMatch(fileName))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Geeft het pad naar het ignore bestand terug
        /// </summary>
        public static string GetIgnoreFilePath()
        {
            return IgnoreFilePath;
        }

        /// <summary>
        /// Converteer een simpel pattern naar een regex
        /// </summary>
        private static Regex PatternToRegex(string pattern)
        {
            var escaped = Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".");

            return new Regex($"^{escaped}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
    }
}
