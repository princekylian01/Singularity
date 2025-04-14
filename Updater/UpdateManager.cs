using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json.Linq;
using Singularity.Core;

namespace Singularity.Updater
{
    public static class UpdateManager
    {
        private const string UpdateZipFile = "update.zip";

        public static async Task<bool> IsUpdateAvailableAsync()
        {
            Logger.Info("Проверка доступности обновлений");

            string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Singularity.exe");
            Version localVersion = new Version(0, 0, 0, 0);

            if (File.Exists(exePath))
            {
                try
                {
                    localVersion = new Version(FileVersionInfo.GetVersionInfo(exePath).FileVersion);
                }
                catch (Exception ex)
                {
                    Logger.Error("Ошибка получения версии Singularity.exe: " + ex.Message);
                }
            }

            Logger.Info($"Локальная версия: {localVersion}");

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "SingularityUpdater");
                    string json = await client.GetStringAsync(UpdateSettings.LatestReleaseApiUrl);
                    var release = JObject.Parse(json);

                    string tagName = release["tag_name"]?.ToString() ?? "";
                    Logger.Info($"Получен тег релиза: {tagName}");
                    if (tagName.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                    {
                        tagName = tagName.Substring(1);
                    }

                    if (Version.TryParse(tagName, out Version gitVersion))
                    {
                        Logger.Info($"Версия на GitHub: {gitVersion}");
                        bool isNewer = gitVersion > localVersion;
                        Logger.Info($"Доступно обновление: {isNewer}");
                        return isNewer;
                    }
                    else
                    {
                        Logger.Warn("Не удалось распознать версию релиза");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка при проверке обновлений: {ex.Message}");
                return false;
            }
        }

        public static async Task DownloadAndApplyUpdateAsync(
            Action<int> onProgressChanged,
            Action<bool, string> onCompleted)
        {
            Logger.Info("Начало процесса обновления");

            try
            {
                await DownloadLatestReleaseAsync(onProgressChanged, onCompleted);
                Logger.Info("Скачивание завершено. Запуск внешнего обновления...");

                string updaterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SingularityUpdater.exe");
                string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Singularity.exe");
                string pid = Process.GetCurrentProcess().Id.ToString();

                Process.Start(updaterPath, $"\"{exePath}\" {pid}");

                Logger.Info("Завершение приложения для обновления");
                Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
            }
            catch (HttpRequestException ex)
            {
                Logger.Error($"Ошибка сети: {ex.Message}");
                onCompleted(false, "Нет подключения к интернету. Проверьте сеть и попробуйте снова.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка при установке обновления: {ex.Message}");
                onCompleted(false, $"Ошибка при установке обновления: {ex.Message}");
            }
        }

        private static async Task DownloadLatestReleaseAsync(
            Action<int> onProgressChanged,
            Action<bool, string> onCompleted)
        {
            try
            {
                string downloadUrl = UpdateSettings.DownloadUrl;
                Logger.Info($"Скачивание обновления с {downloadUrl}");

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
                                    Logger.Info($"Прогресс скачивания: {progress}%");
                                }
                            }
                        }
                    }
                }

                Logger.Info("Скачивание обновления завершено");
            }
            catch (HttpRequestException ex)
            {
                Logger.Error($"HttpRequestException: {ex.Message}");
                onCompleted(false, "Нет подключения к интернету. Проверьте сеть и попробуйте снова.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка скачивания: {ex.Message}");
                onCompleted(false, "Ошибка при скачивании обновления. Попробуйте позже.");
            }
        }
    }
}
