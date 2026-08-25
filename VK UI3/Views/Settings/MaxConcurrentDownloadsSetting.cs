using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Threading.Tasks;
using VK_UI3.Services;
using Microsoft.UI.Xaml.Automation;

namespace VK_UI3.Views.Settings
{
    /// <summary>
    /// Настройка максимального количества одновременных загрузок изображений
    /// </summary>
    public sealed class MaxConcurrentDownloadsSetting : Slider
    {
        public MaxConcurrentDownloadsSetting()
        {
            this.Minimum = 1;
            this.Maximum = 50;
            this.StepFrequency = 1;
            this.Style = Application.Current.Resources["DefaultSliderStyle"] as Style;

            int val = CacheSettingsManager.GetMaxConcurrentDownloads();
            this.Value = val;

            this.ValueChanged += OnValueChanged;

            AutomationProperties.SetName(this, "Максимум одновременных загрузок");
            AutomationProperties.SetHelpText(this, "Максимальное количество изображений, загружаемых одновременно");
        }

        private void OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            string a = e.NewValue.ToString();
            Task.Run(() =>
            {
                CacheSettingsManager.SetMaxConcurrentDownloads((int)e.NewValue);
                CacheSettingsManager.ApplyAllCacheSettings();
            });
        }
    }
}