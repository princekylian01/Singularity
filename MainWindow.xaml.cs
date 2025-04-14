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
            Logger.Info("Инициализация главного окна приложения");
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Logger.Info("Приложение запущено, проверка обновлений...");
            bool isUpdate = await UpdateManager.IsUpdateAvailableAsync();
            if (isUpdate)
            {
                Logger.Info("Обновление найдено, открытие окна загрузки");
                LoadingWindow loadingWindow = new LoadingWindow();
                loadingWindow.Show();
                this.Close();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("Нажата кнопка закрытия");
            if (DownloadLog.Text.Contains("Downloading"))
            {
                Logger.Warn("Попытка закрыть приложение во время загрузки");
                if (MessageBox.Show("Download in progress. Are you sure you want to close?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    Logger.Info("Пользователь подтвердил закрытие");
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
                Logger.Info("Начато перетаскивание окна");
                DragMove();
            }
        }

        private async void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("Нажата кнопка Launch");
            var url = ClipboardHelper.GetUrlFromClipboard();
            if (string.IsNullOrEmpty(url))
            {
                Logger.Warn("URL не найден в буфере обмена");
                MessageBox.Show("Скопируйте ссылку в буфер обмена.");
                return;
            }

            var source = SourceResolver.GetSource(url);
            Logger.Info($"Источник URL: {source}");
            if (source == "Unknown")
            {
                Logger.Warn("Неподдерживаемый источник");
                MessageBox.Show("Источник не поддерживается.");
                return;
            }

            await StartDownload(url);
        }

        private void FolderButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("Нажата кнопка FOLDER");
            try
            {
                Process.Start("explorer.exe", "C:\\Singularity");
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка при открытии папки: {ex.Message}");
                MessageBox.Show($"Ошибка при открытии папки: {ex.Message}");
            }
        }

        private async Task StartDownload(string url)
        {
            try
            {
                Logger.Info($"Starting download: {url}");

                DownloadLog.Text = "Waiting...";
                bool success = false;
                string errorMessage = null;

                await Task.Run(async () =>
                {
                    await Dispatcher.InvokeAsync(() => DownloadLog.Text = "Downloading...");
                    success = Downloader.Start(url, progress =>
                    {
                        Dispatcher.Invoke(() => DownloadLog.Text = progress);
                    }, out errorMessage);
                });

                if (success)
                {
                    Logger.Info("Download completed successfully");
                    DownloadLog.Text = "Download finished!";

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
                    Storyboard.SetTargetProperty(animation, new PropertyPath(UIElement.OpacityProperty));
                    storyboard.Children.Add(animation);
                    storyboard.Begin();

                    await Task.Delay(7000);
                    storyboard.Stop();
                    DownloadLog.Text = "";
                }
                else
                {
                    Logger.Error($"Download failed: {errorMessage}");
                    await Dispatcher.InvokeAsync(() => DownloadLog.Text = $"Failed: {errorMessage}");

                    if (errorMessage.Contains("Restricted") || errorMessage.Contains("Login Required") || errorMessage.Contains("403"))
                    {
                        MessageBox.Show("Загрузка не удалась. Возможно, видео является приватным или содержит возрастные ограничения. Положите cookies.txt с авторизацией Instagram/Twitter в папку с приложением.");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Unexpected error: {ex.Message}");
                await Dispatcher.InvokeAsync(() => DownloadLog.Text = $"Error: {ex.Message}");
            }
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Found a bug? Write to kyliannox@gmail.com",
                "Information",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}