using System;

namespace VK_UI3.Services
{
    public interface IObsWidgetService : IDisposable
    {
        void Start(int port = 8080);
        void Stop();
        void UpdateTrackInfo(string title, string artist, string coverUrl, bool isPlaying, double duration, double currentTime);
    }
}