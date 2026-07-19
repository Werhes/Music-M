using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VK_UI3.Helpers;
using VK_UI3.VKs.IVK;
using Windows.Media;
using Windows.Media.Playback;
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

        public ExtendedAudio TrackDataThis => _TrackDataThisGet().Result;

        public string Thumbnail
        {
            get
            {
                var trackData = TrackDataThis;
                if (trackData?.audio?.Album?.Thumb == null) return null;

                return trackData.audio.Album.Thumb.Photo600
                     ?? trackData.audio.Album.Thumb.Photo300
                     ?? trackData.audio.Album.Thumb.Photo270
                     ?? trackData.audio.Album.Thumb.Photo68
                     ?? trackData.audio.Album.Thumb.Photo34
                     ?? null;
            }
        }

        #endregion

        #region Constructor

        public FullScreenWindow()
        {
            this.InitializeComponent();

            // Переводим окно в полноэкранный режим
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);

            // Подписываемся на события
            VK_UI3.Services.MediaPlayerService.AudioPlayedChangeEvent += OnTrackChanged;
            VK_UI3.Services.MediaPlayerService.PositionChanged += OnPositionChanged;

            // KeyDown для Escape
            this.Content.KeyDown += OnWindowKeyDown;

            // Загружаем данные
            SetData();
        }

        #endregion

        #region Event Handlers

        private void OnTrackChanged(object sender, EventArgs e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                SetData();
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

        #endregion

        #region Data Methods

        private void SetData()
        {
            try
            {
                var track = TrackDataThis;
                if (track?.audio == null)
                {
                    System.Diagnostics.Debug.WriteLine("[FullScreenWindow] CurrentTrack is null");
                    return;
                }

                PositionSlider.Maximum = track.audio.Duration;

                var thumb = Thumbnail;
                if (thumb != null)
                {
                    var bitmapImage = new BitmapImage(new Uri(thumb));
                    CoverImage.Source = bitmapImage;
                    CoverNote.Visibility = Visibility.Collapsed;
                }
                else
                {
                    CoverImage.Source = null;
                    CoverNote.Visibility = Visibility.Visible;
                }

                // Next track info
                SetNextTrackData();

                TrackName.Text = track.audio.Title ?? "Unknown";
                ArtistName.Text = track.audio.Artist ?? "Unknown";

                // Пытаемся загрузить тексты песен
                _ = LoadLyrics();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FullScreenWindow] Error in SetData: {ex.Message}");
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

                var thumb = nextTrack.audio?.Album?.Thumb;
                if (thumb != null)
                {
                    var coverUrl = thumb.Photo68 ?? thumb.Photo34;
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
                // По умолчанию показываем заглушку
                LyricsPlaceholder.Visibility = Visibility.Visible;
                LyricsScrollViewer.Visibility = Visibility.Collapsed;

                var track = TrackDataThis;
                if (track?.audio == null) return;

                // Пытаемся получить тексты через VK API
                var vkService = VK_UI3.VKs.VK.vkService;
                if (vkService == null) return;

                var lyrics = await vkService.GetLyrics(track.audio.OwnerId + "_" + track.audio.Id);
                if (lyrics?.LyricsInfo == null)
                {
                    // Оставляем заглушку
                    return;
                }

                List<string> lines;
                if (lyrics.LyricsInfo.Text != null)
                {
                    lines = lyrics.LyricsInfo.Text;
                }
                else if (lyrics.LyricsInfo.Timestamps != null)
                {
                    lines = lyrics.LyricsInfo.Timestamps.Select(t => t.Line).ToList();
                }
                else
                {
                    // Оставляем заглушку
                    return;
                }

                if (lines.Count == 0)
                {
                    // Оставляем заглушку
                    return;
                }

                // Показываем текст, скрываем заглушку
                LyricsPlaceholder.Visibility = Visibility.Collapsed;
                LyricsScrollViewer.Visibility = Visibility.Visible;
                LyricsItemsControl.ItemsSource = lines;

                // Запускаем таймер для синхронизации текста (если есть таймстемпы)
                if (lyrics.LyricsInfo.Timestamps != null && lyrics.LyricsInfo.Timestamps.Count > 0)
                {
                    if (_timer == null)
                    {
                        _timer = new DispatcherTimer();
                        _timer.Interval = TimeSpan.FromMilliseconds(500);
                        _timer.Tick += Timer_Tick;
                        _timer.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FullScreenWindow] Error in LoadLyrics: {ex.Message}");
                // При ошибке оставляем заглушку
                LyricsPlaceholder.Visibility = Visibility.Visible;
                LyricsScrollViewer.Visibility = Visibility.Collapsed;
            }
        }

        private void Timer_Tick(object sender, object e)
        {
            try
            {
                // Синхронизация текста с позицией — обновляем не чаще чем раз в 500 мс
                if (MediaPlayer?.PlaybackSession != null)
                {
                    var position = MediaPlayer.PlaybackSession.Position;
                    // В будущем здесь можно добавить подсветку текущей строки текста
                }
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