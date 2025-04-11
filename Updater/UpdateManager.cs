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
            Version localVersion = Assembly.GetExecutingAssembly().GetName().Version;
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
                await DownloadLatestReleaseAsync(onProgressChanged);
                Logger.Info("Распаковка обновления");
                FileExtractor.ExtractZipOverwrite(UpdateZipFile, ".");

                if (File.Exists(UpdateZipFile))
                {
                    Logger.Info("Удаление временного файла update.zip");
                    File.Delete(UpdateZipFile);
                }

                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                Logger.Info($"Перезапуск приложения: {exePath}");
                Process.Start(exePath);

                onCompleted(true, "Update applied successfully.");
                Logger.Info("Обновление успешно применено");

                await Task.Delay(500);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка при установке обновления: {ex.Message}");
                onCompleted(false, $"Ошибка при установке обновления: {ex.Message}");
            }
        }

        private static async Task DownloadLatestReleaseAsync(Action<int> onProgressChanged)
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
                            else
                            {
                                onProgressChanged?.Invoke((int)(totalRead / 1024));
                                Logger.Info($"Скачано: {totalRead / 1024} KB");
                            }
                        }
                    }
                }
            }
            Logger.Info("Скачивание обновления завершено");
        }
    }
}