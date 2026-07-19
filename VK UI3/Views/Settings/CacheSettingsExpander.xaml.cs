using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;

namespace VK_UI3.Views.Settings
{
    /// <summary>
    /// Expander для группировки всех настроек кеширования
    /// </summary>
    public sealed partial class CacheSettingsExpander : Expander
    {
        public CacheSettingsExpander()
        {
            this.InitializeComponent();

            AutomationProperties.SetName(this, "Настройки кеширования");
            AutomationProperties.SetHelpText(this, "Настройки кеширования изображений и данных в памяти");
        }
    }
}