using System;
using System.Collections.Generic;
using System.Linq;

namespace BackupCleaner.Models
{
    public class BackupSet
    {
        public DateTime Date { get; set; }
        public List<BackupFile> Files { get; set; } = new();
        public long TotalSize => Files.Sum(f => f.Size);
    }

    public class BackupFile
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime DateModified { get; set; }
    }
}

