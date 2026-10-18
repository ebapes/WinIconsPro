using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace WinIconsPro.Core
{
    public static class IcoBuilder
    {
        private static readonly int[] StandardSizes = new int[] { 256, 128, 64, 48, 32, 16 };

        public static void CreateIcoFrom256(Bitmap master256, string outputPath)
        {
            string dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            Bitmap[] bitmaps = new Bitmap[StandardSizes.Length];
            try
            {
                for (int i = 0; i < StandardSizes.Length; i++)
                {
                    int sz = StandardSizes[i];
                    if (sz == master256.Width && sz == master256.Height)
                    {
                        bitmaps[i] = new Bitmap(master256);
                    }
                    else
                    {
                        bitmaps[i] = ResizeImage(master256, sz, sz);
                    }
                }

                SaveBitmapsAsIco(bitmaps, outputPath);
            }
            finally
            {
                for (int i = 0; i < bitmaps.Length; i++)
                {
                    if (bitmaps[i] != null)
                    {
                        bitmaps[i].Dispose();
                    }
                }
            }
        }

        public static void SaveBitmapsAsIco(Bitmap[] bitmaps, string outputPath)
        {
            byte[][] pngData = new byte[bitmaps.Length][];
            for (int i = 0; i < bitmaps.Length; i++)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmaps[i].Save(ms, ImageFormat.Png);
                    pngData[i] = ms.ToArray();
                }
            }

            using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                // ICONDIR header
                bw.Write((short)0); // Reserved
                bw.Write((short)1); // Type 1 = Icon
                bw.Write((short)bitmaps.Length); // Image count

                int offset = 6 + (16 * bitmaps.Length);

                // ICONDIRENTRY array
                for (int i = 0; i < bitmaps.Length; i++)
                {
                    int w = bitmaps[i].Width;
                    int h = bitmaps[i].Height;

                    bw.Write((byte)(w >= 256 ? 0 : w));
                    bw.Write((byte)(h >= 256 ? 0 : h));
                    bw.Write((byte)0); // Color count (0 for 32bpp)
                    bw.Write((byte)0); // Reserved
                    bw.Write((short)1); // Planes
                    bw.Write((short)32); // Bits per pixel
                    bw.Write(pngData[i].Length); // Size of image data
                    bw.Write(offset); // Image offset in file

                    offset += pngData[i].Length;
                }

                // Write PNG image bytes
                for (int i = 0; i < bitmaps.Length; i++)
                {
                    bw.Write(pngData[i]);
                }
            }
        }

        private static Bitmap ResizeImage(Bitmap image, int width, int height)
        {
            Bitmap dest = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(dest))
            {
                g.CompositingMode = CompositingMode.SourceOver;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (ImageAttributes wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    g.DrawImage(image, new Rectangle(0, 0, width, height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }
            return dest;
        }
    }
}
