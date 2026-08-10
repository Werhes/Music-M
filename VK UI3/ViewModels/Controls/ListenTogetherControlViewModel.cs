using Microsoft.Extensions.DependencyInjection;
using MusicX.Core.Services;
using MusicX.Services;
using MusicX.Shared.ListenTogether;
using MusicX.Shared.Player;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VK_UI3.Helpers;
using VK_UI3.VKs;
using VkNet.Abstractions;
using VkNet.Enums.Filters;

namespace VK_UI3.ViewModels.Controls
{
    public class ListenTogetherControlViewModel : INotifyPropertyChanged
    {
        public ListenTogetherService Service => _service;
        private readonly ListenTogetherService _service;
        private readonly IVkApi _vkApi;
        private readonly VkService _vkService;
        private bool _isConnected;
        private bool _isSessionHost;
        private bool _isLoading;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<ListenTogetherSession> Sessions { get; } = new();

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsDisconnected));
                }
            }
        }

        public bool IsDisconnected => !_isConnected;

        public bool IsSessionHost
        {
            get => _isSessionHost;
            set
            {
                if (_isSessionHost != value)
                {
                    _isSessionHost = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        public ListenTogetherControlViewModel(ListenTogetherService service, IVkApi vkApi, VkService vkService)
        {
            _service = service;
            _vkApi = vkApi;
            _vkService = vkService;

            service.LeaveSession += OnDisconnected;
            service.SessionStoped += OnDisconnected;
            service.SessionOwnerStoped += OnDisconnected;
            service.StartedSession += OnSessionStarted;
            service.ConnectedToSession += OnSessionConnected;
            service.ListenerConnected += OnListenerConnected;
            service.ListenerDisconnected += OnListenerDisconnected;
        }

        public async Task StartSessionAsync()
        {
            try
            {
                var connectionService = StaticService.Container.GetRequiredService<BackendConnectionService>();
                connectionService.ReportMetric("StartSession");

                IsLoading = true;
                var configService = StaticService.Container.GetRequiredService<VkService>();
                var userId = await GetCurrentUserIdAsync();

                var sessionId = await _service.StartSessionAsync(userId);

                // Отправляем текущий трек в сессию
                var currentTrack = GetCurrentPlaylistTrack();
                if (currentTrack != null)
                {
                    await _service.ChangeTrackAsync(currentTrack);
                }

                IsLoading = false;
            }
            catch (HttpRequestException ex)
            {
                IsLoading = false;
                throw new Exception($"Сервер совместного прослушивания недоступен. Проверьте подключение к интернету. ({ex.Message})");
            }
            catch (Exception)
            {
                IsLoading = false;
                throw;
            }
        }

        public async Task ConnectToSessionAsync(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentException("Введите ID сессии");

            IsLoading = true;
            try
            {
                var connectionService = StaticService.Container.GetRequiredService<BackendConnectionService>();
                connectionService.ReportMetric("ConnectToSession");

                var userId = await GetCurrentUserIdAsync();
                await _service.ConnectToServerAsync(userId);
                await _service.JoinToSesstionAsync(sessionId);
            }
            catch (HttpRequestException ex)
            {
                IsLoading = false;
                throw new Exception($"Сервер совместного прослушивания недоступен. Проверьте подключение к интернету. ({ex.Message})");
            }
            catch
            {
                IsLoading = false;
                throw;
            }

            IsLoading = false;
        }

        public async Task StopAsync()
        {
            if (IsSessionHost)
                await _service.StopPlaySessionAsync();
            else
                await _service.LeavePlaySessionAsync();
        }

        private async Task<long> GetCurrentUserIdAsync()
        {
            try
            {
                if (_vkApi.IsAuthorized && _vkApi.UserId.HasValue)
                    return _vkApi.UserId.Value;
                
                // Пробуем получить из VK
                var user = await _vkService.GetCurrentUserAsync();
                if (user != null)
                    return user.Id;
            }
            catch
            {
                // Ignore
            }
            
            return 0L;
        }

        private PlaylistTrack GetCurrentPlaylistTrack()
        {
            var playingTrack = Services.MediaPlayerService.PlayingTrack;
            if (playingTrack?.audio == null) return null;

            var audio = playingTrack.audio;
            var mainArtists = new System.Collections.Generic.List<TrackArtist>();

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

        private Task OnListenerDisconnected(User user)
        {
            var session = Sessions.FirstOrDefault(b => b.VkId == user.VkId);
            if (session != null)
                Sessions.Remove(session);
            return Task.CompletedTask;
        }

        private async Task OnListenerConnected(User user)
        {
            try
            {
                var vkUsers = StaticService.Container.GetRequiredService<VkNet.Abstractions.IUsersCategory>();
                var users = await vkUsers.GetAsync(new[] { user.VkId },
                    ProfileFields.FirstName | ProfileFields.LastName | ProfileFields.Photo100);

                if (users.Any())
                {
                    Sessions.Add(new ListenTogetherSession(
                        user.VkId,
                        $"{users[0].FirstName} {users[0].LastName}",
                        users[0].PhotoPreviews.Photo100?.ToString() ?? ""
                    ));
                }
            }
            catch
            {
                // Если не удалось получить информацию о пользователе, добавляем с минимальными данными
                Sessions.Add(new ListenTogetherSession(user.VkId, $"User {user.VkId}", ""));
            }
        }

        private Task OnSessionConnected(PlaylistTrack arg)
        {
            IsConnected = true;
            IsSessionHost = false;
            return Task.CompletedTask;
        }

        private Task OnSessionStarted(string sessionId)
        {
            IsConnected = true;
            IsSessionHost = true;
            return Task.CompletedTask;
        }

        private Task OnDisconnected()
        {
            IsConnected = false;
            IsSessionHost = false;
            Sessions.Clear();
            return Task.CompletedTask;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public record ListenTogetherSession(long VkId, string Name, string AvatarUrl);
}