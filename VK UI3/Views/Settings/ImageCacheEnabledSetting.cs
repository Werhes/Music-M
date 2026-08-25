using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using VK_UI3.Services;

namespace VK_UI3.Views.Settings
{
    /// <summary>
    /// Включение/выключение кеширования изображений
    /// </summary>
    public sealed class ImageCacheEnabledSetting : CheckBox
    {
        public ImageCacheEnabledSetting()
        {
            this.Content = "Кеширование изображений";

            this.Checked += OnChecked;
            this.Unchecked += OnUnchecked;
            this.Loaded += OnLoaded;

            Style style = Application.Current.Resources["DefaultCheckBoxStyle"] as Style;
            this.Style = style;

            AutomationProperties.SetName(this, "Кеширование изображений");
            AutomationProperties.SetHelpText(this, "Включает или выключает кеширование изображений на диске");
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                this.IsChecked = CacheSettingsManager.IsImageCacheEnabled();
            });
        }

        private void OnUnchecked(object sender, RoutedEventArgs e)
        {
            CacheSettingsManager.SetImageCacheEnabled(false);
        }

        private void OnChecked(object sender, RoutedEventArgs e)
        {
            CacheSettingsManager.SetImageCacheEnabled(true);
        }
    }
}