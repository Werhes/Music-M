using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text;

namespace VK_UI3.Views
{
    public sealed partial class ListenTogetherErrorWindow : ContentDialog
    {
        public ListenTogetherErrorWindow(string errorMessage, string technicalDetails = null)
        {
            this.InitializeComponent();

            var logBuilder = new StringBuilder();
            logBuilder.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Ошибка совместного прослушивания");
            logBuilder.AppendLine();

            if (!string.IsNullOrEmpty(errorMessage))
            {
                logBuilder.AppendLine("Сообщение:");
                logBuilder.AppendLine(errorMessage);
                logBuilder.AppendLine();
            }

            if (!string.IsNullOrEmpty(technicalDetails))
            {
                logBuilder.AppendLine("Технические детали:");
                logBuilder.AppendLine(technicalDetails);
                logBuilder.AppendLine();
            }

            logBuilder.AppendLine("Системная информация:");
            logBuilder.AppendLine($"ОС: {Environment.OSVersion}");
            logBuilder.AppendLine($"Время: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            LogTextBlock.Text = logBuilder.ToString();

            this.PrimaryButtonClick += (s, e) =>
            {
                this.Hide();
            };

            this.SecondaryButtonClick += (s, e) =>
            {
                CopyLogsToClipboard();
            };
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
    }
}