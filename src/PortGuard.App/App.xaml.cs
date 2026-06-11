using System.IO;
using System.Windows;

namespace PortGuard.App;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += (s, e) =>
        {
            LogCrash($"CRASH: {e.Exception.GetType().Name}: {e.Exception.Message}\n{e.Exception.StackTrace}");
            e.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            LogCrash($"FATAL: {e.ExceptionObject}");
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            LogCrash($"TASK: {e.Exception.GetType().Name}: {e.Exception.Message}");
            e.SetObserved();
        };
    }

    private static void LogCrash(string msg)
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "portguard_crash.log");
            File.AppendAllText(path, $"[{DateTime.Now:HH:mm:ss}] {msg}\n---\n");
        }
        catch { }
    }
}
