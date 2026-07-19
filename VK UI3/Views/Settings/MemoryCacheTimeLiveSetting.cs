using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Threading.Tasks;
using VK_UI3.Services;
using Microsoft.UI.Xaml.Automation;

namespace VK_UI3.Views.Settings
{
    /// <summary>
    /// Настройка времени жизни кеша в памяти (в минутах)
    /// </summary>
    public class MemoryCacheTimeLiveSetting : Slider
    {
        public MemoryCacheTimeLiveSetting()
        {
            this.Minimum = 1;
            this.Maximum = 120;
            this.StepFrequency = 1;
            this.Style = Application.Current.Resources["DefaultSliderStyle"] as Style;

            int val = CacheSettingsManager.GetMemoryCacheTimeLiveMinutes();
            this.Value = val;

            this.ValueChanged += OnValueChanged;

            AutomationProperties.SetName(this, "Время жизни кеша в памяти");
            AutomationProperties.SetHelpText(this, "Время в минутах, через которое записи в кеше памяти считаются устаревшими");
        }

        private void OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            Task.Run(() =>
            {
                CacheSettingsManager.SetMemoryCacheTimeLiveMinutes((int)e.NewValue);
            });
        }
    }
}