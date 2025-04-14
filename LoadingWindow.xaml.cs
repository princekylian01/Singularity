using System;
using System.Threading.Tasks;
using System.Windows;
using Singularity.Updater;
using Singularity.Core;

namespace Singularity
{
    public partial class LoadingWindow : Window
    {
        public LoadingWindow()
        {
            InitializeComponent();
            Logger.Info("Инициализация окна обновления");
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Logger.Info("Открыто окно обновления");
            await StartUpdateProcess();
        }

        private async Task StartUpdateProcess()
        {
            if (MessageBox.Show("Доступно обновление. Начать загрузку?", "Обновление", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Logger.Info("Пользователь согласился на обновление");
                try
                {
                    await UpdateManager.DownloadAndApplyUpdateAsync(
                        onProgressChanged: UpdateProgressChanged,
                        onCompleted: UpdateCompleted
                    );
                }
                catch (Exception ex)
                {
                    Logger.Error($"Ошибка при обновлении: {ex.Message}");
                    MessageBox.Show($"Ошибка при обновлении: {ex.Message}");
                    Close();
                }
            }
            else
            {
                Logger.Info("Пользователь отказался от обновления");
                new MainWindow().Show();
                this.Close();
            }
        }

        private void UpdateProgressChanged(int percentage)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateProgress.Value = percentage;
                ProgressText.Text = $"{percentage}%";
                Logger.Info($"Прогресс обновления: {percentage}%");
            });
        }

        private void UpdateCompleted(bool success, string message)
        {
            Dispatcher.Invoke(() =>
            {
                if (!success)
                {
                    Logger.Error($"Обновление не удалось: {message}");
                    MessageBox.Show(message, "Ошибка обновления", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    Logger.Info("Обновление успешно установлено");
                    MessageBox.Show("Обновление успешно установлено. Приложение перезапустится.",
                                    "Успешно",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                }
                Close();
            });
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("Нажата кнопка закрытия окна обновления");
            this.Close();
        }
    }
}