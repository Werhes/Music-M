using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VK_UI3.DB;
using VK_UI3.DownloadTrack;
using VK_UI3.Helpers;
using VK_UI3.Helpers.Animations;
using VK_UI3.Services;
using VK_UI3.Views;
using VK_UI3.VKs;
using VK_UI3.VKs.IVK;
using Windows.Media.Playback;
using Windows.Storage.Pickers;
using static VK_UI3.Views.SectionView;

namespace VK_UI3.Controllers
{
    /// <summary>
    /// Альфа-плеер в стиле MusicX для нижней панели.
    /// </summary>
    public sealed partial class BetterPlayer : Page
    {
        private AnimationsChangeImage _coverChanger;
        private bool _isSeeking;
        private bool _isUpdatingVolume;
        private bool _suppressEvents;
        private bool _repeatEnabled;
        private bool _shuffleEnabled;
        private bool _liked;
        private bool _userToggled;
        private string _currentAudioKey;

        public BetterPlayer()
        {
            this.InitializeComponent();
            this.Loaded += BetterPlayer_Loaded;
            this.Unloaded += BetterPlayer_Unloaded;
        }

        private void BetterPlayer_Loaded(object sender, RoutedEventArgs e)
        {
            _coverChanger = new AnimationsChangeImage(CoverImage, DispatcherQueue);

            MediaPlayerService.AudioPlayedChangeEvent += OnAudioPlayedChange;
            MediaPlayerService.PositionChanged += OnPositionChanged;
            MediaPlayerService.VolumeChanged += OnVolumeChanged;
            MediaPlayerService.MediaPlayer.CurrentStateChanged += OnCurrentStateChanged;
            FlyOutControl.Opened += FlyOutControl_Opened;

            RefreshTrackInfo();
            VolumeSlider.Value = MediaPlayerService.Volume;
            UpdatePlayButton();
            UpdateVisibility();
        }

        private void BetterPlayer_Unloaded(object sender, RoutedEventArgs e)
        {
            MediaPlayerService.AudioPlayedChangeEvent -= OnAudioPlayedChange;
            MediaPlayerService.PositionChanged -= OnPositionChanged;
            MediaPlayerService.VolumeChanged -= OnVolumeChanged;
            MediaPlayerService.MediaPlayer.CurrentStateChanged -= OnCurrentStateChanged;
            FlyOutControl.Opened -= FlyOutControl_Opened;
        }

        private void FlyOutControl_Opened(object sender, object e)
        {
            FlyOutControl.dataTrack = MediaPlayerService.PlayingTrack;
        }

