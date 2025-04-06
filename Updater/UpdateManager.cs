using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Singularity.Updater
{
    public static class UpdateManager
    {
        private const string UpdateZipFile = "update.zip";

        public static async Task<bool> IsUpdateAvailableAsync()
        {
            Version localVersion = Assembly.GetExecutingAssembly().GetName().Version;

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "SingularityUpdater");
                string json = await client.GetStringAsync(UpdateSettings.LatestReleaseApiUrl);
                var release = JObject.Parse(json);

                string tagName = release["tag_name"]?.ToString() ?? "";
                if (tagName.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                {
                    tagName = tagName.Substring(1);
                }

                if (Version.TryParse(tagName, out Version gitVersion))
                {
                    return gitVersion > localVersion;
                }
                else
                {
                    return false;
                }
            }
        }

        public static async Task DownloadAndApplyUpdateAsync(
            Action<int> onProgressChanged,
            Action<bool, string> onCompleted)
        {
            try
            {
                await DownloadLatestReleaseAsync(onProgressChanged);

                FileExtractor.ExtractZipOverwrite(UpdateZipFile, ".");

                if (File.Exists(UpdateZipFile))
                {
                    File.Delete(UpdateZipFile);
                }

                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                System.Diagnostics.Process.Start(exePath);

                onCompleted(true, "Update applied successfully.");

                await Task.Delay(500);
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                onCompleted(false, $"Ошибка при установке обновления: {ex.Message}");
            }
        }

        private static async Task DownloadLatestReleaseAsync(Action<int> onProgressChanged)
        {
            string downloadUrl = UpdateSettings.DownloadUrl;

            using (HttpClient client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true }))
            {
                client.DefaultRequestHeaders.Add("User-Agent", "SingularityUpdater.exe");
                using (var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    long? totalBytes = response.Content.Headers.ContentLength;
                    using (var downloadStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(UpdateZipFile, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        byte[] buffer = new byte[81920];
                        long totalRead = 0;
                        int bytesRead;

                        while ((bytesRead = await downloadStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalRead += bytesRead;

                            if (totalBytes.HasValue && totalBytes > 0)
                            {
                                int progress = (int)((totalRead * 100) / totalBytes.Value);
                                onProgressChanged?.Invoke(progress);
                            }
                            else
                            {
                                onProgressChanged?.Invoke((int)(totalRead / 1024));
                            }
                        }
                    }
                }
            }
        }
    }
}
