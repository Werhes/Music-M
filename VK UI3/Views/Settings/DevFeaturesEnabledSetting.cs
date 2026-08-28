using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using VK_UI3.Services;

namespace VK_UI3.Views.Settings
{
    /// <summary>
    /// Включение/выключение dev-функций
    /// </summary>
    public sealed class DevFeaturesEnabledSetting : CheckBox
    {
        public DevFeaturesEnabledSetting()
        {
            this.Content = "Включить dev-функции";

            this.Checked += OnChecked;
            this.Unchecked += OnUnchecked;
            this.Loaded += OnLoaded;

            Style style = Application.Current.Resources["DefaultCheckBoxStyle"] as Style;
            this.Style = style;

            AutomationProperties.SetName(this, "Включить dev-функции");
            AutomationProperties.SetHelpText(this, "Включает раздел с dev-функциями в навигации приложения");
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                this.IsChecked = CacheSettingsManager.IsDevFeaturesEnabled();
            });
        }

        private void OnUnchecked(object sender, RoutedEventArgs e)
        {
            CacheSettingsManager.SetDevFeaturesEnabled(false);
            Views.MainView.mainView?.UpdateDevFeaturesMenuVisibility();
        }

        private void OnChecked(object sender, RoutedEventArgs e)
        {
            CacheSettingsManager.SetDevFeaturesEnabled(true);
            Views.MainView.mainView?.UpdateDevFeaturesMenuVisibility();
        }
    }
}