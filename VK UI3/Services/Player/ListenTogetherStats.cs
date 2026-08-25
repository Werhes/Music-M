using Microsoft.Extensions.DependencyInjection;
using MusicX.Core.Models;
using MusicX.Core.Services;
using MusicX.Shared.ListenTogether;
using MusicX.Shared.Player;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VK_UI3.Helpers;
using VK_UI3.VKs;

namespace VK_UI3.Services.Player
{
    /// <summary>
    /// Слушает события плеера и отправляет их в сервис совместного прослушивания,
    /// когда пользователь является владельцем сессии.
    /// </summary>
    public class ListenTogetherStats
    {
        private readonly ListenTogetherService _listenTogetherService;
        private bool _isSubscribed = false;

        public ListenTogetherStats(ListenTogetherService listenTogetherService)
        {
            _listenTogetherService = listenTogetherService;
        }

        public void Subscribe()
        {
            if (_isSubscribed) return;
            _isSubscribed = true;

            MediaPlayerService.AudioPlayedChangeEvent += OnTrackChanged;
            MediaPlayerService.PositionChanged += OnPositionChanged;
        }

        public void Unsubscribe()
        {
            if (!_isSubscribed) return;
            _isSubscribed = false;

            MediaPlayerService.AudioPlayedChangeEvent -= OnTrackChanged;
            MediaPlayerService.PositionChanged -= OnPositionChanged;
        }

        private async void OnTrackChanged(object sender, EventArgs e)
        {
            if (_listenTogetherService.PlayerMode != PlayerMode.Owner) return;

            var playingTrack = MediaPlayerService.PlayingTrack;
            if (playingTrack?.audio == null) return;

            var playlistTrack = ConvertToPlaylistTrack(playingTrack);
            if (playlistTrack != null)
            {
                await _listenTogetherService.ChangeTrackAsync(playlistTrack);
            }
        }

        private int _positionCounter = 0;

        private async void OnPositionChanged(object sender, TimeSpan position)
        {
            if (_listenTogetherService.PlayerMode != PlayerMode.Owner) return;

            // Отправляем позицию каждый 3-й раз (для оптимизации)
            _positionCounter++;
            if (_positionCounter < 3) return;
            _positionCounter = 0;

            var isPaused = MediaPlayerService.MediaPlayer.PlaybackSession.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Paused;
            await _listenTogetherService.ChangePlayStateAsync(position, isPaused);
        }

        private PlaylistTrack ConvertToPlaylistTrack(ExtendedAudio extendedAudio)
        {
            if (extendedAudio?.audio == null) return null;

            var audio = extendedAudio.audio;
            var mainArtists = new List<TrackArtist>();

            if (!string.IsNullOrEmpty(audio.Artist))
            {
                mainArtists.Add(new TrackArtist(audio.Artist, null));
            }

            var trackData = new VkTrackData(
                audio.Url?.ToString() ?? "",
                false,
                false,
                null,
                TimeSpan.FromSeconds(audio.Duration),
                new IdInfo(audio.Id, audio.OwnerId, ""),
                "",
                null,
                null
            );

            return new PlaylistTrack(
                audio.Title ?? "Unknown",
                audio.Artist ?? "Unknown",
                null,
                mainArtists,
                null,
                trackData
            );
        }
    }
}