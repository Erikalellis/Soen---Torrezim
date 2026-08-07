using System;
using System.IO;

namespace Soen___Torrezim
{
    public static class Logger
    {
        private static readonly object _sync = new object();
        private static readonly string _logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? ".", "logs", "soen.log");

        public static void LogError(Exception ex)
        {
            try
            {
                lock (_sync)
                {
                    var dir = Path.GetDirectoryName(_logFile);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    File.AppendAllText(_logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {ex}\r\n");
                }
            }
            catch { /* never throw from logger */ }
        }

        public static void LogInfo(string message)
        {
            try
            {
                lock (_sync)
                {
                    var dir = Path.GetDirectoryName(_logFile);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    File.AppendAllText(_logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO: {message}\r\n");
                }
            }
            catch { }
        }
    }
}
