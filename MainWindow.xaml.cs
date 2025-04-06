using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
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
            if (DownloadLog.Text.Contains("Downloading"))
            {
                if (MessageBox.Show("Download in progress. Are you sure you want to close?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    Close();
                }
            }
            else
            {
                Close();
            }
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

        private async void StartDownload(string url)
        {
            try
            {
                DownloadLog.Text = "Waiting...";
                await Task.Run(() =>
                {
                    Dispatcher.Invoke(() => DownloadLog.Text = "Downloading...");
                    Downloader.Start(url, progress => Dispatcher.Invoke(() => DownloadLog.Text = progress));
                });

                DownloadLog.Text = "Done!";
                var storyboard = new Storyboard();
                var animation = new DoubleAnimation
                {
                    From = 0.5,
                    To = 1.0,
                    Duration = TimeSpan.FromSeconds(1),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                Storyboard.SetTarget(animation, DownloadLog);
                Storyboard.SetTargetProperty(animation, new PropertyPath("Opacity"));
                storyboard.Children.Add(animation);
                storyboard.Begin();

                await Task.Delay(7000);
                storyboard.Stop();
                DownloadLog.Text = "";
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => DownloadLog.Text = $"Error: {ex.Message}");
            }
        }
    }
}