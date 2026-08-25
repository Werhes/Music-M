using System;
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

        // ===== Ключи настроек кеша треков =====

        /// <summary>Включить кеширование треков</summary>
        public const string TrackCacheEnabledKey = "trackCacheEnabled";

        /// <summary>Максимальный размер кеша треков в МБ</summary>
        public const string TrackCacheMaxSizeMbKey = "trackCacheMaxSizeMb";

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

        // ===== Значения по умолчанию для кеша треков =====

        public const bool DefaultTrackCacheEnabled = true;
        public const int DefaultTrackCacheMaxSizeMb = 5000; // 5 ГБ

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

        // ===== Методы для кеша треков =====

        public static bool IsTrackCacheEnabled()
        {
            var setting = SettingsTable.GetSetting(TrackCacheEnabledKey);
            if (setting == null)
                return DefaultTrackCacheEnabled;
            return setting.settingValue.Equals("1");
        }

        public static void SetTrackCacheEnabled(bool enabled)
        {
            SettingsTable.SetSetting(TrackCacheEnabledKey, enabled ? "1" : "0");
        }

        public static int GetTrackCacheMaxSizeMb()
        {
            var setting = SettingsTable.GetSetting(TrackCacheMaxSizeMbKey);
            if (setting == null || !int.TryParse(setting.settingValue, out int value))
                return DefaultTrackCacheMaxSizeMb;
            return Math.Clamp(value, 100, 50000);
        }

        public static void SetTrackCacheMaxSizeMb(int sizeMb)
        {
            SettingsTable.SetSetting(TrackCacheMaxSizeMbKey, Math.Clamp(sizeMb, 100, 50000).ToString());
        }

        /// <summary>
        /// Очищает кеш треков на диске
        /// </summary>
        public static void ClearTrackCache()
        {
            TrackCacheManager.ClearCache();
        }

        /// <summary>
        /// Возвращает текущий размер кеша треков в байтах
        /// </summary>
        public static long GetTrackCacheSizeBytes()
        {
            return TrackCacheManager.GetCacheSizeBytes();
        }

        /// <summary>
        /// Возвращает количество файлов в кеше треков
        /// </summary>
        public static int GetTrackCacheFileCount()
        {
            return TrackCacheManager.GetCacheFileCount();
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