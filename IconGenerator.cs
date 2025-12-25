using System;
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
            // Maak een 256x256 bitmap
            using var bitmap = new Bitmap(256, 256);
            using var g = Graphics.FromImage(bitmap);
            
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            
            // Achtergrond - Lightroom-stijl donker met accent kleur
            var bgBrush = new SolidBrush(Color.FromArgb(26, 26, 26)); // #1A1A1A
            var bgRect = new Rectangle(16, 16, 224, 224);
            FillRoundedRectangle(g, bgBrush, bgRect, 40);
            
            // Accent rand
            using var accentPen = new Pen(Color.FromArgb(14, 165, 233), 4); // #0EA5E9
            DrawRoundedRectangle(g, accentPen, bgRect, 40);
            
            // "Lrc" tekst in Lightroom Classic stijl
            using var textBrush = new SolidBrush(Color.FromArgb(14, 165, 233)); // #0EA5E9
            using var font = new Font("Segoe UI", 56, FontStyle.Bold);
            
            var textRect = new RectangleF(20, 55, 216, 120);
            var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            
            g.DrawString("Lrc", font, textBrush, textRect, sf);
            
            // Kleine "backup" indicator (bezem/schoonmaak symbool)
            using var smallBrush = new SolidBrush(Color.FromArgb(34, 197, 94)); // Groen #22C55E
            using var smallFont = new Font("Segoe UI", 24, FontStyle.Bold);
            
            // Vinkje symbool rechtsonder
            using var checkPen = new Pen(Color.FromArgb(34, 197, 94), 8) 
            { 
                StartCap = LineCap.Round, 
                EndCap = LineCap.Round 
            };
            g.DrawLine(checkPen, 150, 185, 175, 210);
            g.DrawLine(checkPen, 175, 210, 215, 165);
            
            // Convert bitmap naar icon
            return Icon.FromHandle(bitmap.GetHicon());
        }

        public static void SaveIconToFile(string path)
        {
            using var icon = CreateAppIcon();
            using var fs = new FileStream(path, FileMode.Create);
            icon.Save(fs);
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
