using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VK_UI3.Models;
using VK_UI3.Services;
using Windows.Storage.Streams;

namespace VK_UI3.Views
{
    /// <summary>
    /// ViewModel для элемента магазина (из репозитория)
    /// </summary>
    public class AddonStoreItemViewModel : INotifyPropertyChanged
    {
        private readonly AddonStoreItem _item;
        private readonly AddonStoreService _storeService;
        private bool _isInstalled;
        private string _buttonText = "Установить";
        private BitmapImage _icon;

        public AddonStoreItem Item => _item;
        public string Name => _item.Name;
        public string Author => !string.IsNullOrEmpty(_item.Author) ? $"by {_item.Author}" : "";
        public string Version => !string.IsNullOrEmpty(_item.Version) ? $"v{_item.Version}" : "";
        public string Description => _item.Description ?? "Описание отсутствует";
        public string FolderName => _item.FolderName;
        public AddonStoreItemType Type => _item.Type;
        public BitmapImage Icon
        {
            get => _icon;
            set { _icon = value; OnPropertyChanged(); }
        }

        public string ButtonText
        {
            get => _buttonText;
            set { _buttonText = value; OnPropertyChanged(); }
        }

        public bool IsInstalled
        {
            get => _isInstalled;
            set { _isInstalled = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public AddonStoreItemViewModel(AddonStoreItem item, AddonStoreService storeService)
        {
            _item = item;
            _storeService = storeService;
            _isInstalled = item.IsInstalled;
            UpdateButtonText();
        }

        public void UpdateButtonText()
        {
            if (_isInstalled)
                ButtonText = _item.Type == AddonStoreItemType.Theme ? "Применить" : "Установлено";
            else
                ButtonText = "Установить";
        }

        public async Task LoadIconAsync()
        {
            try
            {
                using var httpClient = new HttpClient();
                var imageData = await httpClient.GetByteArrayAsync(_item.IconUrl);

                using var stream = new InMemoryRandomAccessStream();
                using var writer = new DataWriter(stream);
                writer.WriteBytes(imageData);
                await writer.StoreAsync();
                stream.Seek(0);

                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(stream);
                Icon = bitmap;
            }
            catch
            {
                // Иконка не загрузилась — показываем заглушку
            }
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// ViewModel для установленного аддона
    /// </summary>
    public class InstalledAddonViewModel : INotifyPropertyChanged
    {
        private readonly LoadedAddon _loadedAddon;
        private readonly AddonManager _addonManager;
        private BitmapImage _icon;

        public string Name => _loadedAddon.Addon.Name ?? Path.GetFileName(_loadedAddon.FolderPath);
        public string Author => !string.IsNullOrEmpty(_loadedAddon.Addon.Author) ? $"by {_loadedAddon.Addon.Author}" : "";
        public string Version => !string.IsNullOrEmpty(_loadedAddon.Addon.Version) ? $"v{_loadedAddon.Addon.Version}" : "";
        public string Description => _loadedAddon.Addon.Description ?? "Описание отсутствует";
        public string FolderName => Path.GetFileName(_loadedAddon.FolderPath);

        public BitmapImage Icon
        {
            get => _icon;
            set { _icon = value; OnPropertyChanged(); }
        }

        public string ButtonText => "Удалить";

        public event PropertyChangedEventHandler PropertyChanged;

        public InstalledAddonViewModel(LoadedAddon loadedAddon, AddonManager addonManager)
        {
            _loadedAddon = loadedAddon;
            _addonManager = addonManager;
        }

        public async Task LoadIconAsync()
        {
            try
            {
                var iconPath = Path.Combine(_loadedAddon.FolderPath, "icon.png");
                if (File.Exists(iconPath))
                {
                    using var stream = File.OpenRead(iconPath);
                    var memStream = new InMemoryRandomAccessStream();
                    var writer = new DataWriter(memStream);
                    writer.WriteBytes(await File.ReadAllBytesAsync(iconPath));
                    await writer.StoreAsync();
                    memStream.Seek(0);

                    var bitmap = new BitmapImage();
                    await bitmap.SetSourceAsync(memStream);
                    Icon = bitmap;
                }
            }
            catch { }
        }

        public async Task<bool> UninstallAsync()
        {
            return await _addonManager.UninstallAddonAsync(FolderName);
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// ViewModel для установленной темы
    /// </summary>
    public class InstalledThemeViewModel : INotifyPropertyChanged
    {
        private readonly InstalledTheme _theme;
        private readonly ThemeManager _themeManager;
        private BitmapImage _icon;

        public string Name => _theme.Name;
        public string Author => !string.IsNullOrEmpty(_theme.Author) ? $"by {_theme.Author}" : "";
        public string Version => !string.IsNullOrEmpty(_theme.Version) ? $"v{_theme.Version}" : "";
        public string Description => _theme.Description ?? "Описание отсутствует";
        public string FolderName => _theme.FolderName;
        public bool IsActive => _theme.IsActive;

        public string ButtonText => _theme.IsActive ? "Применена" : "Применить";

        public BitmapImage Icon
        {
            get => _icon;
            set { _icon = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public InstalledThemeViewModel(InstalledTheme theme, ThemeManager themeManager)
        {
            _theme = theme;
            _themeManager = themeManager;
        }

        public async Task LoadIconAsync()
        {
            try
            {
                if (File.Exists(_theme.IconPath))
                {
                    using var stream = File.OpenRead(_theme.IconPath);
                    var memStream = new InMemoryRandomAccessStream();
                    var writer = new DataWriter(memStream);
                    writer.WriteBytes(await File.ReadAllBytesAsync(_theme.IconPath));
                    await writer.StoreAsync();
                    memStream.Seek(0);

                    var bitmap = new BitmapImage();
                    await bitmap.SetSourceAsync(memStream);
                    Icon = bitmap;
                }
            }
            catch { }
        }

        public async Task<bool> ApplyAsync()
        {
            _themeManager.ScanInstalledThemes();
            return await _themeManager.ApplyThemeAsync(FolderName);
        }

        public async Task<bool> UninstallAsync()
        {
            return await _themeManager.UninstallThemeAsync(FolderName);
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// Страница "Магазин дополнений" с тремя табами
    /// </summary>
    public sealed partial class AddonStorePage : Page, INotifyPropertyChanged
    {
        private readonly AddonStoreService _storeService;
        private readonly AddonManager _addonManager;
        private readonly ThemeManager _themeManager;
        private ObservableCollection<AddonStoreItemViewModel> _storeAddons = new();
        private ObservableCollection<AddonStoreItemViewModel> _storeThemes = new();
        private ObservableCollection<InstalledAddonViewModel> _installedAddons = new();
        private ObservableCollection<InstalledThemeViewModel> _installedThemes = new();
        private bool _isLoading = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public AddonStorePage()
        {
            this.InitializeComponent();
            _storeService = new AddonStoreService();
            _addonManager = new AddonManager();
            _themeManager = new ThemeManager();

            this.Loaded += OnPageLoaded;
        }

        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            // Загружаем установленные элементы сразу
            LoadInstalledItems();

            // Загружаем магазин
            await LoadStoreItemsAsync();
        }

        /// <summary>
        /// Загрузить список установленных аддонов и тем
        /// </summary>
        private void LoadInstalledItems()
        {
            try
            {
                // Загружаем установленные аддоны
                _installedAddons.Clear();
                foreach (var loaded in _addonManager.LoadedAddons)
                {
                    var vm = new InstalledAddonViewModel(loaded, _addonManager);
                    _installedAddons.Add(vm);
                    _ = vm.LoadIconAsync();
                }

                // Загружаем установленные темы
                _installedThemes.Clear();
                _themeManager.ScanInstalledThemes();
                foreach (var theme in _themeManager.InstalledThemes)
                {
                    var vm = new InstalledThemeViewModel(theme, _themeManager);
                    _installedThemes.Add(vm);
                    _ = vm.LoadIconAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AddonStore] Ошибка загрузки установленных элементов: {ex.Message}");
            }
        }

        /// <summary>
        /// Загрузить элементы из магазина (репозитория GitHub)
        /// </summary>
        private async Task LoadStoreItemsAsync()
        {
            if (_isLoading) return;
            _isLoading = true;

            ShowLoading(true);
            EmptyMessage.Visibility = Visibility.Collapsed;

            try
            {
                var addonsTask = _storeService.GetAddonsAsync();
                var themesTask = _storeService.GetThemesAsync();

                await Task.WhenAll(addonsTask, themesTask);

                _storeAddons.Clear();
                _storeThemes.Clear();

                foreach (var item in addonsTask.Result)
                {
                    var vm = new AddonStoreItemViewModel(item, _storeService);
                    _storeAddons.Add(vm);
                    _ = vm.LoadIconAsync();
                }

                foreach (var item in themesTask.Result)
                {
                    var vm = new AddonStoreItemViewModel(item, _storeService);
                    _storeThemes.Add(vm);
                    _ = vm.LoadIconAsync();
                }

                // Если текущий таб — магазин, показываем элементы
                if (TabNav.SelectedItem == StoreTab)
                {
                    ShowStoreItems();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AddonStore] Ошибка загрузки магазина: {ex.Message}");
                EmptyMessage.Text = "Не удалось загрузить список дополнений.\nПроверьте подключение к интернету.";
                EmptyMessage.Visibility = Visibility.Visible;
            }
            finally
            {
                ShowLoading(false);
                _isLoading = false;
            }
        }

        /// <summary>
        /// Показать элементы магазина (аддоны и темы вместе)
        /// </summary>
        private void ShowStoreItems()
        {
            var combined = new ObservableCollection<object>();
            foreach (var a in _storeAddons) combined.Add(a);
            foreach (var t in _storeThemes) combined.Add(t);

            if (combined.Count == 0)
            {
                EmptyMessage.Text = "Репозиторий дополнений пуст или недоступен.";
                EmptyMessage.Visibility = Visibility.Visible;
                ItemsControl.ItemsSource = null;
            }
            else
            {
                EmptyMessage.Visibility = Visibility.Collapsed;
                ItemsControl.ItemsSource = combined;
            }
        }

        /// <summary>
        /// Показать установленные аддоны
        /// </summary>
        private void ShowInstalledAddons()
        {
            if (_installedAddons.Count == 0)
            {
                EmptyMessage.Text = "Нет установленных аддонов.";
                EmptyMessage.Visibility = Visibility.Visible;
                ItemsControl.ItemsSource = null;
            }
            else
            {
                EmptyMessage.Visibility = Visibility.Collapsed;
                ItemsControl.ItemsSource = _installedAddons;
            }
        }

        /// <summary>
        /// Показать установленные темы
        /// </summary>
        private void ShowInstalledThemes()
        {
            if (_installedThemes.Count == 0)
            {
                EmptyMessage.Text = "Нет установленных тем.";
                EmptyMessage.Visibility = Visibility.Visible;
                ItemsControl.ItemsSource = null;
            }
            else
            {
                EmptyMessage.Visibility = Visibility.Collapsed;
                ItemsControl.ItemsSource = _installedThemes;
            }
        }

        private void ShowLoading(bool show)
        {
            LoadingRing.IsActive = show;
            LoadingRing.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            ContentScroll.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Обработчик переключения табов
        /// </summary>
        private void OnTabSelectionChanged(object sender, NavigationViewSelectionChangedEventArgs e)
        {
            var selectedItem = e.SelectedItem as NavigationViewItem;
            if (selectedItem == null) return;

            var tag = selectedItem.Tag as string;
            switch (tag)
            {
                case "store":
                    ShowStoreItems();
                    break;
                case "installedAddons":
                    LoadInstalledItems();
                    ShowInstalledAddons();
                    break;
                case "installedThemes":
                    LoadInstalledItems();
                    ShowInstalledThemes();
                    break;
            }
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            _ = LoadStoreItemsAsync();
        }

        /// <summary>
        /// Обработчик кнопки README
        /// </summary>
        private async void OnReadmeClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is AddonStoreItemViewModel vm)
            {
                try
                {
                    var readmePath = vm.Type == AddonStoreItemType.Addon
                        ? $"Addons/{vm.FolderName}"
                        : $"Themes/{vm.FolderName}";

                    var content = await _storeService.GetReadmeContentAsync(readmePath);

                    var dialog = new ContentDialog
                    {
                        Title = $"О расширении: {vm.Name}",
                        Content = new ScrollViewer
                        {
                            MaxHeight = 400,
                            Content = new TextBlock
                            {
                                Text = content ?? "Описание отсутствует.",
                                TextWrapping = TextWrapping.Wrap,
                                Margin = new Thickness(0, 0, 0, 12)
                            }
                        },
                        CloseButtonText = "Закрыть",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
                catch
                {
                    ShowNotification("Ошибка", "Не удалось загрузить описание.");
                }
            }
        }

        /// <summary>
        /// Обработчик основной кнопки действия (установить/удалить/применить)
        /// </summary>
        private async void OnActionButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                button.IsEnabled = false;
                try
                {
                    if (button.Tag is AddonStoreItemViewModel storeVm)
                    {
                        await HandleStoreItemAction(storeVm);
                    }
                    else if (button.Tag is InstalledAddonViewModel installedAddonVm)
                    {
                        await HandleInstalledAddonAction(installedAddonVm);
                    }
                    else if (button.Tag is InstalledThemeViewModel installedThemeVm)
                    {
                        await HandleInstalledThemeAction(installedThemeVm);
                    }
                }
                finally
                {
                    button.IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// Обработка действия для элемента магазина (установка/применение)
        /// </summary>
        private async Task HandleStoreItemAction(AddonStoreItemViewModel vm)
        {
            if (vm.IsInstalled && vm.Type == AddonStoreItemType.Theme)
            {
                // Применяем тему
                _themeManager.ScanInstalledThemes();
                var success = await _themeManager.ApplyThemeAsync(vm.FolderName);
                if (success)
                {
                    vm.ButtonText = "Применена";
                    ShowNotification("Тема применена", $"Тема \"{vm.Name}\" успешно применена.");
                }
                return;
            }

            if (vm.IsInstalled) return;

            vm.ButtonText = "Загрузка...";

            bool installSuccess;
            if (vm.Type == AddonStoreItemType.Addon)
            {
                installSuccess = await _addonManager.InstallAddonAsync(vm.Item, _storeService);
            }
            else
            {
                installSuccess = await _themeManager.InstallThemeAsync(vm.Item, _storeService);
            }

            if (installSuccess)
            {
                vm.IsInstalled = true;
                vm.UpdateButtonText();

                // Обновляем список установленных
                LoadInstalledItems();

                if (vm.Type == AddonStoreItemType.Theme)
                {
                    var result = await ShowConfirmDialog("Тема установлена", "Хотите применить тему сейчас?");
                    if (result == ContentDialogResult.Primary)
                    {
                        _themeManager.ScanInstalledThemes();
                        await _themeManager.ApplyThemeAsync(vm.FolderName);
                        vm.ButtonText = "Применена";
                    }
                }
                else
                {
                    ShowNotification("Аддон установлен", $"Аддон \"{vm.Name}\" успешно установлен и активирован.");
                }
            }
            else
            {
                ShowNotification("Ошибка", "Не удалось установить элемент. Попробуйте снова.");
            }
        }

        /// <summary>
        /// Обработка действия для установленного аддона (удаление)
        /// </summary>
        private async Task HandleInstalledAddonAction(InstalledAddonViewModel vm)
        {
            var confirm = await ShowConfirmDialog("Удаление аддона",
                $"Вы уверены, что хотите удалить аддон \"{vm.Name}\"?");
            if (confirm != ContentDialogResult.Primary) return;

            var success = await vm.UninstallAsync();
            if (success)
            {
                ShowNotification("Аддон удалён", $"Аддон \"{vm.Name}\" успешно удалён.");
                LoadInstalledItems();
                ShowInstalledAddons();
            }
            else
            {
                ShowNotification("Ошибка", "Не удалось удалить аддон.");
            }
        }

        /// <summary>
        /// Обработка действия для установленной темы (применить/удалить)
        /// </summary>
        private async Task HandleInstalledThemeAction(InstalledThemeViewModel vm)
        {
            if (!vm.IsActive)
            {
                // Применяем тему
                var success = await vm.ApplyAsync();
                if (success)
                {
                    ShowNotification("Тема применена", $"Тема \"{vm.Name}\" успешно применена.");
                    LoadInstalledItems();
                    ShowInstalledThemes();
                }
                return;
            }

            // Если тема активна — показываем меню: сбросить или удалить
            var action = await ShowThreeButtonDialog("Тема активна",
                $"Тема \"{vm.Name}\" сейчас активна. Что сделать?",
                "Сбросить тему", "Удалить тему", "Отмена");

            if (action == "reset")
            {
                _themeManager.ResetTheme();
                ShowNotification("Тема сброшена", "Тема сброшена на стандартную.");
                LoadInstalledItems();
                ShowInstalledThemes();
            }
            else if (action == "uninstall")
            {
                var confirm = await ShowConfirmDialog("Удаление темы",
                    $"Вы уверены, что хотите удалить тему \"{vm.Name}\"?");
                if (confirm != ContentDialogResult.Primary) return;

                var success = await vm.UninstallAsync();
                if (success)
                {
                    ShowNotification("Тема удалена", $"Тема \"{vm.Name}\" успешно удалена.");
                    LoadInstalledItems();
                    ShowInstalledThemes();
                }
                else
                {
                    ShowNotification("Ошибка", "Не удалось удалить тему.");
                }
            }
        }

        private void ShowNotification(string title, string message)
        {
            try
            {
                MainWindow.dispatcherQueue.TryEnqueue(() =>
                {
                    new Notification.Notification(title, message);
                });
            }
            catch { }
        }

        private async Task<ContentDialogResult> ShowConfirmDialog(string title, string content)
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = content,
                    PrimaryButtonText = "Да",
                    CloseButtonText = "Отмена",
                    XamlRoot = this.XamlRoot
                };
                return await dialog.ShowAsync();
            }
            catch
            {
                return ContentDialogResult.None;
            }
        }

        private async Task<string> ShowThreeButtonDialog(string title, string content,
            string primaryText, string secondaryText, string closeText)
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = content,
                    PrimaryButtonText = primaryText,
                    SecondaryButtonText = secondaryText,
                    CloseButtonText = closeText,
                    XamlRoot = this.XamlRoot
                };
                var result = await dialog.ShowAsync();
                return result switch
                {
                    ContentDialogResult.Primary => "reset",
                    ContentDialogResult.Secondary => "uninstall",
                    _ => "cancel"
                };
            }
            catch
            {
                return "cancel";
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}