using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using MusicX.Services;
using VK_UI3.Addons;
using VK_UI3.Models;

namespace VK_UI3.Services
{
    /// <summary>
    /// Менеджер для управления установленными аддонами (расширениями).
    /// Загружает DLL расширений, инициализирует их и управляет их жизненным циклом.
    /// </summary>
    public class AddonManager
    {
        private readonly List<LoadedAddon> _loadedAddons = new();
        private readonly string _addonsDirectory;

        public IReadOnlyList<LoadedAddon> LoadedAddons => _loadedAddons.AsReadOnly();

        public event EventHandler<AddonEventArgs> AddonLoaded;
        public event EventHandler<AddonEventArgs> AddonUnloaded;
        public event EventHandler<AddonEventArgs> AddonError;

        public AddonManager()
        {
            _addonsDirectory = Path.Combine(StaticService.UserDataFolder.FullName, "Addons");
            Directory.CreateDirectory(_addonsDirectory);
        }

        /// <summary>
        /// Загрузить все установленные аддоны из папки Addons
        /// </summary>
        public async Task LoadAllAddonsAsync()
        {
            if (!Directory.Exists(_addonsDirectory))
                return;

            var addonFolders = Directory.GetDirectories(_addonsDirectory);

            foreach (var folder in addonFolders)
            {
                await LoadAddonFromFolderAsync(folder);
            }
        }

        /// <summary>
        /// Загрузить аддон из конкретной папки
        /// </summary>
        public async Task LoadAddonFromFolderAsync(string folderPath)
        {
            try
            {
                var folderName = Path.GetFileName(folderPath);
                var dllFiles = Directory.GetFiles(folderPath, "*.dll");

                if (dllFiles.Length == 0)
                {
                    RaiseError(folderName, "DLL файл не найден в папке аддона");
                    return;
                }

                // Ищем основную DLL (с именем папки или первую)
                var mainDll = dllFiles.FirstOrDefault(f =>
                    Path.GetFileNameWithoutExtension(f).Equals(folderName, StringComparison.OrdinalIgnoreCase))
                    ?? dllFiles[0];

                // Загружаем сборку
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(mainDll);

                // Ищем типы, реализующие IAddon
                var addonTypes = assembly.GetTypes()
                    .Where(t => typeof(IAddon).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                    .ToList();

                if (addonTypes.Count == 0)
                {
                    RaiseError(folderName, "Не найден класс, реализующий IAddon");
                    return;
                }

                foreach (var type in addonTypes)
                {
                    if (Activator.CreateInstance(type) is IAddon addon)
                    {
                        var loadedAddon = new LoadedAddon
                        {
                            Addon = addon,
                            Assembly = assembly,
                            FolderPath = folderPath,
                            IsLoaded = false
                        };

                        try
                        {
                            await addon.InitializeAsync();
                            loadedAddon.IsLoaded = true;
                            _loadedAddons.Add(loadedAddon);
                            RaiseLoaded(addon);
                        }
                        catch (Exception ex)
                        {
                            RaiseError(addon.Name ?? folderName, $"Ошибка инициализации: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseError(Path.GetFileName(folderPath), $"Ошибка загрузки: {ex.Message}");
            }
        }

        /// <summary>
        /// Выгрузить конкретный аддон
        /// </summary>
        public async Task UnloadAddonAsync(string addonId)
        {
            var loaded = _loadedAddons.FirstOrDefault(a => a.Addon.Id == addonId);
            if (loaded == null) return;

            try
            {
                await loaded.Addon.ShutdownAsync();
                _loadedAddons.Remove(loaded);
                RaiseUnloaded(loaded.Addon);
            }
            catch (Exception ex)
            {
                RaiseError(loaded.Addon.Name ?? addonId, $"Ошибка выгрузки: {ex.Message}");
            }
        }

        /// <summary>
        /// Выгрузить все аддоны
        /// </summary>
        public async Task UnloadAllAddonsAsync()
        {
            foreach (var loaded in _loadedAddons.ToList())
            {
                await UnloadAddonAsync(loaded.Addon.Id);
            }
        }

        /// <summary>
        /// Установить аддон из репозитория (скачать и загрузить)
        /// </summary>
        public async Task<bool> InstallAddonAsync(AddonStoreItem item, AddonStoreService storeService)
        {
            try
            {
                var targetFolder = Path.Combine(_addonsDirectory, item.FolderName);
                Directory.CreateDirectory(targetFolder);

                // Скачиваем DLL
                var dllData = await storeService.DownloadFileAsync(item.FileUrl);
                var dllPath = Path.Combine(targetFolder, $"{item.FolderName}.dll");
                await File.WriteAllBytesAsync(dllPath, dllData);

                // Скачиваем иконку
                try
                {
                    var iconData = await storeService.DownloadFileAsync(item.IconUrl);
                    var iconPath = Path.Combine(targetFolder, "icon.png");
                    await File.WriteAllBytesAsync(iconPath, iconData);
                }
                catch { /* иконка опциональна */ }

                // Скачиваем README
                try
                {
                    var readmeContent = await storeService.GetReadmeContentAsync($"Addons/{item.FolderName}");
                    var readmePath = Path.Combine(targetFolder, "README.md");
                    await File.WriteAllTextAsync(readmePath, readmeContent);
                }
                catch { /* README опционален */ }

                // Загружаем установленный аддон
                await LoadAddonFromFolderAsync(targetFolder);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка установки аддона {item.FolderName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Удалить аддон
        /// </summary>
        public async Task<bool> UninstallAddonAsync(string folderName)
        {
            try
            {
                var loaded = _loadedAddons.FirstOrDefault(a =>
                    Path.GetFileName(a.FolderPath).Equals(folderName, StringComparison.OrdinalIgnoreCase));

                if (loaded != null)
                {
                    await UnloadAddonAsync(loaded.Addon.Id);
                }

                var folderPath = Path.Combine(_addonsDirectory, folderName);
                if (Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath, true);
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка удаления аддона {folderName}: {ex.Message}");
                return false;
            }
        }

        private void RaiseLoaded(IAddon addon)
        {
            AddonLoaded?.Invoke(this, new AddonEventArgs(addon));
        }

        private void RaiseUnloaded(IAddon addon)
        {
            AddonUnloaded?.Invoke(this, new AddonEventArgs(addon));
        }

        private void RaiseError(string addonName, string error)
        {
            AddonError?.Invoke(this, new AddonEventArgs(null, addonName, error));
        }
    }

    /// <summary>
    /// Информация о загруженном аддоне
    /// </summary>
    public class LoadedAddon
    {
        public IAddon Addon { get; set; }
        public Assembly Assembly { get; set; }
        public string FolderPath { get; set; }
        public bool IsLoaded { get; set; }
    }

    /// <summary>
    /// Аргументы событий аддона
    /// </summary>
    public class AddonEventArgs : EventArgs
    {
        public IAddon Addon { get; }
        public string AddonName { get; }
        public string ErrorMessage { get; }

        public AddonEventArgs(IAddon addon, string addonName = null, string errorMessage = null)
        {
            Addon = addon;
            AddonName = addonName ?? addon?.Name;
            ErrorMessage = errorMessage;
        }
    }
}