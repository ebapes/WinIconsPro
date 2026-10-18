using System;
using System.Collections.Generic;
using System.IO;

namespace WinIconsPro.Core
{
    public class BackupEntry
    {
        public string FilePath { get; set; }
        public string OriginalIconPath { get; set; }
        public int OriginalIconIndex { get; set; }
        public string ItemType { get; set; }
        public bool IsConvertedLooseFile { get; set; }
    }

    public static class BackupManager
    {
        private static string GetBackupFilePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dir = Path.Combine(appData, "WinIconsPro");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, "backup_state.dat");
        }

        public static string GetCacheDir()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dir = Path.Combine(appData, "WinIconsPro", "Cache");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

        public static void SaveBackup(List<BackupEntry> entries)
        {
            string path = GetBackupFilePath();
            using (StreamWriter sw = new StreamWriter(path, false, System.Text.Encoding.UTF8))
            {
                foreach (BackupEntry e in entries)
                {
                    // Format: FilePath\tOriginalIconPath\tOriginalIconIndex\tItemType\tIsConvertedLooseFile
                    sw.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}",
                        e.FilePath,
                        e.OriginalIconPath ?? "",
                        e.OriginalIconIndex,
                        e.ItemType,
                        e.IsConvertedLooseFile));
                }
            }
        }

        public static List<BackupEntry> LoadBackup()
        {
            List<BackupEntry> list = new List<BackupEntry>();
            string path = GetBackupFilePath();
            if (!File.Exists(path)) return list;

            string[] lines = File.ReadAllLines(path, System.Text.Encoding.UTF8);
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] parts = line.Split('\t');
                if (parts.Length >= 5)
                {
                    BackupEntry e = new BackupEntry();
                    e.FilePath = parts[0];
                    e.OriginalIconPath = parts[1];
                    int idx;
                    int.TryParse(parts[2], out idx);
                    e.OriginalIconIndex = idx;
                    e.ItemType = parts[3];
                    bool converted;
                    bool.TryParse(parts[4], out converted);
                    e.IsConvertedLooseFile = converted;
                    list.Add(e);
                }
            }
            return list;
        }

        public static void ClearBackup()
        {
            string path = GetBackupFilePath();
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
