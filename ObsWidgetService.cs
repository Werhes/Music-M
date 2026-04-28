using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace VK_UI3.Services
{
    public class TrackInfo
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string CoverUrl { get; set; }
        public bool IsPlaying { get; set; }
    }

    public class ObsWidgetService : IDisposable
    {
        private HttpListener _listener;
        private bool _isRunning;
        private TrackInfo _currentTrack;

        public void Start(int port = 8080)
        {
            if (_isRunning) return;

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port}/");
            _listener.Start();
            _isRunning = true;

            Task.Run(ListenAsync);
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
        }

        public void UpdateTrackInfo(string title, string artist, string coverUrl, bool isPlaying)
        {
            _currentTrack = new TrackInfo
            {
                Title = title,
                Artist = artist,
                CoverUrl = coverUrl,
                IsPlaying = isPlaying
            };
        }

        private async Task ListenAsync()
        {
            while (_isRunning)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    ProcessRequest(context);
                }
                catch (HttpListenerException)
                {
                    // Игнорируем ошибку при остановке листенера
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                if (request.Url.AbsolutePath.Equals("/api/current", StringComparison.OrdinalIgnoreCase))
                {
                    response.ContentType = "application/json; charset=utf-8";
                    var json = JsonSerializer.Serialize(_currentTrack ?? new TrackInfo());
                    var buffer = Encoding.UTF8.GetBytes(json);
                    
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }
                else
                {
                    // Отдаем HTML-страницу виджета
                    response.ContentType = "text/html; charset=utf-8";
                    var buffer = Encoding.UTF8.GetBytes(WidgetHtml);
                    
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OBS Widget] Error processing request: {ex.Message}");
            }
            finally
            {
                response.OutputStream.Close();
            }
        }

        public void Dispose()
        {
            Stop();
            _listener?.Close();
        }

        private const string WidgetHtml = @"<!DOCTYPE html>
<html lang='ru'>
<head>
    <meta charset='utf-8'>
    <style>
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color: white; overflow: hidden; margin: 0; padding: 10px; }
        .widget { display: flex; align-items: center; background: rgba(30, 30, 30, 0.85); padding: 10px; border-radius: 12px; width: 350px; opacity: 0; transition: opacity 0.5s ease-in-out, transform 0.5s ease-in-out; transform: translateY(20px); box-shadow: 0 4px 15px rgba(0,0,0,0.5); backdrop-filter: blur(10px); }
        .widget.visible { opacity: 1; transform: translateY(0); }
        .cover { width: 56px; height: 56px; border-radius: 8px; margin-right: 12px; object-fit: cover; background: #333; }
        .info { display: flex; flex-direction: column; overflow: hidden; white-space: nowrap; flex: 1; }
        .title { font-size: 16px; font-weight: 600; overflow: hidden; text-overflow: ellipsis; margin-bottom: 4px; text-shadow: 1px 1px 2px rgba(0,0,0,0.8); }
        .artist { font-size: 13px; color: #b3b3b3; overflow: hidden; text-overflow: ellipsis; text-shadow: 1px 1px 2px rgba(0,0,0,0.8); }
    </style>
</head>
<body>
    <div class='widget' id='widget'>
        <img id='cover' class='cover' src='' />
        <div class='info'>
            <div class='title' id='title'>Ожидание...</div>
            <div class='artist' id='artist'></div>
        </div>
    </div>
    <script>
        let currentTitle = '';
        async function update() {
            try {
                let res = await fetch('/api/current');
                let data = await res.json();
                let widget = document.getElementById('widget');
                
                if (data && data.Title && data.IsPlaying) {
                    if (currentTitle !== data.Title) {
                        document.getElementById('title').innerText = data.Title;
                        document.getElementById('artist').innerText = data.Artist;
                        document.getElementById('cover').src = data.CoverUrl || '';
                        currentTitle = data.Title;
                    }
                    widget.classList.add('visible');
                } else {
                    widget.classList.remove('visible');
                    currentTitle = '';
                }
            } catch(e) { console.error('Error fetching track:', e); }
        }
        setInterval(update, 1000);
    </script>
</body>
</html>";
    }
}