using System;
using System.IO;
using SkiaSharp;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Media.Imaging;

namespace BackupCleaner;

/// <summary>
/// Cross-platform icon loader using SkiaSharp
/// </summary>
public static class IconGenerator
{
    private static SKBitmap? _sourceBitmap;
    
    /// <summary>
    /// Loads the source PNG icon from assets
    /// </summary>
    private static SKBitmap LoadSourceIcon()
    {
        if (_sourceBitmap != null)
            return _sourceBitmap;
        
        try
        {
            // Try to load from Avalonia resources (at runtime)
            try
            {
                var uri = new Uri("avares://LightroomBackupCleaner/Assets/lrcbackupcleaner.png");
                using var stream = AssetLoader.Open(uri);
                if (stream != null)
                {
                    _sourceBitmap = SKBitmap.Decode(stream);
                    return _sourceBitmap;
                }
            }
            catch
            {
                // Continue to file system fallback
            }
            
            // Fallback: try to load from file system (for development/build time)
            var pngPath = Path.Combine(AppContext.BaseDirectory, "Assets", "lrcbackupcleaner.png");
            if (File.Exists(pngPath))
            {
                using var stream = File.OpenRead(pngPath);
                _sourceBitmap = SKBitmap.Decode(stream);
                return _sourceBitmap;
            }
            
            // Fallback: try relative path
            pngPath = Path.Combine("Assets", "lrcbackupcleaner.png");
            if (File.Exists(pngPath))
            {
                using var stream = File.OpenRead(pngPath);
                _sourceBitmap = SKBitmap.Decode(stream);
                return _sourceBitmap;
            }
        }
        catch
        {
            // If loading fails, fall back to generated icon
        }
        
        // Fallback: generate icon programmatically if PNG not found
        return CreateGeneratedIcon(256);
    }
    
    /// <summary>
    /// Creates the app icon as a bitmap from the loaded PNG, resized to the requested size
    /// </summary>
    public static SKBitmap CreateAppBitmap(int size = 256)
    {
        var source = LoadSourceIcon();
        
        // If source is already the right size, return it
        if (source.Width == size && source.Height == size)
            return source.Copy();
        
        // Resize the source bitmap to the requested size
        var resized = source.Resize(new SKImageInfo(size, size), SKFilterQuality.High);
        return resized;
    }
    
    /// <summary>
    /// Creates a programmatically generated icon (fallback)
    /// </summary>
    private static SKBitmap CreateGeneratedIcon(int size = 256)
    {
        var bitmap = new SKBitmap(size, size);
        using var canvas = new SKCanvas(bitmap);
        
        canvas.Clear(SKColors.Transparent);
        
        float scale = size / 256f;
        
        // Achtergrond - Lightroom-stijl donker met accent kleur
        using var bgPaint = new SKPaint
        {
            Color = new SKColor(26, 26, 26), // #1A1A1A
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        
        var bgRect = new SKRect(16 * scale, 16 * scale, 240 * scale, 240 * scale);
        canvas.DrawRoundRect(bgRect, 40 * scale, 40 * scale, bgPaint);
        
        // Accent rand
        using var accentPaint = new SKPaint
        {
            Color = new SKColor(14, 165, 233), // #0EA5E9
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 4 * scale
        };
        canvas.DrawRoundRect(bgRect, 40 * scale, 40 * scale, accentPaint);
        
        // "Lrc" tekst
        using var textPaint = new SKPaint
        {
            Color = new SKColor(14, 165, 233), // #0EA5E9
            IsAntialias = true,
            TextSize = 56 * scale,
            Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold),
            TextAlign = SKTextAlign.Center
        };
        
        canvas.DrawText("Lrc", 128 * scale, 145 * scale, textPaint);
        
        // Vinkje symbool rechtsonder
        using var checkPaint = new SKPaint
        {
            Color = new SKColor(34, 197, 94), // #22C55E
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 8 * scale,
            StrokeCap = SKStrokeCap.Round
        };
        
        using var path = new SKPath();
        path.MoveTo(150 * scale, 185 * scale);
        path.LineTo(175 * scale, 210 * scale);
        path.LineTo(215 * scale, 165 * scale);
        canvas.DrawPath(path, checkPaint);
        
        return bitmap;
    }

    /// <summary>
    /// Creates an Avalonia WindowIcon from the loaded PNG
    /// </summary>
    public static WindowIcon? CreateWindowIcon()
    {
        try
        {
            using var bitmap = CreateAppBitmap(256);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = new MemoryStream(data.ToArray());
            return new WindowIcon(stream);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates an Avalonia Bitmap from the loaded PNG for use in Image controls
    /// </summary>
    public static Bitmap? CreateBitmap(int size = 256)
    {
        try
        {
            using var skBitmap = CreateAppBitmap(size);
            using var skImage = SKImage.FromBitmap(skBitmap);
            using var data = skImage.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = new MemoryStream(data.ToArray());
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Saves the icon as a PNG file
    /// </summary>
    public static void SaveIconAsPng(string path, int size = 256)
    {
        using var bitmap = CreateAppBitmap(size);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(path);
        data.SaveTo(stream);
    }

    /// <summary>
    /// Saves the icon as a multi-resolution ICO file (Windows only)
    /// </summary>
    public static void SaveIconToFile(string path)
    {
        var sizes = new[] { 16, 24, 32, 48, 64, 128, 256 };
        
        using var fs = new FileStream(path, FileMode.Create);
        using var writer = new BinaryWriter(fs);
        
        // ICO Header
        writer.Write((short)0);              // Reserved
        writer.Write((short)1);              // Type: 1 = ICO
        writer.Write((short)sizes.Length);   // Number of images
        
        // Calculate offset for image data
        int offset = 6 + (sizes.Length * 16); // Header + directory entries
        var imageDataList = new System.Collections.Generic.List<byte[]>();
        
        // Create images and write directory entries
        foreach (var size in sizes)
        {
            using var bitmap = CreateAppBitmap(size);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            var imageData = data.ToArray();
            imageDataList.Add(imageData);
            
            writer.Write((byte)(size >= 256 ? 0 : size));   // Width
            writer.Write((byte)(size >= 256 ? 0 : size));   // Height
            writer.Write((byte)0);          // Color palette
            writer.Write((byte)0);          // Reserved
            writer.Write((short)1);         // Color planes
            writer.Write((short)32);        // Bits per pixel
            writer.Write(imageData.Length); // Image size
            writer.Write(offset);           // Offset
            
            offset += imageData.Length;
        }
        
        // Write image data
        foreach (var imageData in imageDataList)
        {
            writer.Write(imageData);
        }
    }
}
