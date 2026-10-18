using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using WinIconsPro.Native;

namespace WinIconsPro.Core
{
    public static class IconPatcher
    {
        public static void ApplyMonochrome(List<DesktopItem> items, ProcessingOptions options, Action<string, int, int> progressCallback)
        {
            string cacheDir = BackupManager.GetCacheDir();
            List<BackupEntry> backupList = BackupManager.LoadBackup();
            Dictionary<string, BackupEntry> existingBackups = new Dictionary<string, BackupEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (BackupEntry b in backupList)
            {
                existingBackups[b.FilePath] = b;
            }

            int total = items.Count;
            for (int i = 0; i < total; i++)
            {
                DesktopItem item = items[i];
                if (progressCallback != null)
                {
                    progressCallback(item.DisplayName, i + 1, total);
                }

                try
                {
                    // Generate unique cache file name
                    string hash = GetHash(item.FilePath);
                    string icoFileName = string.Format("{0}_{1}_{2}_{3}.ico",
                        SanitizeFileName(item.DisplayName),
                        options.Mode,
                        options.Mode == MonochromeMode.ColorTint ? (options.TintColor.ToArgb() & 0xFFFFFF).ToString("X6") : "none",
                        options.UseSquircle ? "squircle" : "plain");
                    string icoPath = Path.Combine(cacheDir, icoFileName);

                    // Generate processed icon
                    using (Bitmap processedBmp = ImageProcessor.ProcessIcon(item.OriginalIcon, options))
                    {
                        IcoBuilder.CreateIcoFrom256(processedBmp, icoPath);
                    }

                    if (item.ItemType == DesktopItemType.Shortcut)
                    {
                        // Save backup if not already saved
                        if (!existingBackups.ContainsKey(item.FilePath))
                        {
                            BackupEntry be = new BackupEntry
                            {
                                FilePath = item.FilePath,
                                OriginalIconPath = item.OriginalIconPath,
                                OriginalIconIndex = item.OriginalIconIndex,
                                ItemType = "Shortcut",
                                IsConvertedLooseFile = false
                            };
                            backupList.Add(be);
                            existingBackups[item.FilePath] = be;
                        }

                        // Apply new icon to shortcut
                        ApplyToShortcut(item.FilePath, icoPath);
                    }
                    else if (item.ItemType == DesktopItemType.Folder)
                    {
                        if (!existingBackups.ContainsKey(item.FilePath))
                        {
                            BackupEntry be = new BackupEntry
                            {
                                FilePath = item.FilePath,
                                OriginalIconPath = "",
                                OriginalIconIndex = 0,
                                ItemType = "Folder",
                                IsConvertedLooseFile = false
                            };
                            backupList.Add(be);
                            existingBackups[item.FilePath] = be;
                        }

                        ApplyToFolder(item.FilePath, icoPath);
                    }
                    else if (item.ItemType == DesktopItemType.File)
                    {
                        // Loose file: create a shortcut pointing to it, style the shortcut, hide original
                        string lnkPath = item.FilePath + ".lnk";
                        if (!existingBackups.ContainsKey(item.FilePath))
                        {
                            BackupEntry be = new BackupEntry
                            {
                                FilePath = item.FilePath,
                                OriginalIconPath = lnkPath,
                                OriginalIconIndex = 0,
                                ItemType = "File",
                                IsConvertedLooseFile = true
                            };
                            backupList.Add(be);
                            existingBackups[item.FilePath] = be;
                        }

                        CreateAndStyleShortcut(lnkPath, item.FilePath, icoPath);
                        try
                        {
                            File.SetAttributes(item.FilePath, File.GetAttributes(item.FilePath) | FileAttributes.Hidden);
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    // If write permission denied, rethrow so caller can prompt for Elevation
                    if (ex is UnauthorizedAccessException)
                    {
                        throw;
                    }
                }
            }

            BackupManager.SaveBackup(backupList);
            RefreshDesktop();
        }

        public static void RestoreOriginals(Action<string, int, int> progressCallback)
        {
            // 1. Process explicit backup list if available
            List<BackupEntry> backupList = BackupManager.LoadBackup();
            int total = backupList.Count;

            for (int i = 0; i < total; i++)
            {
                BackupEntry be = backupList[i];
                if (progressCallback != null)
                {
                    progressCallback(Path.GetFileName(be.FilePath), i + 1, total);
                }

                try
                {
                    if (be.ItemType == "Shortcut")
                    {
                        if (File.Exists(be.FilePath))
                        {
                            IShellLinkW link = (IShellLinkW)new ShellLinkClass();
                            IPersistFile pf = (IPersistFile)link;
                            pf.Load(be.FilePath, 0);

                            string restoreIcon = be.OriginalIconPath;
                            if (string.IsNullOrEmpty(restoreIcon) || restoreIcon.IndexOf("WinIconsPro", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                StringBuilder targetSb = new StringBuilder(260);
                                link.GetPath(targetSb, 260, IntPtr.Zero, 0);
                                restoreIcon = targetSb.ToString();
                            }
                            link.SetIconLocation(restoreIcon, be.OriginalIconIndex);
                            pf.Save(be.FilePath, true);
                        }
                    }
                    else if (be.ItemType == "Folder")
                    {
                        string iniPath = Path.Combine(be.FilePath, "desktop.ini");
                        if (File.Exists(iniPath))
                        {
                            try
                            {
                                File.SetAttributes(iniPath, FileAttributes.Normal);
                                File.Delete(iniPath);
                            }
                            catch { }
                        }
                    }
                    else if (be.ItemType == "File" && be.IsConvertedLooseFile)
                    {
                        if (File.Exists(be.FilePath))
                        {
                            try
                            {
                                FileAttributes attr = File.GetAttributes(be.FilePath);
                                File.SetAttributes(be.FilePath, attr & ~FileAttributes.Hidden);
                            }
                            catch { }
                        }

                        string lnkPath = be.OriginalIconPath;
                        if (File.Exists(lnkPath))
                        {
                            try { File.Delete(lnkPath); } catch { }
                        }
                    }
                }
                catch { }
            }

            // 2. Comprehensive Self-Healing Sweep of User Desktop and Public Desktop
            string[] desktopDirs = new string[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)
            };

            foreach (string dir in desktopDirs)
            {
                if (!Directory.Exists(dir)) continue;

                // Check all shortcuts
                string[] lnkFiles = Directory.GetFiles(dir, "*.lnk");
                foreach (string lnk in lnkFiles)
                {
                    try
                    {
                        // Converted loose files (e.g. Pass.txt.lnk, WinIconsPro.exe.lnk)
                        if (lnk.EndsWith(".txt.lnk", StringComparison.OrdinalIgnoreCase) ||
                            lnk.EndsWith(".pdf.lnk", StringComparison.OrdinalIgnoreCase) ||
                            lnk.EndsWith(".docx.lnk", StringComparison.OrdinalIgnoreCase) ||
                            lnk.EndsWith(".exe.lnk", StringComparison.OrdinalIgnoreCase))
                        {
                            string origFile = lnk.Substring(0, lnk.Length - 4);
                            if (File.Exists(origFile))
                            {
                                FileAttributes attr = File.GetAttributes(origFile);
                                File.SetAttributes(origFile, attr & ~FileAttributes.Hidden);
                                File.Delete(lnk);
                                continue;
                            }
                        }

                        IShellLinkW link = (IShellLinkW)new ShellLinkClass();
                        IPersistFile pf = (IPersistFile)link;
                        pf.Load(lnk, 0);

                        StringBuilder iconSb = new StringBuilder(260);
                        int iconIdx;
                        link.GetIconLocation(iconSb, 260, out iconIdx);
                        string currentIcon = iconSb.ToString();

                        if (!string.IsNullOrEmpty(currentIcon) &&
                            currentIcon.IndexOf("WinIconsPro", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            StringBuilder targetSb = new StringBuilder(260);
                            link.GetPath(targetSb, 260, IntPtr.Zero, 0);
                            string target = targetSb.ToString();

                            link.SetIconLocation(target, 0);
                            pf.Save(lnk, true);
                        }
                    }
                    catch { }
                }

                // Check all folders for WinIconsPro desktop.ini
                string[] subDirs = Directory.GetDirectories(dir);
                foreach (string subDir in subDirs)
                {
                    try
                    {
                        string ini = Path.Combine(subDir, "desktop.ini");
                        if (File.Exists(ini))
                        {
                            string content = File.ReadAllText(ini);
                            if (content.IndexOf("WinIconsPro", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                File.SetAttributes(ini, FileAttributes.Normal);
                                File.Delete(ini);
                            }
                        }
                    }
                    catch { }
                }
            }

            // 3. Clear backup & purge icon cache
            BackupManager.ClearBackup();
            try
            {
                string cache = BackupManager.GetCacheDir();
                if (Directory.Exists(cache))
                {
                    foreach (string f in Directory.GetFiles(cache, "*.ico"))
                    {
                        try { File.Delete(f); } catch { }
                    }
                }
            }
            catch { }

            RefreshDesktop();
        }

        private static void ApplyToShortcut(string lnkPath, string icoPath)
        {
            IShellLinkW link = (IShellLinkW)new ShellLinkClass();
            IPersistFile pf = (IPersistFile)link;
            pf.Load(lnkPath, 0);
            link.SetIconLocation(icoPath, 0);
            pf.Save(lnkPath, true);
        }

        private static void CreateAndStyleShortcut(string lnkPath, string targetPath, string icoPath)
        {
            IShellLinkW link = (IShellLinkW)new ShellLinkClass();
            IPersistFile pf = (IPersistFile)link;
            link.SetPath(targetPath);
            link.SetIconLocation(icoPath, 0);
            pf.Save(lnkPath, true);
        }

        private static void ApplyToFolder(string folderPath, string icoPath)
        {
            string iniPath = Path.Combine(folderPath, "desktop.ini");
            try
            {
                if (File.Exists(iniPath))
                {
                    File.SetAttributes(iniPath, FileAttributes.Normal);
                }

                string iniContent = string.Format("[.ShellClassInfo]\r\nIconResource={0},0\r\n[ViewState]\r\nMode=\r\nVid=\r\nFolderType=Generic\r\n", icoPath);
                File.WriteAllText(iniPath, iniContent, Encoding.Default);

                File.SetAttributes(iniPath, FileAttributes.Hidden | FileAttributes.System);
                // Folder itself must have ReadOnly or System attribute for desktop.ini to be respected by Windows Explorer
                File.SetAttributes(folderPath, File.GetAttributes(folderPath) | FileAttributes.ReadOnly);
            }
            catch { }
        }

        public static void RefreshDesktop()
        {
            ShellApi.SHChangeNotify(ShellApi.SHCNE_ASSOCCHANGED, ShellApi.SHCNF_FLUSHNOWAIT, IntPtr.Zero, IntPtr.Zero);
        }

        private static string GetHash(string text)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(text));
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < 4; i++) sb.Append(bytes[i].ToString("X2"));
                return sb.ToString();
            }
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }
    }
}
