using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace VK_UI3.Services
{
    public class TrackCacheService : ITrackCacheService
    {
        private readonly HttpClient _httpClient;
        private readonly string _cacheDirectory;

        public TrackCacheService()
        {
            _httpClient = new HttpClient();
            
            // Используем локальную папку для кеша
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _cacheDirectory = Path.Combine(localAppData, "VK_UI3", "TrackCache");
            
            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
            }
        }

        /// <summary>
        /// Скачивает трек в кеш, если его там нет, и возвращает локальный путь.
        /// Если скачивание не удалось, возвращает оригинальный URL.
        /// </summary>
        public async Task<string> GetOrDownloadTrackAsync(string trackId, string url)
        {
            if (string.IsNullOrEmpty(url)) return url;

            // Очищаем ID от возможных недопустимых символов для файловой системы
            string safeTrackId = string.Join("_", trackId.Split(Path.GetInvalidFileNameChars()));
            string filePath = Path.Combine(_cacheDirectory, $"{safeTrackId}.mp3");

            if (File.Exists(filePath))
            {
                // Файл уже закеширован, возвращаем локальный путь
                return filePath;
            }

            try
            {
                // Загружаем стрим без буферизации всего файла в память
                using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fs);

                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TrackCacheService] Ошибка загрузки трека {trackId}: {ex.Message}");
                
                // Удаляем битый файл, если он создался, чтобы не было ошибки при следующем воспроизведении
                if (File.Exists(filePath))
                {
                    try { File.Delete(filePath); } catch { }
                }

                // При ошибке скачивания возвращаем оригинальный URL
                // плеер попытается проиграть трек напрямую по сети
                return url;
            }
        }

        public void ClearCache()
        {
            if (Directory.Exists(_cacheDirectory))
            {
                try 
                {
                    Directory.Delete(_cacheDirectory, true);
                    Directory.CreateDirectory(_cacheDirectory);
                } 
                catch (Exception ex)
                {
                    Console.WriteLine($"[TrackCacheService] Ошибка очистки кеша: {ex.Message}");
                }
            }
        }
    }
}