        private void OnAudioPlayedChange(object sender, EventArgs e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                RefreshTrackInfo();
                UpdateVisibility();
            });
        }

        /// <summary>
        /// Плеер виден только когда есть активный трек (играет или на паузе)
        /// и при этом BetterPlayer включён в dev-настройках.
        /// </summary>
        private void UpdateVisibility()
        {
            if (!VK_UI3.Services.CacheSettingsManager.IsBetterPlayerEnabled())
            {
                this.Visibility = Visibility.Collapsed;
                return;
            }

            bool hasTrack = MediaPlayerService.PlayingTrack?.audio != null;
            this.Visibility = hasTrack ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetLikeVisual(bool liked)
        {
            LikeIconOutline.Visibility = liked ? Visibility.Collapsed : Visibility.Visible;
            LikeIconFill.Visibility = liked ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ResetLikeState()
        {
            _liked = false;
            SetLikeVisual(false);
        }

        private void RefreshTrackInfo()
        {
            var audio = MediaPlayerService.PlayingTrack?.audio;
            if (audio == null)
                return;

            TitleText.Text = audio.Title ?? string.Empty;
            ArtistText.Text = audio.Artist ?? string.Empty;

            string thumb = audio.Album?.Thumb?.Photo600
                         ?? audio.Album?.Thumb?.Photo300
                         ?? audio.Album?.Thumb?.Photo270
                         ?? audio.Album?.Thumb?.Photo68
                         ?? audio.Album?.Thumb?.Photo34;
            if (!string.IsNullOrEmpty(thumb) && thumb != "null")
                _coverChanger?.ChangeImageWithAnimation(thumb);

            var session = MediaPlayerService.MediaPlayer.PlaybackSession;
            double duration = session?.NaturalDuration.TotalSeconds ?? 0;
            _suppressEvents = true;
            ProgressSlider.Maximum = duration > 0 ? duration : 1;
            ProgressSlider.Value = 0;
            _suppressEvents = false;
            DurationText.Text = Format(session?.NaturalDuration);
            PositionText.Text = "0:00";

            ResetLikeState();
            _ = ApplyLikedStateAsync();
            UpdatePlayButton();
        }

        /// <summary>
        /// Проверяет по API (audio.isAdded), добавлен ли трек в библиотеку пользователя,
        /// и обновляет иконку лайка. Запускается только при смене трека.
        /// </summary>
        private async Task ApplyLikedStateAsync()
        {
            var audio = MediaPlayerService.PlayingTrack?.audio;
            if (audio == null)
                return;

            string key = $"{audio.OwnerId}_{audio.Id}";
            if (_currentAudioKey == key)
                return;

            _currentAudioKey = key;
            _userToggled = false;

            try
            {
                bool liked = await VK.IsAudioLiked(audio.Id, audio.OwnerId);
                if (_userToggled)
                    return;

                _liked = liked;
                SetLikeVisual(liked);
            }
            catch
            {
                if (_userToggled)
                    return;

                _liked = false;
                SetLikeVisual(false);
            }
        }

        private void OnPositionChanged(object sender, TimeSpan e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (_isSeeking)
                    return;

                _suppressEvents = true;
                ProgressSlider.Value = Math.Min(e.TotalSeconds, ProgressSlider.Maximum);
                _suppressEvents = false;
                PositionText.Text = Format(e);
            });
        }

        private void OnVolumeChanged(object sender, VK_UI3.Services.Player.VolumeChangedEventArgs e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                _isUpdatingVolume = true;
                VolumeSlider.Value = e.Volume;
                _isUpdatingVolume = false;
            });
        }

        private void OnCurrentStateChanged(MediaPlayer sender, object args)
        {
            this.DispatcherQueue.TryEnqueue(UpdatePlayButton);
        }

        private void UpdatePlayButton()
        {
            if (MediaPlayerService.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
                PlayPauseIcon.Glyph = "\uE103"; // Пауза
            else
                PlayPauseIcon.Glyph = "\uE102"; // Воспроизвести
        }

        private void PlayPauseBtn_Click(object sender, RoutedEventArgs e)
        {
            var mp = MediaPlayerService.MediaPlayer;
            if (mp.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
                mp.Pause();
            else
                mp.Play();
        }

        private void NextBtn_Click(object sender, RoutedEventArgs e) => MediaPlayerService.PlayNextTrack();

        private void PrevBtn_Click(object sender, RoutedEventArgs e) => MediaPlayerService.PlayPreviousTrack();

        private void ShuffleBtn_Click(object sender, RoutedEventArgs e)
        {
            _shuffleEnabled = !_shuffleEnabled;
            ShuffleIcon.Opacity = _shuffleEnabled ? 1.0 : 0.4;
        }

        private void RepeatBtn_Click(object sender, RoutedEventArgs e)
        {
            _repeatEnabled = !_repeatEnabled;
            RepeatIcon.Opacity = _repeatEnabled ? 1.0 : 0.4;
            try
            {
                MediaPlayerService.MediaPlayer.IsLoopingEnabled = _repeatEnabled;
            }
            catch
            {
                // Игнорируем, если проигрыватель не поддерживает зацикливание
            }
        }

        private async void LikeBtn_Click(object sender, RoutedEventArgs e)
        {
            var audio = MediaPlayerService.PlayingTrack?.audio;
            if (audio == null)
                return;

            // Сразу переключаем иконку (оптимистично), затем зовём API
            _userToggled = true;
            bool targetLiked = !_liked;
            _liked = targetLiked;
            SetLikeVisual(targetLiked);

            try
            {
                // Та же логика, что в контекстном меню трека (AudioControlFlyOut.AddRemove_Click)
                var vkService = VK.vkService;
                if (targetLiked)
                    await vkService.AudioAddAsync((long)audio.Id, (long)audio.OwnerId);
                else
                    await vkService.AudioDeleteAsync((long)audio.Id, (long)audio.OwnerId);
            }
            catch
            {
                // Сетевая/API ошибка — визуально лайк уже применён
            }
        }

        private void LyricsBtn_Click(object sender, RoutedEventArgs e)
        {
            MainView.mainView?.ToggleLyricsPanel();
        }

        private void QueueBtn_Click(object sender, RoutedEventArgs e)
        {
            MainView.mainView?.TogglePlayNowPanel();
        }

        private void Title_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var audio = MediaPlayerService.PlayingTrack?.audio;
            if (audio?.Album != null)
                MainView.OpenSection(audio.Album.Id.ToString(), SectionType.None);
        }

        private void Artist_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var audio = MediaPlayerService.PlayingTrack?.audio;
            if (audio?.MainArtists?.FirstOrDefault() is { } artist)
                MainView.OpenSection(artist.Id.ToString(), SectionType.Artist);
        }

        private void ProgressSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_suppressEvents)
                return;

            _isSeeking = true;
            try
            {
                var session = MediaPlayerService.MediaPlayer.PlaybackSession;
                if (session != null && session.NaturalDuration > TimeSpan.Zero)
                {
                    session.Position = TimeSpan.FromSeconds(e.NewValue);
                    PositionText.Text = Format(session.Position);
                }
            }
            finally
            {
                _isSeeking = false;
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_isUpdatingVolume)
                return;
            MediaPlayerService.Volume = e.NewValue;
        }

        private void FullScreenBtn_Click(object sender, RoutedEventArgs e)
        {
            new Views.FullScreenWindow().Activate();
        }

        private void Cover_Tapped(object sender, TappedRoutedEventArgs e)
        {
            new Views.FullScreenWindow().Activate();
        }

        private async void DownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var playingTrack = MediaPlayerService.PlayingTrack;
                if (playingTrack?.audio == null)
                    return;

                var folderPicker = new FolderPicker();
                folderPicker.FileTypeFilter.Add("*");

                // Без привязки к окну FolderPicker вылетает с "invalid window handle"
                WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, MainWindow.hvn);

                Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
                if (folder == null)
                    return;

                // Директория выбрана — сохраняем её, как в контекстном меню трека
                DB.PathTable.AddPath(folder.Path);

                IVKGetAudio iVKGetAudio = new SimpleAudio(this.DispatcherQueue)
                {
                    name = playingTrack.audio.Title,
                    itsAll = true,
                    countTracks = 1
                };
                iVKGetAudio.listAudio.Add(playingTrack);

                _ = Task.Run(async () =>
                {
                    new PlayListDownload(iVKGetAudio, folder.Path, this.DispatcherQueue, true);
                });
            }
            catch
            {
                // Игнорируем ошибку инициализации/выбора папки
            }
        }

        private async Task ShowError(string title, string message)
        {
            var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "ОК",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async Task ShowSuccess(string message)
        {
            var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = "Успешно",
                Content = message,
                CloseButtonText = "ОК",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private static string Format(TimeSpan? ts)
        {
            if (ts == null || ts.Value <= TimeSpan.Zero)
                return "0:00";
            var t = ts.Value;
            return t.Hours > 0
                ? $"{t.Hours}:{t.Minutes:00}:{t.Seconds:00}"
                : $"{t.Minutes}:{t.Seconds:00}";
        }
    }
}