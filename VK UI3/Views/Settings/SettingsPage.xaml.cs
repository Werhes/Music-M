using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VK_UI3.Views.Settings
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            this.Loaded += SettingsPage_Loaded;
            
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Подписываемся на изменения значений слайдеров для обновления текста
            maxDownloads.ValueChanged += MaxDownloads_ValueChanged;
            maxFiles.ValueChanged += MaxFiles_ValueChanged;
            memoryTimeLive.ValueChanged += MemoryTimeLive_ValueChanged;
            trackCacheMaxSize.ValueChanged += TrackCacheMaxSize_ValueChanged;

            // Устанавливаем начальные значения текста
            UpdateMaxDownloadsText();
            UpdateMaxFilesText();
            UpdateMemoryTimeLiveText();
            UpdateTrackCacheMaxSizeText();
        }

        private void MaxDownloads_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            UpdateMaxDownloadsText();
        }

        private void MaxFiles_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            UpdateMaxFilesText();
        }

        private void MemoryTimeLive_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            UpdateMemoryTimeLiveText();
        }

        private void UpdateMaxDownloadsText()
        {
            maxDownloadsValue.Text = ((int)maxDownloads.Value).ToString();
        }

        private void UpdateMaxFilesText()
        {
            maxFilesValue.Text = ((int)maxFiles.Value).ToString();
        }

        private void UpdateMemoryTimeLiveText()
        {
            memoryTimeLiveValue.Text = ((int)memoryTimeLive.Value).ToString();
        }

        private void TrackCacheMaxSize_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            UpdateTrackCacheMaxSizeText();
        }

        private void UpdateTrackCacheMaxSizeText()
        {
            int sizeMb = (int)trackCacheMaxSize.Value;
            trackCacheMaxSizeValue.Text = $"{sizeMb / 1000.0:F1} ГБ";
        }
    }
}
