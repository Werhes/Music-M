using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using VK_UI3.Services;

namespace VK_UI3.Views.Settings
{
    /// <summary>
    /// Автоочистка кеша изображений при запуске
    /// </summary>
    public sealed class AutoClearImageCacheSetting : CheckBox
    {
        public AutoClearImageCacheSetting()
        {
            this.Content = "Очищать кеш изображений при запуске";

            this.Checked += OnChecked;
            this.Unchecked += OnUnchecked;
            this.Loaded += OnLoaded;

            Style style = Application.Current.Resources["DefaultCheckBoxStyle"] as Style;
            this.Style = style;

            AutomationProperties.SetName(this, "Очищать кеш изображений при запуске");
            AutomationProperties.SetHelpText(this, "Автоматически очищает кеш изображений при каждом запуске приложения");
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                this.IsChecked = CacheSettingsManager.IsAutoClearImageCacheOnStart();
            });
        }

        private void OnUnchecked(object sender, RoutedEventArgs e)
        {
            CacheSettingsManager.SetAutoClearImageCacheOnStart(false);
        }

        private void OnChecked(object sender, RoutedEventArgs e)
        {
            CacheSettingsManager.SetAutoClearImageCacheOnStart(true);
        }
    }
}