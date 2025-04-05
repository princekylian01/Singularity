using System;
using System.Threading.Tasks;
using System.Windows;
using Singularity.Updater;

namespace Singularity
{
    public partial class LoadingWindow : Window
    {
        public LoadingWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await StartUpdateProcess();
        }

        private async Task StartUpdateProcess()
        {
            try
            {
                await UpdateManager.DownloadAndApplyUpdateAsync(
                    onProgressChanged: UpdateProgressChanged,
                    onCompleted: UpdateCompleted
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении: {ex.Message}");
                Close();
            }
        }

        private void UpdateProgressChanged(int percentage)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateProgress.Value = percentage;
                ProgressText.Text = $"{percentage}%";
            });
        }

        private void UpdateCompleted(bool success, string message)
        {
            Dispatcher.Invoke(() =>
            {
                if (!success)
                {
                    MessageBox.Show(message, "Ошибка обновления", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
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
            this.Close();
        }
    }
}
