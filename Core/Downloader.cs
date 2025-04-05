using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Singularity.Core
{
    public static class Downloader
    {
        private static readonly Regex downloadRegex = new Regex(@"\[download\]\s+\d{1,3}\.\d+%", RegexOptions.Compiled);

        public static void Start(string url, Action<string> onProgress)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "yt-dlp.exe",
                Arguments = $"\"{url}\" -f bestvideo+bestaudio/best --no-overwrites --no-part -o \"C:/Singularity/%(title)s.%(ext)s\"",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = new Process { StartInfo = psi };

            process.OutputDataReceived += (s, e) => HandleLine(e.Data, onProgress);
            process.ErrorDataReceived += (s, e) => HandleLine(e.Data, onProgress);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }

        private static void HandleLine(string line, Action<string> callback)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            var match = downloadRegex.Match(line);
            var display = match.Success ? match.Value : line;

            callback?.Invoke(display);
        }
    }
}