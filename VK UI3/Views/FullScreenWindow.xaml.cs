using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using MusicX.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using VK_UI3.Helpers;
using VK_UI3.VKs;
using VK_UI3.VKs.IVK;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using WinRT.Interop;

namespace VK_UI3.Views
{
    /// <summary>
    /// мне лень писать тут fullscreen
    /// </summary>
    public sealed partial class FullScreenWindow : Window
    {
        #region Fields

        private DispatcherTimer _timer;
        private DateTime _lastPositionUpdate = DateTime.MinValue;
        private readonly GeniusService _geniusService;
        private HttpClient _lyricsHttpClient;

        // Для синхронизированного текста
        private List<(int milliseconds, string line)> _timedLyricsLines = new();
        private int _currentLyricsLineIndex = -1;
        private bool _hasTimedLyrics;

        #endregion

        #region Properties

        public MediaPlayer MediaPlayer
        {
            get => VK_UI3.Services.MediaPlayerService.MediaPlayer;
            set => VK_UI3.Services.MediaPlayerService.MediaPlayer = value;
        }

        public static IVKGetAudio iVKGetAudio
        {
            get => VK_UI3.Services.MediaPlayerService.iVKGetAudio;
            set
            {
                VK_UI3.Services.MediaPlayerService.iVKGetAudio = value;
                MainView.mainView.setNewPlayingList(value);
                VK_UI3.Controllers.AudioPlayer.NotifyoniVKUpdate();
            }
        }

        public async Task<ExtendedAudio> GetTrackDataAsync()
        {
            try
            {
                return await _TrackDataThisGet();
            }
            catch
            {
                return null;
            }
        }

