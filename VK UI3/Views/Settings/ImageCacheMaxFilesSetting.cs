using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Threading.Tasks;
using VK_UI3.Services;
using Microsoft.UI.Xaml.Automation;

namespace VK_UI3.Views.Settings
{
    /// <summary>
    /// Настройка максимального количества файлов в кеше изображений
    /// </summary>
    public sealed class ImageCacheMaxFilesSetting : Slider
    {
        public ImageCacheMaxFilesSetting()
        {
            this.Minimum = 100;
            this.Maximum = 50000;
            this.StepFrequency = 100;
            this.Style = Application.Current.Resources["DefaultSliderStyle"] as Style;

            int val = CacheSettingsManager.GetImageCacheMaxFiles();
            this.Value = val;

            this.ValueChanged += OnValueChanged;

            AutomationProperties.SetName(this, "Максимум файлов в кеше");
            AutomationProperties.SetHelpText(this, "Максимальное количество файлов, которое может храниться в кеше изображений");
        }

        private void OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            Task.Run(() =>
            {
                CacheSettingsManager.SetImageCacheMaxFiles((int)e.NewValue);
            });
        }
    }
}