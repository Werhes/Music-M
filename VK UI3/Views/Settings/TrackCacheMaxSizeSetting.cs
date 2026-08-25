using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Automation;
using VK_UI3.Services;

namespace VK_UI3.Views.Settings
{
    /// <summary>
    /// Slider для настройки максимального размера кеша треков (в МБ)
    /// Диапазон: 100–50000 МБ (100 МБ – 50 ГБ), шаг 100 МБ
    /// </summary>
    public sealed class TrackCacheMaxSizeSetting : Slider
    {
        public TrackCacheMaxSizeSetting()
        {
            this.Minimum = 100;
            this.Maximum = 50000;
            this.StepFrequency = 100;
            this.TickFrequency = 5000;
            this.TickPlacement = TickPlacement.None;
            this.Loaded += OnLoaded;
            this.ValueChanged += OnValueChanged;

            AutomationProperties.SetName(this, "Максимальный размер кеша треков");
            AutomationProperties.SetHelpText(this, "Устанавливает максимальный размер кеша треков в мегабайтах (100 МБ – 50 ГБ)");
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.Value = CacheSettingsManager.GetTrackCacheMaxSizeMb();
        }

        private void OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            CacheSettingsManager.SetTrackCacheMaxSizeMb((int)e.NewValue);
        }
    }
}