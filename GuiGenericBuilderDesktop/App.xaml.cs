using System.Configuration;
using System.Data;
using System.Windows;
using Serilog;
using System.IO;

namespace GuiGenericBuilderDesktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Configure Serilog
            var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logDirectory);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    path: Path.Combine(logDirectory, "gui-generic-builder-.log"),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    retainedFileCountLimit: 30,
                    fileSizeLimitBytes: 10485760) // 10MB
                .WriteTo.Debug()
                .CreateLogger();

            Log.Information("=== GUI Generic Builder Desktop Started ===");
            Log.Information("Application version: {Version}", typeof(App).Assembly.GetName().Version);
            Log.Information("Base directory: {BaseDirectory}", AppDomain.CurrentDomain.BaseDirectory);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("=== GUI Generic Builder Desktop Stopped ===");
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
