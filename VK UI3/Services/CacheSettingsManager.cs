using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VK_UI3.DB;

namespace VK_UI3.Services
{
    /// <summary>
    /// Центральный класс для управления всеми настройками кеширования.
    /// Все настройки хранятся в БД через SettingsTable.
    /// </summary>
    public static class CacheSettingsManager
    {
        // ===== Ключи настроек =====

        /// <summary>Размер кеша фотографий в МБ</summary>
        public const string PhotoCacheSizeKey = "photoCacheSize";

        /// <summary>Включить кеширование изображений</summary>
        public const string ImageCacheEnabledKey = "imageCacheEnabled";

        /// <summary>Максимальное количество одновременных загрузок изображений</summary>
        public const string MaxConcurrentDownloadsKey = "maxConcurrentImageDownloads";

        /// <summary>Включить систему очередей загрузки</summary>
        public const string EnableDownloadQueueKey = "enableDownloadQueue";

        /// <summary>Время жизни кеша в памяти (TimedDictionary) в минутах</summary>
        public const string MemoryCacheTimeLiveKey = "memoryCacheTimeLive";

        /// <summary>Включить кеширование в памяти</summary>
        public const string MemoryCacheEnabledKey = "memoryCacheEnabled";

        /// <summary>Интервал проверки устаревших записей в кеше памяти (мс)</summary>
        public const string MemoryCacheCheckIntervalKey = "memoryCacheCheckInterval";

        /// <summary>Включить автоочистку кеша изображений при запуске</summary>
        public const string AutoClearImageCacheOnStartKey = "autoClearImageCacheOnStart";

        /// <summary>Максимальное количество файлов в кеше изображений</summary>
        public const string ImageCacheMaxFilesKey = "imageCacheMaxFiles";

        /// <summary>Включить dev-функции</summary>
        public const string DevFeaturesEnabledKey = "devFeaturesEnabled";

        /// <summary>Список скрытых кнопок плеера (id через запятую)</summary>
        public const string HiddenPlayerButtonsKey = "hiddenPlayerButtons";

        /// <summary>Список отключённых дополнительных кнопок в трее (id через запятую)</summary>
        public const string DisabledTrayButtonsKey = "disabledTrayButtons";

        /// <summary>Включить BetterPlayer (альфа-плеер в стиле MusicX)</summary>
        public const string BetterPlayerEnabledKey = "betterPlayerEnabled";

        // ===== Значения по умолчанию =====

        public const int DefaultPhotoCacheSizeMb = 100;
        public const bool DefaultImageCacheEnabled = true;
        public const int DefaultMaxConcurrentDownloads = 10;
        public const bool DefaultEnableDownloadQueue = true;
        public const int DefaultMemoryCacheTimeLiveMinutes = 15;
        public const bool DefaultMemoryCacheEnabled = true;
        public const int DefaultMemoryCacheCheckIntervalMs = 1000;
        public const bool DefaultAutoClearImageCacheOnStart = false;
        public const int DefaultImageCacheMaxFiles = 5000;

        /// <summary>Dev-функции выключены по умолчанию</summary>
        public const bool DefaultDevFeaturesEnabled = false;

        // ===== Методы для чтения настроек =====

        public static int GetPhotoCacheSizeMb()
        {
            var setting = SettingsTable.GetSetting(PhotoCacheSizeKey);
            if (setting == null || !int.TryParse(setting.settingValue, out int value))
                return DefaultPhotoCacheSizeMb;
            return value;
        }

        public static void SetPhotoCacheSizeMb(int sizeMb)
        {
            if (sizeMb < 10) sizeMb = 10;
            if (sizeMb > 50000) sizeMb = 50000;
            SettingsTable.SetSetting(PhotoCacheSizeKey, sizeMb.ToString());
        }

        public static bool IsImageCacheEnabled()
        {
            var setting = SettingsTable.GetSetting(ImageCacheEnabledKey);
            if (setting == null)
                return DefaultImageCacheEnabled;
            return setting.settingValue.Equals("1");
        }

        public static void SetImageCacheEnabled(bool enabled)
        {
            SettingsTable.SetSetting(ImageCacheEnabledKey, enabled ? "1" : "0");
        }

