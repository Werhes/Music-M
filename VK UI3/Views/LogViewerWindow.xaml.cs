using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using VK_UI3.Services;

namespace VK_UI3.Views
{
    public sealed partial class LogViewerWindow : ContentDialog
    {
        public LogViewerWindow()
        {
            this.InitializeComponent();

            LoadLogs();

            this.PrimaryButtonClick += (s, e) =>
            {
                this.Hide();
            };

            this.SecondaryButtonClick += (s, e) =>
            {
                CopyLogsToClipboard();
            };
        }

        private void LoadLogs()
        {
            LogTextBlock.Text = AppLogService.Instance.GetAllLogs();

            // Прокручиваем вниз к последним логам
            if (LogScrollViewer != null)
            {
                LogScrollViewer.ChangeView(null, LogScrollViewer.ScrollableHeight, null);
            }
        }

        private void CopyLogsToClipboard()
        {
            try
            {
                var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dataPackage.SetText(LogTextBlock.Text);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            }
            catch { }
        }

        private void ClearLogsButton_Click(object sender, RoutedEventArgs e)
        {
            AppLogService.Instance.Clear();
            LoadLogs();
        }
    }
}