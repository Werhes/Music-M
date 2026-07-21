using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using VK_UI3.Services;

namespace VK_UI3.Views.Settings
{
    /// <summary>
    /// CheckBox для включения/отключения кеширования треков
    /// </summary>
    public sealed class TrackCacheEnabledSetting : CheckBox
    {
        public TrackCacheEnabledSetting()
        {
            this.Content = "Кешировать треки";
            this.Loaded += OnLoaded;
            this.Unchecked += OnUnchecked;
            this.Checked += OnChecked;

            AutomationProperties.SetName(this, "Кешировать треки");
            AutomationProperties.SetHelpText(this, "Включает или отключает кеширование аудио-треков на диск");
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.IsChecked = CacheSettingsManager.IsTrackCacheEnabled();
        }

        private void OnUnchecked(object sender, RoutedEventArgs e)
        {
            CacheSettingsManager.SetTrackCacheEnabled(false);
        }

        private void OnChecked(object sender, RoutedEventArgs e)
        {
            CacheSettingsManager.SetTrackCacheEnabled(true);
        }
    }
}