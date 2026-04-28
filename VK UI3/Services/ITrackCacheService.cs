using System.Threading.Tasks;

namespace VK_UI3.Services
{
    public interface ITrackCacheService
    {
        Task<string> GetOrDownloadTrackAsync(string trackId, string url);
        void ClearCache();
    }
}