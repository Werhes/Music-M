using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MusicX.Services;
using VK_UI3.Models;

namespace VK_UI3.Services
{
    /// <summary>
    /// Менеджер для управления установленными темами.
    /// Загружает XAML ResourceDictionary тем и применяет их.
    /// </summary>
    public class ThemeManager
    {
        private readonly string _themesDirectory;
        private readonly List<InstalledTheme> _installedThemes = new();
        private InstalledTheme _currentTheme;

        /// <summary>
        /// Статический синглтон для использования во всём приложении
        /// </summary>
        public static ThemeManager Instance { get; } = new ThemeManager();

        public IReadOnlyList<InstalledTheme> InstalledThemes => _installedThemes.AsReadOnly();
        public InstalledTheme CurrentTheme => _currentTheme;

        public event EventHandler<ThemeEventArgs> ThemeApplied;
        public event EventHandler<ThemeEventArgs> ThemeError;

        public ThemeManager()
        {
            _themesDirectory = Path.Combine(StaticService.UserDataFolder.FullName, "Themes");
            Directory.CreateDirectory(_themesDirectory);
        }

        /// <summary>
        /// Загрузить список установленных тем
        /// </summary>
        public void ScanInstalledThemes()
        {
            _installedThemes.Clear();

            if (!Directory.Exists(_themesDirectory))
                return;

            var themeFolders = Directory.GetDirectories(_themesDirectory);

            foreach (var folder in themeFolders)
            {
                try
                {
                    var folderName = Path.GetFileName(folder);
                    var xamlFiles = Directory.GetFiles(folder, "*.xaml");

                    if (xamlFiles.Length == 0) continue;

                    var theme = new InstalledTheme
                    {
                        FolderName = folderName,
                        Name = folderName,
                        FolderPath = folder,
                        ThemeFile = xamlFiles[0],
                        IconPath = Path.Combine(folder, "icon.png"),
                        ReadmePath = Path.Combine(folder, "README.md")
                    };

                    // Пытаемся загрузить манифест
                    var manifestPath = Path.Combine(folder, "theme.json");
                    if (File.Exists(manifestPath))
                    {
                        try
                        {
                            var json = File.ReadAllText(manifestPath);
                            var manifest = System.Text.Json.JsonSerializer.Deserialize<ThemeManifest>(json);
                            if (manifest != null)
                            {
                                theme.Name = manifest.Name ?? folderName;
                                theme.Author = manifest.Author;
                                theme.Version = manifest.Version;
                                theme.Description = manifest.Description;
                                theme.IsDark = manifest.IsDark;
                            }
                        }
                        catch { }
                    }

                    // Если нет манифеста, читаем README
                    if (string.IsNullOrEmpty(theme.Description) && File.Exists(theme.ReadmePath))
                    {
                        var readme = File.ReadAllText(theme.ReadmePath);
                        var lines = readme.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            var trimmed = line.Trim().TrimStart('#').Trim();
                            if (!string.IsNullOrEmpty(trimmed) && trimmed.Length > 10)
                            {
                                theme.Description = trimmed.Length > 150
                                    ? trimmed[..150] + "..."
                                    : trimmed;
                                break;
                            }
                        }
                    }

                    _installedThemes.Add(theme);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка сканирования темы {folder}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Применить тему по имени папки
        /// </summary>
        public async Task<bool> ApplyThemeAsync(string folderName)
        {
            var theme = _installedThemes.FirstOrDefault(t =>
                t.FolderName.Equals(folderName, StringComparison.OrdinalIgnoreCase));

            if (theme == null)
            {
                ThemeError?.Invoke(this, new ThemeEventArgs(null, folderName, "Тема не найдена"));
                return false;
            }

            return await ApplyThemeAsync(theme);
        }

        /// <summary>
        /// Применить тему
        /// </summary>
        public async Task<bool> ApplyThemeAsync(InstalledTheme theme)
        {
            try
            {
                if (!File.Exists(theme.ThemeFile))
                {
                    ThemeError?.Invoke(this, new ThemeEventArgs(theme, theme.Name, "Файл темы не найден"));
                    return false;
                }

                // Выгружаем предыдущую тему
                if (_currentTheme != null)
                {
                    RemoveThemeResources(_currentTheme);
                }

                // Загружаем новую тему
                var resourceDictionary = new Microsoft.UI.Xaml.ResourceDictionary
                {
                    Source = new Uri(theme.ThemeFile)
                };

                // Добавляем ресурсы темы в приложение
                Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);

                _currentTheme = theme;
                theme.IsActive = true;

                // Сохраняем выбранную тему в настройках
                DB.SettingsTable.SetSetting("currentTheme", theme.FolderName);

                ThemeApplied?.Invoke(this, new ThemeEventArgs(theme));

                return true;
            }
            catch (Exception ex)
            {
                ThemeError?.Invoke(this, new ThemeEventArgs(theme, theme.Name, $"Ошибка применения: {ex.Message}"));
                return false;
            }
        }

        /// <summary>
        /// Сбросить тему (удалить все ресурсы темы)
        /// </summary>
        public void ResetTheme()
        {
            if (_currentTheme != null)
            {
                RemoveThemeResources(_currentTheme);
                _currentTheme.IsActive = false;
                _currentTheme = null;
                DB.SettingsTable.RemoveSetting("currentTheme");
                ThemeApplied?.Invoke(this, new ThemeEventArgs(null, "Стандартная тема"));
            }
        }

        /// <summary>
        /// Применить тему, сохранённую в настройках (при запуске)
        /// </summary>
        public void ApplySavedTheme()
        {
            var savedTheme = DB.SettingsTable.GetSetting("currentTheme");
            if (savedTheme != null && !string.IsNullOrEmpty(savedTheme.settingValue))
            {
                ScanInstalledThemes();
                var theme = _installedThemes.FirstOrDefault(t =>
                    t.FolderName.Equals(savedTheme.settingValue, StringComparison.OrdinalIgnoreCase));

                if (theme != null)
                {
                    _ = ApplyThemeAsync(theme);
                }
            }
        }

        /// <summary>
        /// Установить тему из репозитория
        /// </summary>
        public async Task<bool> InstallThemeAsync(AddonStoreItem item, AddonStoreService storeService)
        {
            try
            {
                var targetFolder = Path.Combine(_themesDirectory, item.FolderName);
                Directory.CreateDirectory(targetFolder);

                // Скачиваем XAML файл темы
                var xamlData = await storeService.DownloadFileAsync(item.FileUrl);
                var xamlPath = Path.Combine(targetFolder, $"{item.FolderName}.xaml");
                await File.WriteAllBytesAsync(xamlPath, xamlData);

                // Скачиваем иконку
                try
                {
                    var iconData = await storeService.DownloadFileAsync(item.IconUrl);
                    var iconPath = Path.Combine(targetFolder, "icon.png");
                    await File.WriteAllBytesAsync(iconPath, iconData);
                }
                catch { }

                // Скачиваем README
                try
                {
                    var readmeContent = await storeService.GetReadmeContentAsync($"Themes/{item.FolderName}");
                    var readmePath = Path.Combine(targetFolder, "README.md");
                    await File.WriteAllTextAsync(readmePath, readmeContent);
                }
                catch { }

                // Сканируем установленные темы заново
                ScanInstalledThemes();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка установки темы {item.FolderName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Удалить тему
        /// </summary>
        public async Task<bool> UninstallThemeAsync(string folderName)
        {
            try
            {
                if (_currentTheme?.FolderName.Equals(folderName, StringComparison.OrdinalIgnoreCase) == true)
                {
                    ResetTheme();
                }

                var folderPath = Path.Combine(_themesDirectory, folderName);
                if (Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath, true);
                }

                ScanInstalledThemes();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка удаления темы {folderName}: {ex.Message}");
                return false;
            }
        }

        private void RemoveThemeResources(InstalledTheme theme)
        {
            var toRemove = Application.Current.Resources.MergedDictionaries
                .Where(d => d.Source != null && d.Source.LocalPath.Contains(theme.FolderName))
                .ToList();

            foreach (var dict in toRemove)
            {
                Application.Current.Resources.MergedDictionaries.Remove(dict);
            }

            theme.IsActive = false;
        }
    }

    /// <summary>
    /// Информация об установленной теме
    /// </summary>
    public class InstalledTheme
    {
        public string FolderName { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string FolderPath { get; set; }
        public string ThemeFile { get; set; }
        public string IconPath { get; set; }
        public string ReadmePath { get; set; }
        public bool IsDark { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Аргументы событий темы
    /// </summary>
    public class ThemeEventArgs : EventArgs
    {
        public InstalledTheme Theme { get; }
        public string ThemeName { get; }
        public string ErrorMessage { get; }

        public ThemeEventArgs(InstalledTheme theme, string themeName = null, string errorMessage = null)
        {
            Theme = theme;
            ThemeName = themeName ?? theme?.Name;
            ErrorMessage = errorMessage;
        }
    }
}