        public static int GetMaxConcurrentDownloads()
        {
            var setting = SettingsTable.GetSetting(MaxConcurrentDownloadsKey);
            if (setting == null || !int.TryParse(setting.settingValue, out int value))
                return DefaultMaxConcurrentDownloads;
            return Math.Clamp(value, 1, 50);
        }

        public static void SetMaxConcurrentDownloads(int maxDownloads)
        {
            SettingsTable.SetSetting(MaxConcurrentDownloadsKey, Math.Clamp(maxDownloads, 1, 50).ToString());
        }

        public static bool IsDownloadQueueEnabled()
        {
            var setting = SettingsTable.GetSetting(EnableDownloadQueueKey);
            if (setting == null)
                return DefaultEnableDownloadQueue;
            return setting.settingValue.Equals("1");
        }

        public static void SetDownloadQueueEnabled(bool enabled)
        {
            SettingsTable.SetSetting(EnableDownloadQueueKey, enabled ? "1" : "0");
        }

        public static int GetMemoryCacheTimeLiveMinutes()
        {
            var setting = SettingsTable.GetSetting(MemoryCacheTimeLiveKey);
            if (setting == null || !int.TryParse(setting.settingValue, out int value))
                return DefaultMemoryCacheTimeLiveMinutes;
            return Math.Clamp(value, 1, 1440);
        }

        public static void SetMemoryCacheTimeLiveMinutes(int minutes)
        {
            SettingsTable.SetSetting(MemoryCacheTimeLiveKey, Math.Clamp(minutes, 1, 1440).ToString());
        }

        public static bool IsMemoryCacheEnabled()
        {
            var setting = SettingsTable.GetSetting(MemoryCacheEnabledKey);
            if (setting == null)
                return DefaultMemoryCacheEnabled;
            return setting.settingValue.Equals("1");
        }

        public static void SetMemoryCacheEnabled(bool enabled)
        {
            SettingsTable.SetSetting(MemoryCacheEnabledKey, enabled ? "1" : "0");
        }

        public static int GetMemoryCacheCheckIntervalMs()
        {
            var setting = SettingsTable.GetSetting(MemoryCacheCheckIntervalKey);
            if (setting == null || !int.TryParse(setting.settingValue, out int value))
                return DefaultMemoryCacheCheckIntervalMs;
            return Math.Clamp(value, 100, 60000);
        }

        public static void SetMemoryCacheCheckIntervalMs(int intervalMs)
        {
            SettingsTable.SetSetting(MemoryCacheCheckIntervalKey, Math.Clamp(intervalMs, 100, 60000).ToString());
        }

        public static bool IsAutoClearImageCacheOnStart()
        {
            var setting = SettingsTable.GetSetting(AutoClearImageCacheOnStartKey);
            if (setting == null)
                return DefaultAutoClearImageCacheOnStart;
            return setting.settingValue.Equals("1");
        }

        public static void SetAutoClearImageCacheOnStart(bool enabled)
        {
            SettingsTable.SetSetting(AutoClearImageCacheOnStartKey, enabled ? "1" : "0");
        }

        public static int GetImageCacheMaxFiles()
        {
            var setting = SettingsTable.GetSetting(ImageCacheMaxFilesKey);
            if (setting == null || !int.TryParse(setting.settingValue, out int value))
                return DefaultImageCacheMaxFiles;
            return Math.Clamp(value, 100, 50000);
        }

        public static void SetImageCacheMaxFiles(int maxFiles)
        {
            SettingsTable.SetSetting(ImageCacheMaxFilesKey, Math.Clamp(maxFiles, 100, 50000).ToString());
        }

        /// <summary>
        /// Включены ли dev-функции (по умолчанию выключены)
        /// </summary>
        public static bool IsDevFeaturesEnabled()
        {
            var setting = SettingsTable.GetSetting(DevFeaturesEnabledKey);
            if (setting == null)
                return DefaultDevFeaturesEnabled;
            return setting.settingValue.Equals("1");
        }

        /// <summary>
        /// Включает или выключает dev-функции
        /// </summary>
        public static void SetDevFeaturesEnabled(bool enabled)
        {
            SettingsTable.SetSetting(DevFeaturesEnabledKey, enabled ? "1" : "0");
        }

