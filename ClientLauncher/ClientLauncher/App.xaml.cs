using ClientLauncher.Windows;
using System.Windows;

namespace ClientLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ✅ Check if launched with --app argument
            if (e.Args.Length > 0)
            {
                var appCodeArg = e.Args.FirstOrDefault(arg => arg.StartsWith("--app="));

                if (!string.IsNullOrEmpty(appCodeArg))
                {
                    // Extract appCode
                    var appCode = appCodeArg.Replace("--app=", string.Empty);

                    // ✅ Show Update/Launch Window instead of Main Window
                    var launchWindow = new LaunchWindow(appCode);
                    launchWindow.Show();
                    return;
                }
            }

            // ✅ No arguments -> Show normal application selection window
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}