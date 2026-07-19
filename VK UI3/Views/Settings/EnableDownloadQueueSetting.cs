using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using VK_UI3.Services;

namespace VK_UI3.Views.Settings
{
    /// <summary>
    /// Включение/выключение системы очередей загрузки изображений
    /// </summary>
    public sealed class EnableDownloadQueueSetting : CheckBox
    {
        public EnableDownloadQueueSetting()
        {
            this.Content = "Очередь загрузки изображений";

            this.Checked += OnChecked;
            this.Unchecked += OnUnchecked;
            this.Loaded += OnLoaded;

            Style style = Application.Current.Resources["DefaultCheckBoxStyle"] as Style;
            this.Style = style;

            AutomationProperties.SetName(this, "Очередь загрузки изображений");
            AutomationProperties.SetHelpText(this, "Включает или выключает систему очередей при загрузке изображений");
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                this.IsChecked = CacheSettingsManager.IsDownloadQueueEnabled();
            });
        }

        private void OnUnchecked(object sender, RoutedEventArgs e)
        {
            CacheSettingsManager.SetDownloadQueueEnabled(false);
            CacheSettingsManager.ApplyAllCacheSettings();
        }

        private void OnChecked(object sender, RoutedEventArgs e)
        {
            CacheSettingsManager.SetDownloadQueueEnabled(true);
            CacheSettingsManager.ApplyAllCacheSettings();
        }
    }
}