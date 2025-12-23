using ClientLauncher.Windows;
using NLog;
using System.IO;
using System.Windows;

namespace ClientLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            try
            {
                var logsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                Directory.CreateDirectory(logsPath);
                Directory.CreateDirectory(Path.Combine(logsPath, "Archived"));
                Directory.CreateDirectory(Path.Combine(logsPath, "Errors", "Archived"));

                Logger.Info("===============================================");
                Logger.Info("ClientLauncher Application Started");
                Logger.Info($"Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
                Logger.Info($"User: {Environment.UserName}");
                Logger.Info($"Machine: {Environment.MachineName}");
                Logger.Info("===============================================");

                // Handle unhandled exceptions
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
                DispatcherUnhandledException += OnDispatcherUnhandledException;
                TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

                // Check if launched with --app argument
                if (e.Args.Length > 0)
                {
                    var appCodeArg = e.Args.FirstOrDefault(arg => arg.StartsWith("--app="));

                    if (!string.IsNullOrEmpty(appCodeArg))
                    {
                        // Extract appCode
                        var appCode = appCodeArg.Replace("--app=", string.Empty);

                        // Show Update/Launch Window instead of Main Window
                        var launchWindow = new LaunchWindow(appCode);
                        launchWindow.Show();
                        return;
                    }
                }
                else
                {
                    Logger.Info("Starting main application window");
                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "Fatal error during application startup");
                MessageBox.Show(
                    $"A fatal error occurred during startup:\n\n{ex.Message}",
                    "Fatal Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                Shutdown(1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Info("===============================================");
            Logger.Info($"ClientLauncher Application Exiting with code: {e.ApplicationExitCode}");
            Logger.Info("===============================================");

            LogManager.Shutdown();
            base.OnExit(e);
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            Logger.Fatal(exception, "Unhandled exception occurred");

            MessageBox.Show(
                $"A critical error occurred:\n\n{exception?.Message}\n\nThe application will now close.",
                "Critical Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.Error(e.Exception, "Dispatcher unhandled exception");

            MessageBox.Show(
                $"An error occurred:\n\n{e.Exception.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );

            e.Handled = true;
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Logger.Error(e.Exception, "Unobserved task exception");
            e.SetObserved();
        }
    }
}