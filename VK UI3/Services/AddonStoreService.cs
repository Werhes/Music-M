using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MusicX.Services;
using VK_UI3.Models;

namespace VK_UI3.Services
{
    /// <summary>
    /// Сервис для работы с репозиторием Music-M_Addons
    /// Загружает список аддонов и тем из GitHub
    /// </summary>
    public class AddonStoreService
    {
        private readonly HttpClient _httpClient;
        private const string GitHubApiBase = "https://api.github.com/repos/Werhes/Music-M_Addons";
        private const string GitHubRawBase = "https://raw.githubusercontent.com/Werhes/Music-M_Addons/main";
        private const string GitHubContentsBase = "https://api.github.com/repos/Werhes/Music-M_Addons/contents";

        public AddonStoreService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("VK-M-AddonStore/1.0");
            _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");
        }

        /// <summary>
        /// Получить список всех аддонов из репозитория
        /// </summary>
        public async Task<List<AddonStoreItem>> GetAddonsAsync()
        {
            return await GetItemsFromFolderAsync("Addons", AddonStoreItemType.Addon);
        }

        /// <summary>
        /// Получить список всех тем из репозитория
        /// </summary>
        public async Task<List<AddonStoreItem>> GetThemesAsync()
        {
            return await GetItemsFromFolderAsync("Themes", AddonStoreItemType.Theme);
        }

        /// <summary>
        /// Получить все элементы (аддоны + темы)
        /// </summary>
        public async Task<List<AddonStoreItem>> GetAllItemsAsync()
        {
            var addons = await GetAddonsAsync();
            var themes = await GetThemesAsync();
            var all = new List<AddonStoreItem>();
            all.AddRange(addons);
            all.AddRange(themes);
            return all;
        }

        /// <summary>
        /// Получить содержимое README из репозитория
        /// </summary>
        public async Task<string> GetReadmeContentAsync(string folderPath)
        {
            try
            {
                var url = $"{GitHubRawBase}/{folderPath}/README.md";
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                return "Описание отсутствует.";
            }
            catch
            {
                return "Не удалось загрузить описание.";
            }
        }

        /// <summary>
        /// Получить содержимое манифеста аддона (addon.json)
        /// </summary>
        public async Task<AddonManifest> GetAddonManifestAsync(string folderName)
        {
            try
            {
                var url = $"{GitHubRawBase}/Addons/{folderName}/addon.json";
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<AddonManifest>();
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Получить содержимое манифеста темы (theme.json)
        /// </summary>
        public async Task<ThemeManifest> GetThemeManifestAsync(string folderName)
        {
            try
            {
                var url = $"{GitHubRawBase}/Themes/{folderName}/theme.json";
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ThemeManifest>();
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Скачать файл из репозитория
        /// </summary>
        public async Task<byte[]> DownloadFileAsync(string url)
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }

        /// <summary>
        /// Получить список папок в указанной директории репозитория через GitHub API
        /// </summary>
        private async Task<List<AddonStoreItem>> GetItemsFromFolderAsync(string folder, AddonStoreItemType type)
        {
            var items = new List<AddonStoreItem>();

            try
            {
                var url = $"{GitHubContentsBase}/{folder}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return items;

                var content = await response.Content.ReadAsStringAsync();
                var entries = JsonSerializer.Deserialize<List<GitHubContentEntry>>(content);

                if (entries == null) return items;

                foreach (var entry in entries)
                {
                    if (entry.Type != "dir") continue;

                    var folderName = entry.Name;
                    var item = new AddonStoreItem
                    {
                        FolderName = folderName,
                        Type = type,
                        Name = folderName,
                        Description = "Загрузка...",
                        GitHubFolderUrl = entry.HtmlUrl,
                        IconUrl = $"{GitHubRawBase}/{folder}/{folderName}/icon.png",
                        ReadmeUrl = $"{GitHubRawBase}/{folder}/{folderName}/README.md",
                        FileUrl = $"{GitHubRawBase}/{folder}/{folderName}/{folderName}.dll",
                        IsInstalled = IsItemInstalled(folderName, type)
                    };

                    // Пытаемся загрузить манифест для получения доп. информации
                    if (type == AddonStoreItemType.Addon)
                    {
                        var manifest = await GetAddonManifestAsync(folderName);
                        if (manifest != null)
                        {
                            item.Name = manifest.Name ?? folderName;
                            item.Description = manifest.Description ?? "Описание отсутствует";
                            item.Version = manifest.Version;
                            item.Author = manifest.Author;
                        }
                    }
                    else
                    {
                        var manifest = await GetThemeManifestAsync(folderName);
                        if (manifest != null)
                        {
                            item.Name = manifest.Name ?? folderName;
                            item.Description = manifest.Description ?? "Описание отсутствует";
                            item.Version = manifest.Version;
                            item.Author = manifest.Author;
                        }
                    }

                    // Если манифеста нет, пробуем получить описание из README
                    if (string.IsNullOrEmpty(item.Description) || item.Description == "Загрузка...")
                    {
                        var readme = await GetReadmeContentAsync($"{folder}/{folderName}");
                        if (!string.IsNullOrEmpty(readme) && readme != "Описание отсутствует.")
                        {
                            // Берём первую строку README как краткое описание
                            var lines = readme.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                            foreach (var line in lines)
                            {
                                var trimmed = line.Trim().TrimStart('#').Trim();
                                if (!string.IsNullOrEmpty(trimmed) && trimmed.Length > 10)
                                {
                                    item.Description = trimmed.Length > 150
                                        ? trimmed[..150] + "..."
                                        : trimmed;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            item.Description = "Описание отсутствует";
                        }
                    }

                    items.Add(item);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки списка {folder}: {ex.Message}");
            }

            return items;
        }

        /// <summary>
        /// Проверить, установлен ли элемент локально
        /// </summary>
        private bool IsItemInstalled(string folderName, AddonStoreItemType type)
        {
            var basePath = type == AddonStoreItemType.Addon
                ? Path.Combine(StaticService.UserDataFolder.FullName, "Addons", folderName)
                : Path.Combine(StaticService.UserDataFolder.FullName, "Themes", folderName);

            return Directory.Exists(basePath);
        }

        /// <summary>
        /// Модель для десериализации ответа GitHub Contents API
        /// </summary>
        private class GitHubContentEntry
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("html_url")]
            public string HtmlUrl { get; set; }
        }
    }
}