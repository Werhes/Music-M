using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using VK_UI3.DB;

namespace VK_UI3.Services
{
    /// <summary>
    /// Информация о кешированном треке.
    /// </summary>
    public class CachedTrackInfo
    {
        public long OwnerId { get; set; }
        public long AudioId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long SizeBytes { get; set; }
        public DateTime CachedDate { get; set; }
    }

    /// <summary>
    /// Сервис для кеширования аудио-треков (MP3) на диск.
    /// Треки сохраняются в %APPDATA%/VKMMKZ/tracksCache/.
    /// Имя файла: {OwnerID}_{AudioID}.mp3
    /// </summary>
    public static class TrackCacheManager
    {
        private const string CACHE_SUBDIR = "tracksCache";

        private static readonly HttpClient _httpClient = new HttpClient();
        // Разрешаем до 3 одновременных скачиваний треков
        private static readonly SemaphoreSlim _downloadSemaphore = new SemaphoreSlim(3, 3);

        /// <summary>
        /// Возвращает путь к директории кеша треков.
        /// </summary>
        public static string GetCacheDirectory()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataPath, "VKMMKZ", CACHE_SUBDIR);
        }

        /// <summary>
        /// Возвращает полный путь к файлу кеша для указанного трека.
        /// </summary>
        public static string GetCachedFilePath(long ownerId, long audioId)
        {
            string cacheDir = GetCacheDirectory();
            return Path.Combine(cacheDir, $"{ownerId}_{audioId}.mp3");
        }

        /// <summary>
        /// Проверяет, существует ли трек в кеше.
        /// </summary>
        public static bool IsTrackCached(long ownerId, long audioId)
        {
            string filePath = GetCachedFilePath(ownerId, audioId);
            return File.Exists(filePath);
        }

        /// <summary>
        /// Возвращает путь к кешированному треку, если он существует.
        /// Иначе возвращает null.
        /// </summary>
        public static Task<string?> GetCachedTrackPathAsync(long ownerId, long audioId)
        {
            string filePath = GetCachedFilePath(ownerId, audioId);
            if (File.Exists(filePath))
            {
                return Task.FromResult<string?>(filePath);
            }
            return Task.FromResult<string?>(null);
        }

        /// <summary>
        /// Скачивает трек в кеш. Если файл уже существует — пропускает.
        /// Потокобезопасно — использует SemaphoreSlim для синхронизации.
        /// </summary>
        /// <summary>
        /// Скачивает трек в кеш. Если файл уже существует — пропускает.
        /// Потокобезопасно — использует SemaphoreSlim для синхронизации.
        /// </summary>
        public static async Task CacheTrackAsync(Uri trackUrl, long ownerId, long audioId)
        {
            // Проверяем URL на null
            if (trackUrl == null)
            {
                System.Diagnostics.Debug.WriteLine($"[TrackCache] Cannot cache track {ownerId}_{audioId}: URL is null");
                return;
            }

            string filePath = GetCachedFilePath(ownerId, audioId);

            // Если уже в кеше — ничего не делаем
            if (File.Exists(filePath))
                return;

            await _downloadSemaphore.WaitAsync();
            try
            {
                // Повторная проверка после захвата семафора
                if (File.Exists(filePath))
                    return;

                // Создаём директорию, если её нет
                string cacheDir = GetCacheDirectory();
                if (!Directory.Exists(cacheDir))
                    Directory.CreateDirectory(cacheDir);

                System.Diagnostics.Debug.WriteLine($"[TrackCache] Downloading track {ownerId}_{audioId} from {trackUrl}");

                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60)))
                using (var response = await _httpClient.GetAsync(trackUrl, HttpCompletionOption.ResponseHeadersRead, cts.Token))
                {
                    response.EnsureSuccessStatusCode();

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await stream.CopyToAsync(fileStream);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[TrackCache] Successfully cached track {ownerId}_{audioId}");

                // После сохранения проверяем лимит кеша
                int maxSizeMb = CacheSettingsManager.GetTrackCacheMaxSizeMb();
                EnforceCacheSizeLimit(maxSizeMb);
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine($"[TrackCache] Timeout downloading track {ownerId}_{audioId}");
                if (File.Exists(filePath))
                {
                    try { File.Delete(filePath); } catch { }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TrackCache] Failed to cache track {ownerId}_{audioId}: {ex.Message}");
                // Если файл был частично записан — удаляем
                if (File.Exists(filePath))
                {
                    try { File.Delete(filePath); } catch { }
                }
            }
            finally
            {
                _downloadSemaphore.Release();
            }
        }

        /// <summary>
        /// Кеширует трек в фоне (fire-and-forget). Используется для сохранения трека
        /// сразу после начала проигрывания, без ожидания завершения.
        /// </summary>
        public static async void CacheTrackInBackground(Uri trackUrl, long ownerId, long audioId)
        {
            try
            {
                await CacheTrackAsync(trackUrl, ownerId, audioId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TrackCache] Background cache failed for track {ownerId}_{audioId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Возвращает общий размер кеша треков в байтах.
        /// </summary>
        public static long GetCacheSizeBytes()
        {
            try
            {
                string cacheDir = GetCacheDirectory();
                if (!Directory.Exists(cacheDir))
                    return 0;

                var directoryInfo = new DirectoryInfo(cacheDir);
                return directoryInfo.GetFiles("*.mp3").Sum(f => f.Length);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Возвращает количество файлов в кеше треков.
        /// </summary>
        public static int GetCacheFileCount()
        {
            try
            {
                string cacheDir = GetCacheDirectory();
                if (!Directory.Exists(cacheDir))
                    return 0;

                return new DirectoryInfo(cacheDir).GetFiles("*.mp3").Length;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Очищает весь кеш треков.
        /// </summary>
        public static void ClearCache()
        {
            try
            {
                string cacheDir = GetCacheDirectory();
                if (!Directory.Exists(cacheDir))
                    return;

                var directoryInfo = new DirectoryInfo(cacheDir);
                foreach (var file in directoryInfo.GetFiles("*.mp3"))
                {
                    try { file.Delete(); } catch { }
                }

                System.Diagnostics.Debug.WriteLine($"[TrackCache] Cache cleared");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TrackCache] Failed to clear cache: {ex.Message}");
            }
        }

        /// <summary>
        /// Проверяет, не превышает ли размер кеша указанный лимит.
        /// Если превышает — удаляет самые старые файлы.
        /// </summary>
        /// <param name="maxSizeMb">Максимальный размер кеша в мегабайтах.</param>
        public static void EnforceCacheSizeLimit(int maxSizeMb)
        {
            try
            {
                string cacheDir = GetCacheDirectory();
                if (!Directory.Exists(cacheDir))
                    return;

                var directoryInfo = new DirectoryInfo(cacheDir);
                var files = directoryInfo.GetFiles("*.mp3")
                    .OrderBy(f => f.CreationTime)
                    .ToList();

                long maxSizeBytes = (long)maxSizeMb * 1024 * 1024;
                long currentSize = files.Sum(f => f.Length);

                if (currentSize <= maxSizeBytes)
                    return;

                System.Diagnostics.Debug.WriteLine($"[TrackCache] Cache size ({currentSize / 1024 / 1024} MB) exceeds limit ({maxSizeMb} MB). Cleaning old files...");

                foreach (var file in files)
                {
                    if (currentSize <= maxSizeBytes)
                        break;

                    try
                    {
                        long fileSize = file.Length;
                        file.Delete();
                        currentSize -= fileSize;
                        System.Diagnostics.Debug.WriteLine($"[TrackCache] Deleted old cached track: {file.Name}");
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TrackCache] Error enforcing cache size limit: {ex.Message}");
            }
        }

        /// <summary>
        /// Возвращает список всех кешированных треков с информацией о них.
        /// </summary>
        public static List<CachedTrackInfo> GetCachedTracks()
        {
            var result = new List<CachedTrackInfo>();
            try
            {
                string cacheDir = GetCacheDirectory();
                if (!Directory.Exists(cacheDir))
                    return result;

                var files = new DirectoryInfo(cacheDir).GetFiles("*.mp3")
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                foreach (var file in files)
                {
                    // Имя файла: {OwnerID}_{AudioID}.mp3
                    string nameWithoutExt = Path.GetFileNameWithoutExtension(file.Name);
                    var parts = nameWithoutExt.Split('_');
                    if (parts.Length == 2 && long.TryParse(parts[0], out long ownerId) && long.TryParse(parts[1], out long audioId))
                    {
                        result.Add(new CachedTrackInfo
                        {
                            OwnerId = ownerId,
                            AudioId = audioId,
                            FileName = file.Name,
                            FilePath = file.FullName,
                            SizeBytes = file.Length,
                            CachedDate = file.CreationTime
                        });
                    }
                }
            }
            catch { }
            return result;
        }
    }
}