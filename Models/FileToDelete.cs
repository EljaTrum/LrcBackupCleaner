using System;

namespace BackupCleaner.Models
{
    public class FileToDelete
    {
        public string CustomerName { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime BackupDate { get; set; }
        public long Size { get; set; }
        
        public string SizeFormatted => FormatBytes(Size);
        public string BackupDateFormatted => BackupDate.ToString("dd-MM-yyyy HH:mm");

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}

