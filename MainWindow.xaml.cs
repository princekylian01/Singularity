using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Singularity.Core;
using Singularity.Updater;

namespace Singularity
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await CheckForUpdatesAsync();
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                bool isUpdate = await UpdateManager.IsUpdateAvailableAsync();
                if (isUpdate)
                {
                    LoadingWindow loadingWindow = new LoadingWindow();
                    loadingWindow.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при проверке обновлений: {ex.Message}");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            var url = ClipboardHelper.GetUrlFromClipboard();
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("Скопируйте ссылку в буфер обмена.");
                return;
            }

            var source = SourceResolver.GetSource(url);
            if (source == "Unknown")
            {
                MessageBox.Show("Источник не поддерживается.");
                return;
            }

            StartDownload(url);
        }

        private void FolderButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", "C:\\Singularity");
        }

        private void StartDownload(string url)
        {
            DownloadLog.Text = "Waiting...";

            Thread thread = new Thread(() =>
            {
                Dispatcher.Invoke(() => DownloadLog.Text = "Downloading...");

                Downloader.Start(url, progress =>
                {
                    Dispatcher.Invoke(() => DownloadLog.Text = progress);
                });

                Dispatcher.Invoke(async () =>
                {
                    DownloadLog.Text = "Done!";
                    await Task.Delay(5000);
                    DownloadLog.Text = "";
                });
            });

            thread.IsBackground = true;
            thread.Start();
        }
    }
}
