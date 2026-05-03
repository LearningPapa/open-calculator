using Avalonia;

namespace ScientificCalculator;

// Avalonia requires an explicit entry point — WPF generated this automatically.
// This is the only new file you need to add.
class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()   // auto-selects Windows/Linux/macOS renderer
            .LogToTrace();
}
