using NLog;

namespace ClientLauncher.Services
{
    public class LoggerService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static void LogInfo(string message)
        {
            Logger.Info(message);
        }

        public static void LogDebug(string message)
        {
            Logger.Debug(message);
        }

        public static void LogWarning(string message)
        {
            Logger.Warn(message);
        }

        public static void LogError(string message, Exception? ex = null)
        {
            if (ex != null)
            {
                Logger.Error(ex, message);
            }
            else
            {
                Logger.Error(message);
            }
        }

        public static void LogFatal(string message, Exception? ex = null)
        {
            if (ex != null)
            {
                Logger.Fatal(ex, message);
            }
            else
            {
                Logger.Fatal(message);
            }
        }
    }
}