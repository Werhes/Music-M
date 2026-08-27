using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Threading.Tasks;
using VK_UI3.DB;
using VK_UI3.Helpers;
using VK_UI3.Services;
using VK_UI3.Views;

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

            // Кроссфейд — начальные значения
            var cf = SettingsTable.GetSetting("crossfadeEnabled");
            crossfadeToggle.IsChecked = cf == null || cf.settingValue != "0";
            crossfadeDuration.Value = GetCrossfadeDurationFromSettings();

            // Автообновление — начальное значение
            var au = SettingsTable.GetSetting("autoUpdateEnabled");
            autoUpdateToggle.IsChecked = au == null || au.settingValue != "0";

            // Защита кодом — обновляем состояние кнопок
            UpdateLockUi();

            // Устанавливаем начальные значения текста
            UpdateMaxDownloadsText();
            UpdateMaxFilesText();
            UpdateMemoryTimeLiveText();
            UpdateCrossfadeDurationText();
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

        private void AutoUpdateToggle_Changed(object sender, RoutedEventArgs e)
        {
            SettingsTable.SetSetting("autoUpdateEnabled", (autoUpdateToggle.IsChecked == true) ? "1" : "0");
        }

        private void CrossfadeToggle_Changed(object sender, RoutedEventArgs e)
        {
            SettingsTable.SetSetting("crossfadeEnabled", (crossfadeToggle.IsChecked == true) ? "1" : "0");
        }

        private void CrossfadeDuration_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            SettingsTable.SetSetting("crossfadeDurationMs", ((int)crossfadeDuration.Value).ToString());
            UpdateCrossfadeDurationText();
        }

        private int GetCrossfadeDurationFromSettings()
        {
            var s = SettingsTable.GetSetting("crossfadeDurationMs");
            if (s == null || !int.TryParse(s.settingValue, out int v))
                return 1200;
            return Math.Clamp(v, 300, 5000);
        }

        private void UpdateCrossfadeDurationText()
        {
            // Обработчик ValueChanged может вызываться во время InitializeComponent,
            // когда crossfadeDurationValue ещё не создан — просто пропускаем.
            if (crossfadeDuration == null || crossfadeDurationValue == null)
                return;

            crossfadeDurationValue.Text = ((int)crossfadeDuration.Value).ToString() + " мс";
        }

        private void UpdateLockUi()
        {
            bool enabled = LockManager.IsPinEnabled();
            LockStatusText.Text = enabled ? "Код установлен" : "Код не установлен";
            SetLockButton.Visibility = enabled ? Visibility.Collapsed : Visibility.Visible;
            ChangeLockButton.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
            DeleteLockButton.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        }

        private async Task<string?> PromptPinAsync(string title, string placeholder)
        {
            var pw = new PasswordBox { PlaceholderText = placeholder };
            var dialog = new ContentDialog
            {
                Title = title,
                Content = pw,
                PrimaryButtonText = "ОК",
                CloseButtonText = "Отмена",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary ? pw.Password : null;
        }

        private async Task ShowMessageAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Music M",
                Content = message,
                CloseButtonText = "ОК",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async void SetLockButton_Click(object sender, RoutedEventArgs e)
        {
            var pin = await PromptPinAsync("Установить код", "Придумайте код");
            if (string.IsNullOrEmpty(pin)) return;

            var confirm = await PromptPinAsync("Повторите код", "Повторите код");
            if (pin != confirm)
            {
                await ShowMessageAsync("Коды не совпадают.");
                return;
            }

            LockManager.SetPin(pin);
            UpdateLockUi();
            await ShowMessageAsync("Код установлен.");
        }

        private async void ChangeLockButton_Click(object sender, RoutedEventArgs e)
        {
            var current = await PromptPinAsync("Изменить код", "Введите текущий код");
            if (current == null) return;

            if (!LockManager.VerifyPin(current))
            {
                await ShowMessageAsync("Неверный текущий код.");
                return;
            }

            var newPin = await PromptPinAsync("Новый код", "Придумайте новый код");
            if (string.IsNullOrEmpty(newPin)) return;

            var confirm = await PromptPinAsync("Повторите новый код", "Повторите новый код");
            if (newPin != confirm)
            {
                await ShowMessageAsync("Коды не совпадают.");
                return;
            }

            LockManager.SetPin(newPin);
            UpdateLockUi();
            await ShowMessageAsync("Код изменён.");
        }

        private async void DeleteLockButton_Click(object sender, RoutedEventArgs e)
        {
            var current = await PromptPinAsync("Удалить код", "Введите текущий код");
            if (current == null) return;

            if (!LockManager.VerifyPin(current))
            {
                await ShowMessageAsync("Неверный код.");
                return;
            }

            LockManager.ClearPin();
            UpdateLockUi();
            await ShowMessageAsync("Код удалён.");
        }

        private async void ViewLogsButton_Click(object sender, RoutedEventArgs e)
        {
            var logViewer = new LogViewerWindow();
            logViewer.XamlRoot = this.XamlRoot;
            await logViewer.ShowAsync();
        }

        private async void LaunchMessengerButton_Click(object sender, RoutedEventArgs e)
        {
            LaunchMessengerButton.IsEnabled = false;
            MessengerStatusText.Text = "Запуск мессенджера…";
            try
            {
                MessengerStatusText.Text = await MessengerModeService.LaunchMessengerAsync();

                // Мессенджер открылся — прячем Music M в трей.
                if (MessengerStatusText.Text.Contains("запущен"))
                {
                    MainWindow.mainWindow?.HideFromTaskbar();
                }
            }
            catch (Exception ex)
            {
                MessengerStatusText.Text = "Ошибка: " + ex.Message;
            }
            finally
            {
                LaunchMessengerButton.IsEnabled = true;
            }
        }
    }
}
