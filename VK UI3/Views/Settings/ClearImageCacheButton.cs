using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using VK_UI3.Services;
using Microsoft.UI.Xaml.Automation;

namespace VK_UI3.Views.Settings
{
    /// <summary>
    /// Кнопка очистки кеша изображений с отображением текущего размера
    /// </summary>
    public sealed class ClearImageCacheButton : Button
    {
        public ClearImageCacheButton()
        {
            this.Content = "Очистить кеш изображений";
            this.Click += OnClick;
            this.Loaded += OnLoaded;
            this.Style = Application.Current.Resources["DefaultButtonStyle"] as Style;

            AutomationProperties.SetName(this, "Очистить кеш изображений");
            AutomationProperties.SetHelpText(this, "Удаляет все файлы из кеша изображений");
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
                CacheSettingsManager.ClearImageCache();
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
                long sizeBytes = CacheSettingsManager.GetImageCacheSizeBytes();
                int fileCount = CacheSettingsManager.GetImageCacheFileCount();
                string sizeStr = FormatSize(sizeBytes);

                this.DispatcherQueue.TryEnqueue(() =>
                {
                    this.Content = $"Очистить кеш изображений ({sizeStr}, {fileCount} файлов)";
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