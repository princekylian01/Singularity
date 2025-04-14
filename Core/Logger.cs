using System;
using System.IO;
using System.Threading.Tasks;

namespace Singularity.Core
{
    public static class Logger
    {
        private static string _logFilePath = "log.txt";
        private static readonly object LockObject = new object();
        public static bool ClearOnStart { get; set; } = true;

        public static string LogFilePath
        {
            get => _logFilePath;
            set => _logFilePath = string.IsNullOrEmpty(value) ? "log.txt" : value;
        }

        static Logger()
        {
            if (ClearOnStart)
            {
                lock (LockObject)
                {
                    File.WriteAllText(LogFilePath, string.Empty);
                }
            }
        }

        public static void Info(string message)
        {
            Log("INFO", message);
        }

        public static void Error(string message)
        {
            Log("ERROR", message);
        }

        public static void Warn(string message)
        {
            Log("WARN", message);
        }

        private static void Log(string level, string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logEntry = $"[{timestamp}] [{level,-5}] {message}";
            lock (LockObject)
            {
                try
                {
                    File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Log write error: {ex.Message}");
                }
            }
        }
    }
}