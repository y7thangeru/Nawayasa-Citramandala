using System.IO;
using System.Windows;
using NawayasaCitramandala.Services;
using NawayasaCitramandala.Localization;

namespace NawayasaCitramandala;

internal static class AppHost
{
    internal static int Run()
    {
        string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup.log");
        try
        {
            File.WriteAllText(logPath, $"[1] Starting at {DateTime.Now}\n");
            AppSettingsStore store = new();
            AppSettings settings = store.Load();
            File.AppendAllText(logPath, "[2] Settings loaded\n");
            Application application = CreateApplication(ShutdownMode.OnMainWindowClose, settings.Theme, settings.Language);
            File.AppendAllText(logPath, "[3] Application created\n");
            application.DispatcherUnhandledException += (s, e) =>
            {
                File.AppendAllText(logPath, $"[DispatcherUnhandledException] {e.Exception}\n");
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                File.AppendAllText(logPath, $"[AppDomain.UnhandledException] {e.ExceptionObject}\n");
            };
            TempFiles.TryConfigureCacheDirectory(settings.CacheDirectory);
            File.AppendAllText(logPath, "[4] Creating MainWindow...\n");
            MainWindow window = new(settings, store);
            window.Loaded += (s, e) => { try { window.Activate(); window.Topmost = true; window.Topmost = false; window.Focus(); } catch { } };
            File.AppendAllText(logPath, "[5] MainWindow created, calling application.Run(window)...\n");
            int code = application.Run(window);
            File.AppendAllText(logPath, $"[6] Application exited with code {code}\n");
            return code;
        }
        catch (Exception ex)
        {
            File.AppendAllText(logPath, $"[FATAL STARTUP EXCEPTION] {ex}\n");
            return 1;
        }
    }

    /// <summary>Installs the design-token theme at the application scope so every window,
    /// control template, and embedded UserControl can resolve resource lookups. The light
    /// palette overlays Tokens when requested; Controls is added last so its references see
    /// the finished order Tokens -> Light (optional) -> Controls.</summary>
    internal static Application CreateApplication(ShutdownMode shutdownMode, AppTheme theme = AppTheme.System,
        string language = L.English)
    {
        L.Initialize(language);
        Application application = new()
        {
            ShutdownMode = shutdownMode,
        };
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("/Themes/Tokens.xaml", UriKind.Relative),
        });
        ThemeResolver.Apply(application, theme);
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("/Themes/Controls.xaml", UriKind.Relative),
        });
        return application;
    }
}
