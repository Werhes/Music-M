using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace VK_UI3.Services
{
    public class ObsWidgetService : IObsWidgetService
    {
        private HttpListener _listener;
        private bool _isRunning;

        // Текущая информация о треке
        private string _title = "";
        private string _artist = "";
        private string _coverUrl = "";
        private bool _isPlaying = false;
        private double _duration = 0;
        private double _currentTime = 0;

        public void Start(int port = 8080)
        {
            if (_isRunning) return;

            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{port}/");
                _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                _listener.Start();
                _isRunning = true;

                Task.Run(ListenAsync);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка запуска OBS Widget Server: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _listener?.Stop();
            _listener?.Close();
        }

        public void UpdateTrackInfo(string title, string artist, string coverUrl, bool isPlaying, double duration, double currentTime)
        {
            _title = title;
            _artist = artist;
            _coverUrl = coverUrl ?? "";
            _isPlaying = isPlaying;
            _duration = duration;
            _currentTime = currentTime;
        }

        private async Task ListenAsync()
        {
            while (_isRunning)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = ProcessRequestAsync(context);
                }
                catch (HttpListenerException)
                {
                    // Игнорируем исключения при штатной остановке listener'а
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"OBS Widget Server Error: {ex.Message}");
                }
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;

                response.AppendHeader("Access-Control-Allow-Origin", "*");

                string responseString = "";
                string contentType = "text/html; charset=utf-8";

                if (request.Url.AbsolutePath.Equals("/api/track", StringComparison.OrdinalIgnoreCase))
                {
                    contentType = "application/json";
                    var trackData = new
                    {
                        title = _title,
                        artist = _artist,
                        coverUrl = _coverUrl,
                        isPlaying = _isPlaying,
                        duration = _duration,
                        currentTime = _currentTime
                    };
                    responseString = JsonSerializer.Serialize(trackData);
                }
                else
                {
                    responseString = GetWidgetHtml();
                }

                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentType = contentType;
                response.ContentLength64 = buffer.Length;

                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception)
            {
                // Игнорируем ошибки записи (например, если клиент преждевременно закрыл соединение)
            }
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }

        private string GetWidgetHtml()
        {
            return @"<!DOCTYPE html>
<html lang='ru'>
<head>
    <meta charset='UTF-8'>
    <style>
        body { margin: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; overflow: hidden; background: transparent; color: #fff; }
        .widget { display: flex; align-items: center; background: rgba(30, 30, 30, 0.85); border-radius: 12px; padding: 12px; width: 350px; box-sizing: border-box; transition: opacity 0.5s ease, transform 0.5s ease; opacity: 1; transform: translateY(0); box-shadow: 0 4px 12px rgba(0,0,0,0.3); }
        .hidden { opacity: 0; transform: translateY(20px); }
        .cover { width: 64px; height: 64px; border-radius: 8px; object-fit: cover; background: #333; margin-right: 14px; box-shadow: 0 2px 6px rgba(0,0,0,0.2); }
        .info { display: flex; flex-direction: column; justify-content: center; overflow: hidden; width: 100%; }
        .title { font-size: 16px; font-weight: bold; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; margin-bottom: 4px; }
        .artist { font-size: 14px; color: #b3b3b3; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
        .progress-bar { margin-top: 10px; height: 4px; background: rgba(255,255,255,0.2); border-radius: 2px; position: relative; overflow: hidden; }
        .progress { height: 100%; background: #2787F5; border-radius: 2px; width: 0%; transition: width 1s linear; }
    </style>
</head>
<body>
    <div class='widget hidden' id='widget'>
        <img id='cover' class='cover' src='' alt='' />
        <div class='info'>
            <div id='title' class='title'>Загрузка...</div>
            <div id='artist' class='artist'></div>
            <div class='progress-bar'>
                <div id='progress' class='progress'></div>
            </div>
        </div>
    </div>
    <script>
        async function update() {
            try {
                const res = await fetch('/api/track');
                if (res.ok) {
                    const data = await res.json();
                    const widget = document.getElementById('widget');
                    
                    if (data.isPlaying && data.title) {
                        widget.classList.remove('hidden');
                    } else {
                        widget.classList.add('hidden');
                    }

                    document.getElementById('title').innerText = data.title || 'Неизвестно';
                    document.getElementById('artist').innerText = data.artist || 'Неизвестно';
                    
                    const coverEl = document.getElementById('cover');
                    if (data.coverUrl && data.coverUrl !== coverEl.src) {
                        coverEl.src = data.coverUrl;
                    } else if (!data.coverUrl) {
                        coverEl.src = 'data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7';
                    }

                    const progress = document.getElementById('progress');
                    if (data.duration > 0) {
                        progress.style.width = ((data.currentTime / data.duration) * 100) + '%';
                    } else {
                        progress.style.width = '0%';
                    }
                }
            } catch(e) { console.error(e); }
        }
        setInterval(update, 1000);
        update();
    </script>
</body>
</html>";
        }
    }
}