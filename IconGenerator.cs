using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;

namespace BackupCleaner
{
    public static class IconGenerator
    {
        public static Icon CreateAppIcon()
        {
            return CreateAppIconAtSize(256);
        }
        
        private static Icon CreateAppIconAtSize(int size)
        {
            using var bitmap = CreateAppBitmap(size);
            return Icon.FromHandle(bitmap.GetHicon());
        }
        
        private static Bitmap CreateAppBitmap(int size)
        {
            var bitmap = new Bitmap(size, size);
            using var g = Graphics.FromImage(bitmap);
            
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            
            // Schaal factor
            float scale = size / 256f;
            
            // Achtergrond - Lightroom-stijl donker met accent kleur
            using var bgBrush = new SolidBrush(Color.FromArgb(26, 26, 26)); // #1A1A1A
            var bgRect = new Rectangle(
                (int)(16 * scale), 
                (int)(16 * scale), 
                (int)(224 * scale), 
                (int)(224 * scale)
            );
            int radius = (int)(40 * scale);
            FillRoundedRectangle(g, bgBrush, bgRect, radius);
            
            // Accent rand
            using var accentPen = new Pen(Color.FromArgb(14, 165, 233), Math.Max(1, (int)(4 * scale))); // #0EA5E9
            DrawRoundedRectangle(g, accentPen, bgRect, radius);
            
            // "Lrc" tekst in Lightroom Classic stijl
            using var textBrush = new SolidBrush(Color.FromArgb(14, 165, 233)); // #0EA5E9
            using var font = new Font("Segoe UI", Math.Max(8, 56 * scale), FontStyle.Bold);
            
            var textRect = new RectangleF(20 * scale, 55 * scale, 216 * scale, 120 * scale);
            var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            
            g.DrawString("Lrc", font, textBrush, textRect, sf);
            
            // Vinkje symbool rechtsonder
            using var checkPen = new Pen(Color.FromArgb(34, 197, 94), Math.Max(1, (int)(8 * scale))) 
            { 
                StartCap = LineCap.Round, 
                EndCap = LineCap.Round 
            };
            g.DrawLine(checkPen, 150 * scale, 185 * scale, 175 * scale, 210 * scale);
            g.DrawLine(checkPen, 175 * scale, 210 * scale, 215 * scale, 165 * scale);
            
            return bitmap;
        }

        /// <summary>
        /// Maakt een correct ICO bestand met meerdere resoluties voor Windows Verkenner
        /// </summary>
        public static void SaveIconToFile(string path)
        {
            var sizes = new[] { 16, 24, 32, 48, 64, 128, 256 };
            var images = new List<Bitmap>();
            
            foreach (var size in sizes)
            {
                images.Add(CreateAppBitmap(size));
            }
            
            try
            {
                using var fs = new FileStream(path, FileMode.Create);
                WriteIcoFile(fs, images);
            }
            finally
            {
                foreach (var img in images)
                {
                    img.Dispose();
                }
            }
        }
        
        private static void WriteIcoFile(Stream stream, List<Bitmap> images)
        {
            using var writer = new BinaryWriter(stream);
            
            // ICO Header
            writer.Write((short)0);              // Reserved
            writer.Write((short)1);              // Type: 1 = ICO
            writer.Write((short)images.Count);   // Number of images
            
            // Bereken offset voor image data
            int offset = 6 + (images.Count * 16); // Header + directory entries
            var imageDataList = new List<byte[]>();
            
            // Schrijf directory entries
            foreach (var bitmap in images)
            {
                using var ms = new MemoryStream();
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                var imageData = ms.ToArray();
                imageDataList.Add(imageData);
                
                writer.Write((byte)(bitmap.Width >= 256 ? 0 : bitmap.Width));   // Width
                writer.Write((byte)(bitmap.Height >= 256 ? 0 : bitmap.Height)); // Height
                writer.Write((byte)0);          // Color palette
                writer.Write((byte)0);          // Reserved
                writer.Write((short)1);         // Color planes
                writer.Write((short)32);        // Bits per pixel
                writer.Write(imageData.Length); // Image size
                writer.Write(offset);           // Offset
                
                offset += imageData.Length;
            }
            
            // Schrijf image data
            foreach (var imageData in imageDataList)
            {
                writer.Write(imageData);
            }
        }

        private static void FillRoundedRectangle(Graphics g, Brush brush, Rectangle rect, int radius)
        {
            using var path = CreateRoundedRectanglePath(rect, radius);
            g.FillPath(brush, path);
        }

        private static void DrawRoundedRectangle(Graphics g, Pen pen, Rectangle rect, int radius)
        {
            using var path = CreateRoundedRectanglePath(rect, radius);
            g.DrawPath(pen, path);
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
