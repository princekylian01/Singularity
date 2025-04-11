using System;
using System.IO;

namespace Singularity.Core
{
    public static class Logger
    {
        private static readonly string LogFilePath = "log.txt";
        private static readonly object LockObject = new object();

        static Logger()
        {
            lock (LockObject)
            {
                File.WriteAllText(LogFilePath, string.Empty);
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
                File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
            }
        }
    }
}