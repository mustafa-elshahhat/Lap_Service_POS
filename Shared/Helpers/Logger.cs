using System;
using System.IO;

namespace AlJohary.ServiceHub.Shared.Helpers
{
    public static class Logger
    {
        private static readonly string LogPath = AppPaths.LogFile;

        public static void LogException(Exception ex, string context = "")
        {
            try
            {
                var message = $"{DateTime.Now:s} - {context}:\n{ex}\n\n";
                File.AppendAllText(LogPath, message);
            }
            catch { }
        }

        public static void LogInfo(string message)
        {
            try
            {
                var logEntry = $"{DateTime.Now:s} - INFO: {message}\n";
                File.AppendAllText(LogPath, logEntry);
            }
            catch { }
        }

        public static void LogWarning(string message)
        {
            try
            {
                var logEntry = $"{DateTime.Now:s} - WARNING: {message}\n";
                File.AppendAllText(LogPath, logEntry);
            }
            catch { }
        }
    }
}
