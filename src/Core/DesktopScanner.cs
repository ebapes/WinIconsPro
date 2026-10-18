using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using WinIconsPro.Native;

namespace WinIconsPro.Core
{
    public enum DesktopItemType
    {
        Shortcut,
        Folder,
        File
    }

    public class DesktopItem
    {
        public string FilePath { get; set; }
        public string DisplayName { get; set; }
        public DesktopItemType ItemType { get; set; }
        public string TargetPath { get; set; }
        public string OriginalIconPath { get; set; }
        public int OriginalIconIndex { get; set; }
        public bool IsPublicDesktop { get; set; }
        public Bitmap OriginalIcon { get; set; }
        public Bitmap ProcessedIcon { get; set; }

        public bool IsCustomized { get; set; }
    }

    public static class DesktopScanner
    {
        public static List<DesktopItem> ScanDesktopItems()
        {
            List<DesktopItem> items = new List<DesktopItem>();

            string userDesktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string publicDesktop = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);

            ScanDirectory(userDesktop, false, items);
            if (Directory.Exists(publicDesktop))
            {
                ScanDirectory(publicDesktop, true, items);
            }

            return items;
        }

        private static void ScanDirectory(string dirPath, bool isPublic, List<DesktopItem> items)
        {
            if (!Directory.Exists(dirPath)) return;

            DirectoryInfo dir = new DirectoryInfo(dirPath);

            // 1. Process Shortcuts (.lnk)
            FileInfo[] lnkFiles = dir.GetFiles("*.lnk");
            foreach (FileInfo file in lnkFiles)
            {
                if (file.Name.IndexOf("WinIconsPro", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                try
                {
                    DesktopItem item = ProcessShortcut(file.FullName, isPublic);
                    if (item != null) items.Add(item);
                }
                catch { }
            }

            // 2. Process Folders
            DirectoryInfo[] subDirs = dir.GetDirectories();
            foreach (DirectoryInfo subDir in subDirs)
            {
                // Skip hidden/system folders
                if ((subDir.Attributes & FileAttributes.Hidden) != 0 || (subDir.Attributes & FileAttributes.System) != 0)
                    continue;

                try
                {
                    DesktopItem item = ProcessFolder(subDir.FullName, isPublic);
                    if (item != null) items.Add(item);
                }
                catch { }
            }

            // 3. Process regular loose files (e.g. .txt, .pdf) - excluding desktop.ini
            FileInfo[] allFiles = dir.GetFiles();
            foreach (FileInfo file in allFiles)
            {
                if (file.Extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase)) continue;
                if (file.Name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)) continue;
                if (file.Name.IndexOf("WinIconsPro", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if ((file.Attributes & FileAttributes.Hidden) != 0) continue;

                try
                {
                    DesktopItem item = ProcessLooseFile(file.FullName, isPublic);
                    if (item != null) items.Add(item);
                }
                catch { }
            }
        }

        private static DesktopItem ProcessShortcut(string lnkPath, bool isPublic)
        {
            DesktopItem item = new DesktopItem();
            item.FilePath = lnkPath;
            item.DisplayName = Path.GetFileNameWithoutExtension(lnkPath);
            item.ItemType = DesktopItemType.Shortcut;
            item.IsPublicDesktop = isPublic;

            IShellLinkW link = (IShellLinkW)new ShellLinkClass();
            IPersistFile persistFile = (IPersistFile)link;

            try
            {
                persistFile.Load(lnkPath, 0);

                StringBuilder targetSb = new StringBuilder(260);
                link.GetPath(targetSb, 260, IntPtr.Zero, 0);
                item.TargetPath = targetSb.ToString();

                StringBuilder iconSb = new StringBuilder(260);
                int iconIndex = 0;
                link.GetIconLocation(iconSb, 260, out iconIndex);
                string loc = iconSb.ToString();

                // Check if current icon path points to WinIconsPro cache
                if (!string.IsNullOrEmpty(loc) &&
                    loc.IndexOf("WinIconsPro", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    item.IsCustomized = true;
                    // Do NOT use the cache path as original icon! Use target executable path instead.
                    item.OriginalIconPath = item.TargetPath;
                    item.OriginalIconIndex = 0;
                }
                else
                {
                    item.OriginalIconPath = loc;
                    item.OriginalIconIndex = iconIndex;
                }
            }
            catch { }

            item.OriginalIcon = ExtractJumboIcon(lnkPath, item.TargetPath);
            return item;
        }

        private static DesktopItem ProcessFolder(string folderPath, bool isPublic)
        {
            DesktopItem item = new DesktopItem();
            item.FilePath = folderPath;
            item.DisplayName = Path.GetFileName(folderPath);
            item.ItemType = DesktopItemType.Folder;
            item.IsPublicDesktop = isPublic;
            item.OriginalIcon = ExtractJumboIcon(folderPath, null);
            return item;
        }

        private static DesktopItem ProcessLooseFile(string filePath, bool isPublic)
        {
            DesktopItem item = new DesktopItem();
            item.FilePath = filePath;
            item.DisplayName = Path.GetFileName(filePath);
            item.ItemType = DesktopItemType.File;
            item.IsPublicDesktop = isPublic;
            item.OriginalIcon = ExtractJumboIcon(filePath, null);
            return item;
        }

        public static Bitmap ExtractJumboIcon(string filePath, string targetPath)
        {
            // Try extracting directly from target executable first if available
            if (!string.IsNullOrEmpty(targetPath) && File.Exists(targetPath))
            {
                IntPtr[] phicon = new IntPtr[1];
                int[] piconid = new int[1];
                uint count = ShellApi.PrivateExtractIcons(targetPath, 0, 256, 256, phicon, piconid, 1, 0);
                if (count > 0 && phicon[0] != IntPtr.Zero)
                {
                    try
                    {
                        using (Icon ico = Icon.FromHandle(phicon[0]))
                        {
                            Bitmap bmp = ico.ToBitmap();
                            if (bmp.Width == 256 && bmp.Height == 256)
                            {
                                return bmp;
                            }
                            // If smaller, resize to 256
                            Bitmap scaled = ResizeTo256(bmp);
                            bmp.Dispose();
                            return scaled;
                        }
                    }
                    finally
                    {
                        ShellApi.DestroyIcon(phicon[0]);
                    }
                }
            }

            // Fallback to Shell Jumbo Image List (covers 100% of all files/folders/links)
            try
            {
                ShellApi.SHFILEINFO sfi = new ShellApi.SHFILEINFO();
                IntPtr res = ShellApi.SHGetFileInfo(filePath, 0, ref sfi, (uint)Marshal.SizeOf(sfi), ShellApi.SHGFI_SYSICONINDEX);
                if (res != IntPtr.Zero)
                {
                    Guid iid = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950"); // IImageList GUID
                    IImageList iml;
                    int hr = ShellApi.SHGetImageList(ShellApi.SHIL_JUMBO, ref iid, out iml);
                    if (hr == 0 && iml != null)
                    {
                        IntPtr hIcon;
                        hr = iml.GetIcon(sfi.iIcon, ShellApi.ILD_TRANSPARENT, out hIcon);
                        if (hr == 0 && hIcon != IntPtr.Zero)
                        {
                            try
                            {
                                using (Icon ico = Icon.FromHandle(hIcon))
                                {
                                    Bitmap bmp = ico.ToBitmap();
                                    if (bmp.Width == 256 && bmp.Height == 256)
                                    {
                                        return bmp;
                                    }
                                    Bitmap scaled = ResizeTo256(bmp);
                                    bmp.Dispose();
                                    return scaled;
                                }
                            }
                            finally
                            {
                                ShellApi.DestroyIcon(hIcon);
                            }
                        }
                    }
                }
            }
            catch { }

            // Last fallback: generic 256x256 blank
            Bitmap blank = new Bitmap(256, 256, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(blank))
            {
                g.Clear(Color.Transparent);
            }
            return blank;
        }

        private static Bitmap ResizeTo256(Bitmap src)
        {
            Bitmap dst = new Bitmap(256, 256, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(dst))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.DrawImage(src, new Rectangle(0, 0, 256, 256));
            }
            return dst;
        }
    }
}
