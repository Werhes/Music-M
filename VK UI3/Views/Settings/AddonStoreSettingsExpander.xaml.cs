using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using VK_UI3.Services;

namespace VK_UI3.Views.Settings
{
    /// <summary>
    /// Expander для управления установленными аддонами и темами в настройках
    /// </summary>
    public sealed partial class AddonStoreSettingsExpander : Expander
    {
        private readonly AddonManager _addonManager;
        private readonly ThemeManager _themeManager;
        private readonly ObservableCollection<AddonItemViewModel> _installedAddons = new();
        private readonly ObservableCollection<ThemeItemViewModel> _installedThemes = new();

        public AddonStoreSettingsExpander()
        {
            this.InitializeComponent();
            _addonManager = AddonManager.Instance;
            _themeManager = ThemeManager.Instance;

            InstalledAddonsList.ItemsSource = _installedAddons;
            InstalledThemesList.ItemsSource = _installedThemes;

            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            RefreshInstalledItems();
        }

        /// <summary>
        /// Обновить список установленных элементов
        /// </summary>
        public void RefreshInstalledItems()
        {
            // Обновляем аддоны
            _installedAddons.Clear();

            // Сначала успешно загруженные IAddon
            foreach (var loaded in _addonManager.LoadedAddons)
            {
                _installedAddons.Add(new AddonItemViewModel
                {
                    Name = loaded.Addon.Name,
                    Version = loaded.Addon.Version,
                    Id = loaded.Addon.Id,
                    FolderName = System.IO.Path.GetFileName(loaded.FolderPath)
                });
            }

            // Затем установленные, но не загруженные (есть папка, но нет IAddon)
            _addonManager.ScanInstalledAddons();
            foreach (var folder in _addonManager.InstalledAddonFolders)
            {
                if (folder.IsLoaded) continue; // уже добавлен выше

                _installedAddons.Add(new AddonItemViewModel
                {
                    Name = folder.FolderName,
                    Version = "",
                    Id = folder.FolderName,
                    FolderName = folder.FolderName
                });
            }

            NoAddonsMessage.Visibility = _installedAddons.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            // Обновляем темы
            _installedThemes.Clear();
            _themeManager.ScanInstalledThemes();
            foreach (var theme in _themeManager.InstalledThemes)
            {
                _installedThemes.Add(new ThemeItemViewModel
                {
                    Name = theme.Name,
                    Version = theme.Version,
                    FolderName = theme.FolderName,
                    IsActive = theme.IsActive
                });
            }
            NoThemesMessage.Visibility = _installedThemes.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        /// <summary>
        /// Открыть страницу магазина дополнений
        /// </summary>
        private void OnOpenStoreClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // Находим MainView и навигируемся к странице магазина
                if (MainView.mainView != null)
                {
                    MainView.mainView.hideSearch();
                    var frame = MainView.mainView.ContentFramePublic;
                    if (frame != null)
                    {
                        frame.Navigate(typeof(AddonStorePage), null,
                            new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка навигации в магазин: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// ViewModel для отображения аддона в списке
    /// </summary>
    public class AddonItemViewModel
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Id { get; set; }
        public string FolderName { get; set; }
    }

    /// <summary>
    /// ViewModel для отображения темы в списке
    /// </summary>
    public class ThemeItemViewModel
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string FolderName { get; set; }
        public bool IsActive { get; set; }
    }
}