        public async Task<string> GetThumbnailAsync()
        {
            try
            {
                var trackData = await GetTrackDataAsync();
                if (trackData?.audio == null) return null;

                // Сначала пробуем получить обложку из альбома
                if (trackData.audio.Album?.Thumb != null)
                {
                    return trackData.audio.Album.Thumb.Photo600
                         ?? trackData.audio.Album.Thumb.Photo300
                         ?? trackData.audio.Album.Thumb.Photo270
                         ?? trackData.audio.Album.Thumb.Photo68
                         ?? trackData.audio.Album.Thumb.Photo34
                         ?? null;
                }

                // Если в альбоме нет — пробуем прямой thumb аудио (как в TrackControl.xaml.cs)
                if (trackData.audio.Thumb != null)
                {
                    return trackData.audio.Thumb.Photo600
                         ?? trackData.audio.Thumb.Photo300
                         ?? trackData.audio.Thumb.Photo270
                         ?? trackData.audio.Thumb.Photo68
                         ?? trackData.audio.Thumb.Photo34
                         ?? null;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Constructor

        public FullScreenWindow()
        {
            this.InitializeComponent();

            // Инициализируем сервисы для загрузки текстов
            _geniusService = App._host.Services.GetRequiredService<GeniusService>();
            _lyricsHttpClient = new HttpClient();

            // Переводим окно в полноэкранный режим
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);

            // Подписываемся на события
            VK_UI3.Services.MediaPlayerService.AudioPlayedChangeEvent += OnTrackChanged;
            VK_UI3.Services.MediaPlayerService.PositionChanged += OnPositionChanged;
            VK_UI3.Services.MediaPlayerService.MediaPlayer.CurrentStateChanged += OnPlaybackStateChanged;

            // KeyDown для Escape
            this.Content.KeyDown += OnWindowKeyDown;

            // Загружаем настройку источника текста
            var lyricsSourceSetting = DB.SettingsTable.GetSetting("lyricsSource", "0");
            LyricsSourceCombo.SelectedIndex = int.Parse(lyricsSourceSetting.settingValue);

            // Загружаем данные асинхронно
            _ = SetDataAsync();
            UpdatePlayPauseIcon();
        }

        #endregion

        #region Event Handlers

        private void OnTrackChanged(object sender, EventArgs e)
        {
            this.DispatcherQueue.TryEnqueue(async () =>
            {
                await SetDataAsync();
            });
        }

        private void OnPositionChanged(object sender, TimeSpan e)
        {
            try
            {
                // Throttle: обновляем слайдер не чаще чем раз в 250 мс
                var now = DateTime.Now;
                if ((now - _lastPositionUpdate).TotalMilliseconds < 250)
                    return;
                _lastPositionUpdate = now;

                this.DispatcherQueue.TryEnqueue(() =>
                {
                    PositionSlider.Value = e.TotalSeconds;
                });
            }
            catch { }
        }

        private void OnWindowKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                CloseFullScreen();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseFullScreen();
        }

        private void NextTrackCard_Tapped(object sender, TappedRoutedEventArgs e)
        {
            VK_UI3.Services.MediaPlayerService.PlayNextTrack();
        }

        private void PositionSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (MediaPlayer == null) return;

            MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(e.NewValue);
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            VK_UI3.Services.MediaPlayerService.HandlePreviousTrack();
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (MediaPlayer == null) return;

            switch (MediaPlayer.CurrentState)
            {
                case MediaPlayerState.Playing:
                    MediaPlayer.Pause();
                    break;
                case MediaPlayerState.Paused:
                    MediaPlayer.Play();
                    break;
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            VK_UI3.Services.MediaPlayerService.PlayNextTrack();
        }

        private void OnPlaybackStateChanged(Windows.Media.Playback.MediaPlayer sender, object args)
        {
            this.DispatcherQueue.TryEnqueue(() => UpdatePlayPauseIcon());
        }

        private void UpdatePlayPauseIcon()
        {
            if (MediaPlayer == null) return;

            if (MediaPlayer.CurrentState == MediaPlayerState.Playing)
            {
                PlayPauseIcon.Glyph = "\uE769"; // Pause icon
            }
            else
            {
                PlayPauseIcon.Glyph = "\uE768"; // Play icon
            }
        }

        #endregion

        #region Data Methods

        // Общий HttpClient для загрузки изображений (как в AnimationsChangeImage)
        private static readonly HttpClient _httpClient = new HttpClient(new SocketsHttpHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = 10,
            ConnectTimeout = TimeSpan.FromSeconds(15)
        })
        {
            Timeout = TimeSpan.FromMinutes(2),
            DefaultRequestHeaders =
            {
                { "User-Agent", "VK-Music/1.0" }
            }
        };

