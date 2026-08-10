using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MusicX.Core.Services;
using MusicX.Services;
using System;
using System.Text;
using System.Threading.Tasks;
using VK_UI3.ViewModels.Controls;
using VK_UI3.Views;

namespace VK_UI3.Controls
{
    public sealed partial class ListenTogetherControl : UserControl
    {
        public ListenTogetherControlViewModel ViewModel { get; }

        public ListenTogetherControl()
        {
            this.InitializeComponent();
            ViewModel = StaticService.Container.GetRequiredService<ListenTogetherControlViewModel>();
            this.DataContext = ViewModel;

            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ListenTogetherControlViewModel.IsConnected))
                {
                    UpdateVisibility();
                }
            };

            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            ConnectedPanel.Visibility = ViewModel.IsConnected ? Visibility.Visible : Visibility.Collapsed;
            DisconnectedPanel.Visibility = ViewModel.IsConnected ? Visibility.Collapsed : Visibility.Visible;
        }

        public static Visibility InvertVisibility(bool value)
        {
            return value ? Visibility.Collapsed : Visibility.Visible;
        }

        public static string GetStopButtonText(bool isSessionHost)
        {
            return isSessionHost ? "Завершить сессию" : "Отключиться";
        }

        private async void StartSessionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ViewModel.StartSessionAsync();
                ShowNotification($"Сессия создана! ID: {ViewModel.Service.SessionId}\nID скопирован в буфер обмена.");

                // Копируем ID сессии в буфер обмена
                try
                {
                    var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                    dataPackage.SetText(ViewModel.Service.SessionId);
                    Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
                }
                catch { }
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("Ошибка создания сессии", ex);
            }
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ViewModel.ConnectToSessionAsync(SessionIdBox.Text);
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("Ошибка подключения к сессии", ex);
            }
        }

        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ViewModel.StopAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("Ошибка", ex);
            }
        }

        private async void OpenLinkButton_Click(object sender, RoutedEventArgs e)
        {
            ShowNotification($"ID сессии: {ViewModel.Service.SessionId}\nID скопирован в буфер обмена.");

            try
            {
                var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dataPackage.SetText(ViewModel.Service.SessionId);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            }
            catch { }
        }

        private async Task ShowErrorDialogAsync(string title, Exception ex)
        {
            // Собираем технические детали
            var detailsBuilder = new StringBuilder();
            detailsBuilder.AppendLine($"Тип исключения: {ex.GetType().FullName}");

            if (ex is System.Net.Http.HttpRequestException httpEx)
            {
                detailsBuilder.AppendLine($"HTTP статус: {httpEx.StatusCode}");
            }

            detailsBuilder.AppendLine($"Сообщение: {ex.Message}");
            detailsBuilder.AppendLine();

            // Inner exception
            if (ex.InnerException != null)
            {
                detailsBuilder.AppendLine("Внутреннее исключение:");
                detailsBuilder.AppendLine($"  Тип: {ex.InnerException.GetType().FullName}");
                detailsBuilder.AppendLine($"  Сообщение: {ex.InnerException.Message}");
                detailsBuilder.AppendLine();
            }

            detailsBuilder.AppendLine("Stack Trace:");
            detailsBuilder.AppendLine(ex.StackTrace ?? "N/A");

            // Информация о сервере
            detailsBuilder.AppendLine();
            detailsBuilder.AppendLine("Информация о подключении:");
            try
            {
                var connectionService = StaticService.Container.GetService<BackendConnectionService>();
                if (connectionService != null)
                {
                    detailsBuilder.AppendLine($"Сервер: {connectionService.GetType().Name}");
                }
            }
            catch { }

            var errorDialog = new ListenTogetherErrorWindow(title, detailsBuilder.ToString());
            errorDialog.XamlRoot = this.XamlRoot;

            // Закрываем Flyout перед показом диалога
            var flyoutParent = this.Parent;
            while (flyoutParent != null && flyoutParent is not FlyoutPresenter)
            {
                flyoutParent = (flyoutParent as FrameworkElement)?.Parent;
            }

            if (flyoutParent is FlyoutPresenter flyoutPresenter)
            {
                var flyout = flyoutPresenter.Parent as Flyout;
                flyout?.Hide();
            }

            await errorDialog.ShowAsync();
        }

        private async void ShowNotification(string message, bool isError = false)
        {
            NotificationText.Text = message;
            NotificationBorder.Visibility = Visibility.Visible;
            NotificationBorder.Background = isError
                ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DarkRed)
                : (Microsoft.UI.Xaml.Media.SolidColorBrush)Application.Current.Resources["SystemAccentColor"];

            // Автоскрытие через 3 секунды
            await Task.Delay(3000);
            NotificationBorder.Visibility = Visibility.Collapsed;
        }
    }
}