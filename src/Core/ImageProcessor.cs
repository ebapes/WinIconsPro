using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WinIconsPro.Core
{
    public enum MonochromeMode
    {
        DarkTitanium = 0,   // Deep Titanium & Platinum
        PureBlackWhite = 1, // Pure High-Contrast B&W
        Inverted = 2,       // Negative / Inverted
        ColorTint = 3       // Modern Duotone Tint
    }

    public class ProcessingOptions
    {
        public MonochromeMode Mode { get; set; }
        public bool UseSquircle { get; set; }
        public float Contrast { get; set; } // 0.5 to 2.0 (default 1.35)
        public float Brightness { get; set; } // -0.5 to 0.5 (default 0.0)
        public Color TintColor { get; set; } // Custom Accent Tint Color

        public ProcessingOptions()
        {
            Mode = MonochromeMode.DarkTitanium;
            UseSquircle = false;
            Contrast = 1.35f;
            Brightness = 0.0f;
            TintColor = Color.FromArgb(10, 132, 255); // Electric Blue
        }
    }

    public static class ImageProcessor
    {
        public static Bitmap ProcessIcon(Bitmap source, ProcessingOptions options)
        {
            if (source == null) return null;

            Bitmap canvas = new Bitmap(256, 256, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(canvas))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                if (options.UseSquircle)
                {
                    // Render dark squircle container with frosted depth
                    using (GraphicsPath path = CreateSquirclePath(new Rectangle(6, 6, 244, 244), 54))
                    {
                        // Background gradient / subtle depth
                        using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                            new Point(0, 6), new Point(0, 250),
                            Color.FromArgb(245, 28, 28, 32),
                            Color.FromArgb(245, 14, 14, 16)))
                        {
                            g.FillPath(bgBrush, path);
                        }

                        // Subtle inner border (hairline highlight)
                        using (Pen borderPen = new Pen(Color.FromArgb(80, 255, 255, 255), 1.5f))
                        {
                            g.DrawPath(borderPen, path);
                        }
                    }
                }

                // Process the pixel colors
                Bitmap processedGlyph = TransformPixels(source, options);

                if (options.UseSquircle)
                {
                    // Scale glyph into center of the squircle
                    int glyphSize = 160;
                    int offset = (256 - glyphSize) / 2;
                    g.DrawImage(processedGlyph, new Rectangle(offset, offset, glyphSize, glyphSize));
                }
                else
                {
                    g.DrawImage(processedGlyph, new Rectangle(0, 0, 256, 256));
                }

                processedGlyph.Dispose();
            }

            return canvas;
        }

        private static Bitmap TransformPixels(Bitmap src, ProcessingOptions options)
        {
            Bitmap dst = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
            BitmapData srcData = src.LockBits(new Rectangle(0, 0, src.Width, src.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData dstData = dst.LockBits(new Rectangle(0, 0, dst.Width, dst.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            int bytes = src.Width * src.Height * 4;
            byte[] buffer = new byte[bytes];
            Marshal.Copy(srcData.Scan0, buffer, 0, bytes);

            float contrast = options.Contrast;
            float brightness = options.Brightness;

            for (int i = 0; i < bytes; i += 4)
            {
                byte b = buffer[i];
                byte g = buffer[i + 1];
                byte r = buffer[i + 2];
                byte a = buffer[i + 3];

                if (a < 5) continue; // Skip near-transparent pixels

                if (options.Mode == MonochromeMode.Inverted)
                {
                    buffer[i] = (byte)(255 - b);
                    buffer[i + 1] = (byte)(255 - g);
                    buffer[i + 2] = (byte)(255 - r);
                    continue;
                }

                // Calculate perceptual luminance
                float lum = (0.299f * r + 0.587f * g + 0.114f * b) / 255.0f;

                // Apply brightness & contrast
                lum = (lum - 0.5f) * contrast + 0.5f + brightness;
                if (lum < 0f) lum = 0f;
                if (lum > 1f) lum = 1f;

                if (options.Mode == MonochromeMode.DarkTitanium)
                {
                    // Dark Titanium & Platinum styling:
                    // Dark tones map to #18181C (deep titanium slate)
                    // Highlights map to #F8F8FA (crisp platinum)
                    byte rOut = (byte)(24 + lum * (248 - 24));
                    byte gOut = (byte)(24 + lum * (248 - 24));
                    byte bOut = (byte)(28 + lum * (252 - 28));

                    buffer[i] = bOut;
                    buffer[i + 1] = gOut;
                    buffer[i + 2] = rOut;
                }
                else if (options.Mode == MonochromeMode.PureBlackWhite)
                {
                    byte v = (byte)(lum * 255.0f);
                    buffer[i] = v;
                    buffer[i + 1] = v;
                    buffer[i + 2] = v;
                }
                else if (options.Mode == MonochromeMode.ColorTint)
                {
                    Color tint = options.TintColor;
                    float tr = tint.R / 255.0f;
                    float tg = tint.G / 255.0f;
                    float tb = tint.B / 255.0f;

                    float rOut, gOut, bOut;
                    if (lum < 0.25f)
                    {
                        float t = lum / 0.25f;
                        // Blend from deep graphite #18181C (24, 24, 28) to dark tint
                        rOut = 24f * (1f - t) + (tr * 130f) * t;
                        gOut = 24f * (1f - t) + (tg * 130f) * t;
                        bOut = 28f * (1f - t) + (tb * 130f) * t;
                    }
                    else if (lum < 0.80f)
                    {
                        float t = (lum - 0.25f) / 0.55f;
                        rOut = (tr * 130f) * (1f - t) + (tr * 255f) * t;
                        gOut = (tg * 130f) * (1f - t) + (tg * 255f) * t;
                        bOut = (tb * 130f) * (1f - t) + (tb * 255f) * t;
                    }
                    else
                    {
                        float t = (lum - 0.80f) / 0.20f;
                        rOut = (tr * 255f) * (1f - t) + (tr * 180f + 75f) * t;
                        gOut = (tg * 255f) * (1f - t) + (tg * 180f + 75f) * t;
                        bOut = (tb * 255f) * (1f - t) + (tb * 180f + 75f) * t;
                    }

                    buffer[i] = (byte)Math.Min(255, Math.Max(0, (int)bOut));
                    buffer[i + 1] = (byte)Math.Min(255, Math.Max(0, (int)gOut));
                    buffer[i + 2] = (byte)Math.Min(255, Math.Max(0, (int)rOut));
                }
            }

            Marshal.Copy(buffer, 0, dstData.Scan0, bytes);
            src.UnlockBits(srcData);
            dst.UnlockBits(dstData);

            return dst;
        }

        public static GraphicsPath CreateSquirclePath(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

            // Top-left
            path.AddArc(arc, 180, 90);

            // Top-right
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // Bottom-right
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // Bottom-left
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
}
