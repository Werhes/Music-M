using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VK_UI3.Models;
using VK_UI3.Services;
using Windows.Storage.Streams;

namespace VK_UI3.Views.Controls
{
    /// <summary>
    /// Контрол для отображения элемента магазина (аддон или тема)
    /// </summary>
    public sealed partial class AddonStoreItemControl : UserControl, INotifyPropertyChanged
    {
        private readonly AddonStoreService _storeService;
        private readonly AddonManager _addonManager;
        private readonly ThemeManager _themeManager;
        private AddonStoreItem _item;
        private bool _isInstalled;
        private bool _isLoading;

        public event PropertyChangedEventHandler PropertyChanged;

        public AddonStoreItem Item => _item;
        public bool IsInstalled => _isInstalled;

        public AddonStoreItemControl()
        {
            this.InitializeComponent();
            _storeService = new AddonStoreService();
            _addonManager = new AddonManager();
            _themeManager = new ThemeManager();
        }

        /// <summary>
        /// Установить данные элемента
        /// </summary>
        public async Task SetItemAsync(AddonStoreItem item)
        {
            _item = item;
            _isInstalled = item.IsInstalled;

            // Заполняем UI
            ItemName.Text = item.Name;
            ItemAuthor.Text = !string.IsNullOrEmpty(item.Author) ? $"by {item.Author}" : "";
            ItemVersion.Text = !string.IsNullOrEmpty(item.Version) ? $"v{item.Version}" : "";
            ItemDescription.Text = item.Description ?? "Описание отсутствует";

            UpdateButtonState();

            // Загружаем иконку
            await LoadIconAsync(item.IconUrl);
        }

        /// <summary>
        /// Загрузить иконку из URL
        /// </summary>
        private async Task LoadIconAsync(string iconUrl)
        {
            try
            {
                using var httpClient = new HttpClient();
                var imageData = await httpClient.GetByteArrayAsync(iconUrl);

                using var stream = new InMemoryRandomAccessStream();
                using var writer = new DataWriter(stream);
                writer.WriteBytes(imageData);
                await writer.StoreAsync();
                stream.Seek(0);

                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(stream);
                ItemIcon.Source = bitmap;
            }
            catch
            {
                // Если иконка не загрузилась, показываем заглушку
                ItemIcon.Source = null;
            }
        }

        /// <summary>
        /// Обновить состояние кнопки в зависимости от статуса установки
        /// </summary>
        private void UpdateButtonState()
        {
            if (_isLoading)
            {
                ActionButton.Content = "Загрузка...";
                ActionButton.IsEnabled = false;
                return;
            }

            if (_isInstalled)
            {
                if (_item.Type == AddonStoreItemType.Theme)
                {
                    ActionButton.Content = "Применить";
                    ActionButton.IsEnabled = true;
                }
                else
                {
                    ActionButton.Content = "Установлено";
                    ActionButton.IsEnabled = false;
                }
            }
            else
            {
                ActionButton.Content = "Установить";
                ActionButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку действия
        /// </summary>
        private async void OnActionButtonClick(object sender, RoutedEventArgs e)
        {
            if (_item == null) return;

            if (_isInstalled && _item.Type == AddonStoreItemType.Theme)
            {
                // Применяем тему
                await ApplyThemeAsync();
                return;
            }

            if (_isInstalled) return;

            _isLoading = true;
            UpdateButtonState();

            try
            {
                bool success;
                if (_item.Type == AddonStoreItemType.Addon)
                {
                    success = await _addonManager.InstallAddonAsync(_item, _storeService);
                }
                else
                {
                    success = await _themeManager.InstallThemeAsync(_item, _storeService);
                }

                if (success)
                {
                    _isInstalled = true;
                    _item.IsInstalled = true;

                    // Если это тема, предлагаем применить
                    if (_item.Type == AddonStoreItemType.Theme)
                    {
                        var dialog = new ContentDialog
                        {
                            Title = "Тема установлена",
                            Content = "Хотите применить тему сейчас?",
                            PrimaryButtonText = "Применить",
                            CloseButtonText = "Позже",
                            XamlRoot = this.XamlRoot
                        };

                        var result = await dialog.ShowAsync();
                        if (result == ContentDialogResult.Primary)
                        {
                            await ApplyThemeAsync();
                        }
                    }
                }
                else
                {
                    ShowErrorDialog("Не удалось установить элемент. Попробуйте снова.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка установки: {ex.Message}");
                ShowErrorDialog($"Ошибка установки: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
                UpdateButtonState();
            }
        }

        /// <summary>
        /// Применить тему
        /// </summary>
        private async Task ApplyThemeAsync()
        {
            _themeManager.ScanInstalledThemes();
            var success = await _themeManager.ApplyThemeAsync(_item.FolderName);

            if (success)
            {
                ActionButton.Content = "Применена";
                var notification = new VK_UI3.Views.Notification.Notification(
                    "Тема применена",
                    $"Тема \"{_item.Name}\" успешно применена."
                );
            }
            else
            {
                ShowErrorDialog("Не удалось применить тему.");
            }
        }

        /// <summary>
        /// Показать диалог с ошибкой
        /// </summary>
        private async void ShowErrorDialog(string message)
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = "Ошибка",
                    Content = message,
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
            catch { }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}