        /// <summary>
        /// Плавно меняет обложку с fade-анимацией: сначала fade-out (250ms),
        /// затем загружает изображение через HttpClient, затем fade-in (500ms).
        /// </summary>
        private async void AnimateCoverImage(string imageUrl)
        {
            try
            {
                // Если обложки нет — показываем иконку ноты
                if (string.IsNullOrEmpty(imageUrl))
                {
                    CoverImage.Source = null;
                    CoverNote.Visibility = Visibility.Visible;
                    return;
                }

                // Fade-out: 0.25 сек
                var storyboard = new Storyboard();
                var fadeOut = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(250),
                    EnableDependentAnimation = true
                };
                Storyboard.SetTarget(fadeOut, CoverImage);
                Storyboard.SetTargetProperty(fadeOut, "Opacity");
                storyboard.Children.Add(fadeOut);

                var tcs = new TaskCompletionSource<bool>();
                EventHandler<object> onFadeOutCompleted = null;
                onFadeOutCompleted = (s, e) =>
                {
                    storyboard.Completed -= onFadeOutCompleted;
                    tcs.TrySetResult(true);
                };
                storyboard.Completed += onFadeOutCompleted;
                storyboard.Begin();

                // Ждём завершения fade-out
                await tcs.Task;

                // Загружаем изображение через HttpClient в MemoryStream
                var uri = new Uri(imageUrl, UriKind.Absolute);
                byte[] imageBytes = await _httpClient.GetByteArrayAsync(uri);

                BitmapImage bitmap = null;
                using (var stream = new MemoryStream(imageBytes))
                {
                    var randomAccessStream = new InMemoryRandomAccessStream();
                    var dw = new DataWriter(randomAccessStream.GetOutputStreamAt(0));
                    dw.WriteBytes(stream.ToArray());
                    await dw.StoreAsync();
                    await dw.FlushAsync();
                    randomAccessStream.Seek(0);

                    bitmap = new BitmapImage();
                    await bitmap.SetSourceAsync(randomAccessStream);
                }

                // Устанавливаем изображение и показываем с fade-in
                CoverImage.Source = bitmap;
                CoverNote.Visibility = Visibility.Collapsed;

                var fadeIn = new DoubleAnimation
                {
                    From = 0.0,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(500),
                    EnableDependentAnimation = true
                };
                Storyboard.SetTarget(fadeIn, CoverImage);
                Storyboard.SetTargetProperty(fadeIn, "Opacity");

                var fadeInStoryboard = new Storyboard();
                fadeInStoryboard.Children.Add(fadeIn);
                fadeInStoryboard.Begin();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FullScreenWindow] Error in AnimateCoverImage: {ex.Message}");
                CoverImage.Source = null;
                CoverNote.Visibility = Visibility.Visible;
            }
        }

        private async Task SetDataAsync()
        {
            try
            {
                var track = await GetTrackDataAsync();
                if (track?.audio == null)
                {
                    System.Diagnostics.Debug.WriteLine("[FullScreenWindow] CurrentTrack is null");
                    return;
                }

                PositionSlider.Maximum = track.audio.Duration;

                // Плавная смена обложки (асинхронно получаем URL)
                var thumb = await GetThumbnailAsync();
                AnimateCoverImage(thumb);

                // Next track info
                SetNextTrackData();

                TrackName.Text = track.audio.Title ?? "Unknown";
                ArtistName.Text = track.audio.Artist ?? "Unknown";

                // Пытаемся загрузить тексты песен
                _ = LoadLyrics();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FullScreenWindow] Error in SetDataAsync: {ex.Message}");
            }
        }

        private void SetNextTrackData()
        {
            try
            {
                var iVK = VK_UI3.Services.MediaPlayerService.iVKGetAudio;
                ExtendedAudio nextTrack = null;

                if (iVK != null && iVK.listAudio.Count > 0)
                {
                    // Определяем индекс следующего трека
                    long nextIndex;
                    if (iVK.currentTrack >= iVK.countTracks - 1 && iVK.countTracks != -1)
                        nextIndex = 0;
                    else
                        nextIndex = (iVK.currentTrack ?? 0) + 1;

                    if (nextIndex >= 0 && nextIndex < iVK.listAudio.Count)
                    {
                        nextTrack = iVK.listAudio[(int)nextIndex];
                    }
                }

                // Fallback на _nextTrack если не удалось получить через iVK
                nextTrack ??= VK_UI3.Services.MediaPlayerService._nextTrack;

                if (nextTrack == null)
                {
                    NextTrackName.Text = "Нет следующего трека";
                    NextTrackArtist.Text = "";
                    NextTrackCover.Source = null;
                    NextTrackNote.Visibility = Visibility.Visible;
                    return;
                }

                // Пробуем получить обложку из альбома, затем из прямого thumb аудио
                string coverUrl = null;
                var albumThumb = nextTrack.audio?.Album?.Thumb;
                if (albumThumb != null)
                {
                    coverUrl = albumThumb.Photo68 ?? albumThumb.Photo34;
                }

                // Если в альбоме нет — пробуем прямой thumb аудио
                if (coverUrl == null && nextTrack.audio?.Thumb != null)
                {
                    coverUrl = nextTrack.audio.Thumb.Photo68 ?? nextTrack.audio.Thumb.Photo34;
                }

                if (coverUrl != null)
                {
                    NextTrackCover.Source = new BitmapImage(new Uri(coverUrl));
                    NextTrackNote.Visibility = Visibility.Collapsed;
                }
                else
                {
                    NextTrackCover.Source = null;
                    NextTrackNote.Visibility = Visibility.Visible;
                }

                NextTrackName.Text = nextTrack.audio?.Title ?? "Unknown";
                NextTrackArtist.Text = nextTrack.audio?.Artist ?? "Unknown";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FullScreenWindow] Error in SetNextTrackData: {ex.Message}");
            }
        }

        private async Task LoadLyrics()
        {
            try
            {
                // Сбрасываем состояние синхронизации
                _timedLyricsLines.Clear();
                _currentLyricsLineIndex = -1;
                _hasTimedLyrics = false;

                // По умолчанию показываем заглушку
                if (LyricsPlaceholder != null)
                    LyricsPlaceholder.Visibility = Visibility.Visible;
                if (LyricsScrollViewer != null)
                    LyricsScrollViewer.Visibility = Visibility.Collapsed;

                // Проверяем настройку — если текст скрыт, показываем соответствующее сообщение
                var hideSetting = DB.SettingsTable.GetSetting("fullScreenHideLyrics");
                if (hideSetting != null && hideSetting.settingValue == "1")
                {
                    System.Diagnostics.Debug.WriteLine("[FullScreenWindow] Lyrics hidden by setting");
                    ShowLyricsDisabledMessage();
                    return;
                }

                var track = await GetTrackDataAsync();
                if (track?.audio == null) return;

                var audioTrack = track.audio;

                // Определяем порядок источников на основе настройки
                var sourceSetting = DB.SettingsTable.GetSetting("lyricsSource", "0");
                int sourceIndex = int.Parse(sourceSetting.settingValue);

                bool loaded = false;

                switch (sourceIndex)
                {
                    case 1: // Только VK
                        loaded = await TryLoadVkLyrics(audioTrack);
                        break;
                    case 2: // Только Genius
                        loaded = await TryLoadGeniusLyrics(audioTrack);
                        break;
                    case 3: // Только LRCLib
                        loaded = await TryLoadLrcLibLyrics(audioTrack);
                        break;
                    default: // Auto — пробуем все по порядку
                        loaded = await TryLoadVkLyrics(audioTrack)
                               || await TryLoadGeniusLyrics(audioTrack)
                               || await TryLoadLrcLibLyrics(audioTrack);
                        break;
                }

                if (!loaded)
                {
                    System.Diagnostics.Debug.WriteLine("[FullScreenWindow] No lyrics found from any source");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FullScreenWindow] Error in LoadLyrics: {ex.Message}");
                // При ошибке оставляем заглушку
                if (LyricsPlaceholder != null)
                    LyricsPlaceholder.Visibility = Visibility.Visible;
                if (LyricsScrollViewer != null)
                    LyricsScrollViewer.Visibility = Visibility.Collapsed;
            }
        }

        private async Task<bool> TryLoadVkLyrics(VkNet.Model.Attachments.Audio audioTrack)
        {
            try
            {
                if (!audioTrack.HasLyrics) return false;

                var vkService = VK.vkService;
                if (vkService == null) return false;

                var lyrics = await vkService.GetLyrics(audioTrack.OwnerId + "_" + audioTrack.Id);
                if (lyrics?.LyricsInfo == null) return false;

                List<string> lines;
                if (lyrics.LyricsInfo.Timestamps != null && lyrics.LyricsInfo.Timestamps.Count > 0)
                {
                    // Сохраняем строки с таймстемпами для синхронизации
                    _timedLyricsLines = lyrics.LyricsInfo.Timestamps
                        .Select(t => (t.Begin, t.Line)) // Begin в миллисекундах
                        .ToList();
                    _hasTimedLyrics = true;
                    _currentLyricsLineIndex = -1;

                    lines = lyrics.LyricsInfo.Timestamps.Select(t => t.Line).ToList();

                    // Запускаем таймер для синхронизации текста
                    if (_timer == null)
                    {
                        _timer = new DispatcherTimer();
                        _timer.Interval = TimeSpan.FromMilliseconds(250);
                        _timer.Tick += Timer_Tick;
                        _timer.Start();
                    }
                }
                else if (lyrics.LyricsInfo.Text != null && lyrics.LyricsInfo.Text.Count > 0)
                {
                    _hasTimedLyrics = false;
                    lines = lyrics.LyricsInfo.Text;
                }
                else
                {
                    return false;
                }

                ShowLyrics(lines);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> TryLoadGeniusLyrics(VkNet.Model.Attachments.Audio audioTrack)
        {
            try
            {
                if (_geniusService == null) return false;

                var artist = audioTrack.MainArtists?.FirstOrDefault()?.Name ?? audioTrack.Artist ?? "";
                var results = await _geniusService.SearchAsync($"{audioTrack.Title} {artist}");
                var song = results.FirstOrDefault()?.Result;

                if (song == null) return false;

                var lyrics = await _geniusService.GetSongAsync(song.Id);
                if (lyrics?.Lyrics?.Plain == null) return false;

                var lines = lyrics.Lyrics.Plain.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
                if (lines.Count == 0) return false;

                ShowLyrics(lines);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> TryLoadLrcLibLyrics(VkNet.Model.Attachments.Audio audioTrack)
        {
            try
            {
                // Http-клиент может быть не инициализирован (или уже освобождён) —
                // создаём при необходимости, чтобы не падать с NullReferenceException.
                _lyricsHttpClient ??= new HttpClient();

                var artist = audioTrack.MainArtists?.FirstOrDefault()?.Name ?? audioTrack.Artist ?? "";
                var title = audioTrack.Title ?? "";

                // Remove (feat. ...) parts from title if present
                var featIndex = title.IndexOf("(feat.");
                if (featIndex > 0)
                {
                    title = title.Substring(0, featIndex).Trim();
                }

                var url = $"https://lrclib.net/api/search?track_name={WebUtility.UrlEncode(title)}&artist_name={WebUtility.UrlEncode(artist)}";
                var response = await _lyricsHttpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode) return false;

                var json = await response.Content.ReadAsStringAsync();
                var results = JsonSerializer.Deserialize<List<LrcLibResult>>(json);

                var bestMatch = results?.FirstOrDefault();
                if (bestMatch?.PlainLyrics == null && bestMatch?.SyncedLyrics == null) return false;

                // Пробуем сначала синхронизированный текст (LRC формат)
                if (bestMatch.SyncedLyrics != null)
                {
                    var syncedLines = bestMatch.SyncedLyrics.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    var timedLines = new List<(int ms, string line)>();
                    var plainLines = new List<string>();

                    foreach (var rawLine in syncedLines)
                    {
                        // Парсим LRC формат: [mm:ss.xx]line text
                        var match = System.Text.RegularExpressions.Regex.Match(rawLine, @"^\[(\d+):(\d+(?:\.\d+)?)\](.*)");
                        if (match.Success)
                        {
                            int minutes = int.Parse(match.Groups[1].Value);
                            double seconds = double.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                            int ms = (int)(minutes * 60000 + seconds * 1000);
                            string text = match.Groups[3].Value.Trim();

                            if (!string.IsNullOrEmpty(text))
                            {
                                timedLines.Add((ms, text));
                                plainLines.Add(text);
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(rawLine))
                        {
                            plainLines.Add(rawLine.Trim());
                        }
                    }

                    if (timedLines.Count > 0)
                    {
                        _timedLyricsLines = timedLines;
                        _hasTimedLyrics = true;
                        _currentLyricsLineIndex = -1;

                        // Запускаем таймер для синхронизации
                        if (_timer == null)
                        {
                            _timer = new DispatcherTimer();
                            _timer.Interval = TimeSpan.FromMilliseconds(250);
                            _timer.Tick += Timer_Tick;
                            _timer.Start();
                        }

                        ShowLyrics(plainLines);
                        return true;
                    }

                    // Если не удалось распарсить LRC, используем как plain text
                    if (plainLines.Count > 0)
                    {
                        _hasTimedLyrics = false;
                        ShowLyrics(plainLines);
                        return true;
                    }
                }

                // Plain text fallback
                if (bestMatch.PlainLyrics != null)
                {
                    var lines = bestMatch.PlainLyrics.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
                    if (lines.Count > 0)
                    {
                        _hasTimedLyrics = false;
                        ShowLyrics(lines);
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private void ShowLyrics(List<string> lines)
        {
            if (LyricsPlaceholder == null || LyricsScrollViewer == null || LyricsListView == null)
                return;

            LyricsPlaceholder.Visibility = Visibility.Collapsed;
            LyricsScrollViewer.Visibility = Visibility.Visible;
            LyricsListView.ItemsSource = lines;
        }

        private void ShowLyricsDisabledMessage()
        {
            if (LyricsPlaceholder == null || LyricsScrollViewer == null)
                return;

            LyricsPlaceholder.Visibility = Visibility.Visible;
            LyricsScrollViewer.Visibility = Visibility.Collapsed;

            // Меняем текст заглушки на сообщение об отключении
            if (LyricsPlaceholder.Children.Count >= 2)
            {
                if (LyricsPlaceholder.Children[0] is TextBlock titleBlock)
                {
                    titleBlock.Text = "Отображение текста";
                }
                if (LyricsPlaceholder.Children[1] is TextBlock subtitleBlock)
                {
                    subtitleBlock.Text = "Отключено в настройках";
                }
            }
        }

        /// <summary>
        /// 
        /// Модель для ответа LRCLib API
        /// </summary>
        private class LrcLibResult
        {
            public string PlainLyrics { get; set; }
            public string SyncedLyrics { get; set; }
        }

        private void Timer_Tick(object sender, object e)
        {
            try
            {
                if (MediaPlayer?.PlaybackSession == null || !_hasTimedLyrics || _timedLyricsLines.Count == 0 || LyricsListView == null)
                    return;

                var positionMs = (int)MediaPlayer.PlaybackSession.Position.TotalMilliseconds;

                // Ищем текущую строку по таймстемпам
                int newIndex = -1;
                for (int i = 0; i < _timedLyricsLines.Count; i++)
                {
                    if (positionMs >= _timedLyricsLines[i].milliseconds)
                    {
                        newIndex = i;
                    }
                    else
                    {
                        break;
                    }
                }

                if (newIndex != _currentLyricsLineIndex && newIndex >= 0 && newIndex < _timedLyricsLines.Count)
                {
                    _currentLyricsLineIndex = newIndex;

                    // Подсвечиваем текущую строку в ListView
                    LyricsListView.SelectedIndex = _currentLyricsLineIndex;
                    LyricsListView.ScrollIntoView(LyricsListView.SelectedItem, ScrollIntoViewAlignment.Leading);
                }
            }
            catch { }
        }

        private void LyricsSourceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Сохраняем выбранный источник
                DB.SettingsTable.SetSetting("lyricsSource", LyricsSourceCombo.SelectedIndex.ToString());

                // Перезагружаем текст с новым источником
                _ = LoadLyrics();
            }
            catch { }
        }

        #endregion

        #region Helper Methods

        private void CloseFullScreen()
        {
            // Отписываемся от событий
            VK_UI3.Services.MediaPlayerService.AudioPlayedChangeEvent -= OnTrackChanged;
            VK_UI3.Services.MediaPlayerService.PositionChanged -= OnPositionChanged;
            VK_UI3.Services.MediaPlayerService.MediaPlayer.CurrentStateChanged -= OnPlaybackStateChanged;

            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
            }

            this.Close();
        }

        public static async Task<ExtendedAudio> _TrackDataThisGet(bool forced = false)
        {
            if (iVKGetAudio != null && iVKGetAudio.countTracks != 0)
            {
                return await iVKGetAudio.GetTrackPlay(forced);
            }
            return VK_UI3.Services.MediaPlayerService._trackDataThis;
        }

        #endregion
    }
}