using Microsoft.AspNetCore.SignalR;
using MusicX.Server.Services;
using MusicX.Server.Shared.ListenTogether;
using MusicX.Server.Shared.Player;

namespace MusicX.Server.Hubs;

public class ListenTogetherHub : Hub
{
    private readonly ILogger<ListenTogetherHub> _logger;
    private readonly ListenTogetherService _listenTogetherService;

    public ListenTogetherHub(ILogger<ListenTogetherHub> logger, ListenTogetherService listenTogetherService)
    {
        _logger = logger;
        _listenTogetherService = listenTogetherService;
    }

    /// <summary>
    /// Смена трека
    /// </summary>
    public async Task<ErrorState> ChangeTrack(PlaylistTrack track)
    {
        try
        {
            var owner = Context.ConnectionId;
            return new(await _listenTogetherService.ChangeTrackAsync(track, owner));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw new HubException(ex.Message, ex);
        }
    }

    /// <summary>
    /// Изменилось состояние трека
    /// </summary>
    public async Task<ErrorState> ChangePlayState(PlayState state)
    {
        try
        {
            var (position, pause) = state;
            var owner = Context.ConnectionId;
            return new(await _listenTogetherService.ChangePlayStateAsync(owner, position, pause));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw new HubException(ex.Message, ex);
        }
    }
    
    /// <summary>
    /// Создать сессию "Слушать вместе"
    /// </summary>
    public async Task<SessionId?> StartPlaySession()
    {
        try
        {
            var ownerConnectionId = Context.ConnectionId;
            var ownerVkId = long.Parse(Context.UserIdentifier!);

            var id = await _listenTogetherService.StartSessionAsync(ownerConnectionId, ownerVkId);

            return id is null ? null : new(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw new HubException(ex.Message, ex);
        }
    }

    /// <summary>
    /// Остановить сессию "Слушать вместе"
    /// </summary>
    public async Task<ErrorState> StopPlaySession()
    {
        try
        {
            var owner = Context.ConnectionId;
            ErrorState res = new(await _listenTogetherService.StopSessionAsync(owner));

            return res;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw new HubException(ex.Message, ex);
        }
    }
    
    /// <summary>
    /// Присоединиться к сессии
    /// </summary>
    public async Task<ErrorState> JoinPlaySession(SessionId sessionId)
    {
        try
        {
            var listenerConnectionId = Context.ConnectionId;
            var listenerVkId = long.Parse(Context.UserIdentifier!);

            return new(await _listenTogetherService.JoinToSessionAsync(listenerConnectionId, listenerVkId, sessionId.Id));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw new HubException(ex.Message, ex);
        }
    }

    /// <summary>
    /// Покинуть сессию
    /// </summary>
    public async Task<ErrorState> LeavePlaySession()
    {
        try
        {
            var listenerConnectionId = Context.ConnectionId;
            var listenerVkId = long.Parse(Context.UserIdentifier!);

            var result = await _listenTogetherService.LeaveSessionAsync(listenerConnectionId, listenerVkId);

            return new(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw new HubException(ex.Message, ex);
        }
    }

    public async Task<PlaylistTrack?> GetCurrentTrack()
    {
        try
        {
            var listenerVkId = long.Parse(Context.UserIdentifier!);
            return await _listenTogetherService.GetCurrentSessionTrack(listenerVkId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw new HubException(ex.Message, ex);
        }
    }

    public async Task<User> GetSessionOwnerInfo()
    {
        try
        {
            var listenerVkId = long.Parse(Context.UserIdentifier!);
            var res = await _listenTogetherService.GetOwnerSessionInfoAsync(listenerVkId);

            return new(res.ConnectionId, res.VkId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw new HubException(ex.Message, ex);
        }
    }

    public async Task<UsersList?> GetListenersInSession()
    {
        try
        {
            var ownerConnectionid = Context.ConnectionId;
            var res = await _listenTogetherService.GetListenersInSessionAsync(ownerConnectionid);

            return res;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw new HubException(ex.Message, ex);
        }
    }

    public async override Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var connectionId = Context.ConnectionId;
            await _listenTogetherService.ClientDisconnected(connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw new HubException(ex.Message, ex);
        }
    }
}