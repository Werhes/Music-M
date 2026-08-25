using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;

namespace VK_UI3.Views.Settings
{
    public sealed class FullScreenLyricsSetting : CheckBox
    {
        public FullScreenLyricsSetting()
        {
            try
            {
                this.Content = "Не отображать текст в полноэкранном плеере";

                this.Checked += OnChecked;
                this.Unchecked += OnUnchecked;
                this.Loaded += OnLoaded;

                // Получение стиля из ресурсов
                Style style = Application.Current.Resources["DefaultCheckBoxStyle"] as Style;
                this.Style = style;

                AutomationProperties.SetName(this, "Не отображать текст в полноэкранном плеере");
                AutomationProperties.SetHelpText(this, "При включении этой опции текст песни не будет загружаться и отображаться в полноэкранном режиме плеера");
            }
            catch { }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.DispatcherQueue.TryEnqueue(async () =>
            {
                var setting = DB.SettingsTable.GetSetting("fullScreenHideLyrics");
                if (setting == null)
                    return;
                this.IsChecked = setting.settingValue.Equals("1");
            });
        }

        private void OnUnchecked(object sender, RoutedEventArgs e)
        {
            DB.SettingsTable.SetSetting("fullScreenHideLyrics", "0");
        }

        private void OnChecked(object sender, RoutedEventArgs e)
        {
            DB.SettingsTable.SetSetting("fullScreenHideLyrics", "1");
        }
    }
}