        /// <summary>
        /// Возвращает множество id скрытых кнопок плеера
        /// </summary>
        public static HashSet<string> GetHiddenPlayerButtons()
        {
            var setting = SettingsTable.GetSetting(HiddenPlayerButtonsKey);
            if (setting == null || string.IsNullOrEmpty(setting.settingValue))
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return new HashSet<string>(
                setting.settingValue.Split(',', StringSplitOptions.RemoveEmptyEntries),
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Добавляет или убирает кнопку плеера из списка скрытых
        /// </summary>
        public static void SetPlayerButtonHidden(string buttonId, bool hidden)
        {
            if (string.IsNullOrWhiteSpace(buttonId))
                return;

            var set = GetHiddenPlayerButtons();
            if (hidden)
                set.Add(buttonId);
            else
                set.Remove(buttonId);

            SettingsTable.SetSetting(HiddenPlayerButtonsKey, string.Join(",", set));
        }

        /// <summary>
        /// Возвращает множество id отключённых дополнительных кнопок в трее
        /// </summary>
        public static HashSet<string> GetDisabledTrayButtons()
        {
            var setting = SettingsTable.GetSetting(DisabledTrayButtonsKey);
            if (setting == null || string.IsNullOrEmpty(setting.settingValue))
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return new HashSet<string>(
                setting.settingValue.Split(',', StringSplitOptions.RemoveEmptyEntries),
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Включает или отключает дополнительную кнопку в трее
        /// </summary>
        public static void SetTrayButtonDisabled(string buttonId, bool disabled)
        {
            if (string.IsNullOrWhiteSpace(buttonId))
                return;

            var set = GetDisabledTrayButtons();
            if (disabled)
                set.Add(buttonId);
            else
                set.Remove(buttonId);

            SettingsTable.SetSetting(DisabledTrayButtonsKey, string.Join(",", set));
        }

        /// <summary>
        /// Включён ли BetterPlayer (альфа-плеер в стиле MusicX), по умолчанию выключен
        /// </summary>
        public static bool IsBetterPlayerEnabled()
        {
            var setting = SettingsTable.GetSetting(BetterPlayerEnabledKey);
            if (setting == null)
                return false;
            return setting.settingValue.Equals("1");
        }

        /// <summary>
        /// Включает или выключает BetterPlayer
        /// </summary>
        public static void SetBetterPlayerEnabled(bool enabled)
        {
            SettingsTable.SetSetting(BetterPlayerEnabledKey, enabled ? "1" : "0");
        }

        /// <summary>
        /// Очищает кеш изображений на диске
        /// </summary>
        public static void ClearImageCache()
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cacheFolderPath = Path.Combine(appDataPath, "VKMMKZ", "photosCache");

                if (Directory.Exists(cacheFolderPath))
                {
                    var directoryInfo = new DirectoryInfo(cacheFolderPath);
                    foreach (var file in directoryInfo.GetFiles())
                    {
                        try { file.Delete(); }
                        catch { }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Возвращает текущий размер кеша изображений в байтах
        /// </summary>
        public static long GetImageCacheSizeBytes()
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cacheFolderPath = Path.Combine(appDataPath, "VKMMKZ", "photosCache");

                if (Directory.Exists(cacheFolderPath))
                {
                    var directoryInfo = new DirectoryInfo(cacheFolderPath);
                    return directoryInfo.GetFiles().Sum(f => f.Length);
                }
            }
            catch { }
            return 0;
        }

        /// <summary>
        /// Возвращает количество файлов в кеше изображений
        /// </summary>
        public static int GetImageCacheFileCount()
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cacheFolderPath = Path.Combine(appDataPath, "VKMMKZ", "photosCache");

                if (Directory.Exists(cacheFolderPath))
                {
                    return new DirectoryInfo(cacheFolderPath).GetFiles().Length;
                }
            }
            catch { }
            return 0;
        }

        /// <summary>
        /// Применяет все настройки кеширования к соответствующим компонентам
        /// </summary>
        public static void ApplyAllCacheSettings()
        {
            // Применяем настройки для AnimationsChangeImage
            Helpers.Animations.AnimationsChangeImage.SetMaxConcurrentDownloads(GetMaxConcurrentDownloads());
            Helpers.Animations.AnimationsChangeImage.EnableQueueSystem(IsDownloadQueueEnabled());
        }
    }
}