using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using VK_UI3.Services;

namespace VK_UI3.Views.Settings
{
    /// <summary>
    /// Включение/выключение кеширования в памяти
    /// </summary>
    public sealed class MemoryCacheEnabledSetting : CheckBox
    {
        public MemoryCacheEnabledSetting()
        {
            this.Content = "Кеширование в памяти";

            this.Checked += OnChecked;
            this.Unchecked += OnUnchecked;
            this.Loaded += OnLoaded;

            Style style = Application.Current.Resources["DefaultCheckBoxStyle"] as Style;
            this.Style = style;

            AutomationProperties.SetName(this, "Кеширование в памяти");
            AutomationProperties.SetHelpText(this, "Включает или выключает кеширование данных в оперативной памяти");
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                this.IsChecked = CacheSettingsManager.IsMemoryCacheEnabled();
            });
        }

        private void OnUnchecked(object sender, RoutedEventArgs e)
        {
            CacheSettingsManager.SetMemoryCacheEnabled(false);
        }

        private void OnChecked(object sender, RoutedEventArgs e)
        {
            CacheSettingsManager.SetMemoryCacheEnabled(true);
        }
    }
}