using System;
using System.IO;
using Avalonia;

namespace BackupCleaner;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Check if we need to generate ICO file
        if (args.Length > 0 && args[0] == "generate-ico")
        {
            var pngPath = Path.Combine("Assets", "lrcbackupcleaner.png");
            var icoPath = Path.Combine("Assets", "app.ico");
            
            if (!File.Exists(pngPath))
            {
                Console.WriteLine($"PNG not found: {pngPath}");
                return;
            }
            
            Console.WriteLine($"Generating {icoPath} from {pngPath}...");
            IconGenerator.SaveIconToFile(icoPath);
            Console.WriteLine("Done!");
            return;
        }
        
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}

