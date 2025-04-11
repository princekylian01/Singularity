using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Singularity.Core
{
    public static class Downloader
    {
        private static readonly Regex downloadRegex = new Regex(@"\[download\]\s+\d{1,3}\.\d+%", RegexOptions.Compiled);
        private static readonly string CookieFilePath = "cookies.txt";

        public static bool Start(string url, Action<string> onProgress, out string errorMessage)
        {
            errorMessage = null;
            Logger.Info($"Starting download for URL: {url}");

            bool useCookies = File.Exists(CookieFilePath);

            if (useCookies)
                Logger.Info("cookies.txt найден — будет использован для аутентификации");
            else
                Logger.Warn("cookies.txt не найден — будет произведена попытка без аутентификации");

            return TryDownload(url, useCookies, onProgress, out errorMessage);
        }



        private static bool TryDownload(string url, bool useCookies, Action<string> onProgress, out string errorMessage)
        {
            errorMessage = null;
            string arguments = useCookies
                ? $"\"{url}\" -f bestvideo+bestaudio/best --no-overwrites --no-part --cookies \"{CookieFilePath}\" -o \"C:/Singularity/%(title)s.%(ext)s\""
                : $"\"{url}\" -f bestvideo+bestaudio/best --no-overwrites --no-part -o \"C:/Singularity/%(title)s.%(ext)s\"";

            var psi = new ProcessStartInfo
            {
                FileName = "yt-dlp.exe",
                Arguments = arguments,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (var process = new Process { StartInfo = psi })
                {
                    string lastLine = null;
                    process.OutputDataReceived += (s, e) => { if (e.Data != null) { HandleLine(e.Data, onProgress); lastLine = e.Data; } };
                    process.ErrorDataReceived += (s, e) => { if (e.Data != null) { HandleLine(e.Data, onProgress); lastLine = e.Data; } };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        return true;
                    }
                    else
                    {
                        errorMessage = CleanOutput(lastLine ?? "Unknown error");
                        Logger.Error(errorMessage);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to start download: {ex.Message}";
                Logger.Error(errorMessage);
                return false;
            }
        }

        private static void HandleLine(string line, Action<string> callback)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            string cleanedLine = CleanOutput(line);
            Logger.Info(cleanedLine);
            callback?.Invoke(cleanedLine);
        }

        private static string CleanOutput(string line)
        {
            line = line.Replace("[yt-dlp] ", "").Trim();
            if (line.StartsWith("[Instagram] "))
            {
                line = line.Replace("[Instagram] ", "Instagram - ");
            }
            if (line.Contains(": "))
            {
                int colonIndex = line.IndexOf(": ");
                if (line.Contains("ERROR: "))
                {
                    line = line.Substring(0, colonIndex + 2) + line.Substring(colonIndex + 2).Split('.')[0];
                }
                else if (!downloadRegex.IsMatch(line))
                {
                    line = line.Substring(colonIndex + 2);
                }
            }
            return line;
        }
    }
}