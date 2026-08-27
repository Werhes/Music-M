using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using VK_UI3.DownloadTrack;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VK_UI3.Views.Download
{
    public sealed partial class DownloadFFmpegController : UserControl
    {
        private DispatcherQueueTimer _hideTimer;

        public DownloadFFmpegController()
        {
            this.InitializeComponent();

            this.Loaded += DownloadFFmpegController_Loaded;
            this.Unloaded += DownloadFFmpegController_Unloaded;
        }

        private void DownloadFFmpegController_Unloaded(object sender, RoutedEventArgs e)
        {
            if (MainWindow.downloadFileWithProgress != null)
            {
                MainWindow.downloadFileWithProgress.ProgressChanged -= DownloadFileWithProgress_ProgressChanged;
                MainWindow.downloadFileWithProgress.DownloadCompleted -= DownloadFileWithProgress_DownloadCompleted;
            }
            _hideTimer?.Stop();
        }

        private void DownloadFileWithProgress_DownloadCompleted(object sender, EventArgs e)
        {
            var dl = sender as DownloadFileWithProgress;
            if (dl != null)
            {
                dl.DownloadCompleted -= DownloadFileWithProgress_DownloadCompleted;
                dl.ProgressChanged -= DownloadFileWithProgress_ProgressChanged;
            }

            bool hasError = (e as System.ComponentModel.AsyncCompletedEventArgs)?.Error != null;

            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (hasError)
                {
                    DownloadTitle.Text = "Ошибка загрузки FFmpeg";
                    pathText.Text = "Попробуйте ещё раз позже.";
                    DownloadProgressBar.Value = 0;
                    return;
                }

                // Установка завершена — показываем статус и скрываем сообщение
                DownloadProgressBar.Value = 100;
                DownloadTitle.Text = "Дополнение установлено";
                pathText.Text = "FFmpeg установлен.";

                if (_hideTimer == null)
                {
                    _hideTimer = this.DispatcherQueue.CreateTimer();
                    _hideTimer.IsRepeating = false;
                    _hideTimer.Tick += (s, args) => this.Visibility = Visibility.Collapsed;
                }
                _hideTimer.Interval = TimeSpan.FromSeconds(1.5);
                _hideTimer.Start();
            });
        }

        private void DownloadFFmpegController_Loaded(object sender, RoutedEventArgs e)
        {
            if (MainWindow.downloadFileWithProgress != null)
            {
                MainWindow.downloadFileWithProgress.ProgressChanged += DownloadFileWithProgress_ProgressChanged;
                MainWindow.downloadFileWithProgress.DownloadCompleted += DownloadFileWithProgress_DownloadCompleted;
            }
        }

        private void DownloadFileWithProgress_ProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                int progressPercentage = e.ProgressPercentage; // Прогресс в процентах
                DownloadProgressBar.Value = progressPercentage;
                double totalMb = (sender as DownloadFileWithProgress)?.mb ?? 0;
                double loadedMb = e.BytesReceived / (1024.0 * 1024.0);
                pathText.Text = $"Загружено: {Math.Round(loadedMb)} Мб  из  {Math.Round(totalMb)} Мб";
            });
        }

    }
}
