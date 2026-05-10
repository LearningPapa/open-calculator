using Avalonia;

namespace TIDestroyer9000;

// Avalonia requires an explicit entry point — WPF generated this automatically.
// This is the only new file you need to add.
class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
