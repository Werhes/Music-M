using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using System;
using System.Threading.Tasks;
using VK_UI3.Services;

namespace VK_UI3.Views.Settings
{
    /// <summary>
    /// Кнопка очистки кеша треков с отображением текущего размера
    /// </summary>
    public sealed class ClearTrackCacheButton : Button
    {
        public ClearTrackCacheButton()
        {
            this.Content = "Очистить кеш треков";
            this.Click += OnClick;
            this.Loaded += OnLoaded;
            this.Style = Application.Current.Resources["DefaultButtonStyle"] as Style;

            AutomationProperties.SetName(this, "Очистить кеш треков");
            AutomationProperties.SetHelpText(this, "Удаляет все файлы из кеша треков");
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await UpdateCacheInfoAsync();
        }

        private async void OnClick(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            try
            {
                TrackCacheManager.ClearCache();
                this.Content = "Кеш очищен!";
                await Task.Delay(2000);
                await UpdateCacheInfoAsync();
            }
            finally
            {
                this.IsEnabled = true;
            }
        }

        private async Task UpdateCacheInfoAsync()
        {
            await Task.Run(() =>
            {
                long sizeBytes = TrackCacheManager.GetCacheSizeBytes();
                int fileCount = TrackCacheManager.GetCacheFileCount();
                string sizeStr = FormatSize(sizeBytes);

                this.DispatcherQueue.TryEnqueue(() =>
                {
                    this.Content = $"Очистить кеш треков ({sizeStr}, {fileCount} файлов)";
                });
            });
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} Б";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} КБ";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} МБ";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} ГБ";
        }
    }
}