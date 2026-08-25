using Microsoft.AspNetCore.SignalR;
using MusicX.Server.Shared.ListenTogether;
using MusicX.Server.Shared.Player;
using SignalR.Protobuf;

namespace MusicX.Server.Shared.Extensions;

public static class HubBuilderExtensions
{
    public static T AddProtobufProtocol<T>(this T builder) where T : ISignalRBuilder
    {
        return builder.AddProtobufProtocol(new Dictionary<int, Type>
        {
            [0] = typeof(PlaylistTrack),
            [1] = typeof(SessionId),
            [2] = typeof(PlayState),
            [3] = typeof(ErrorState),
            [4] = typeof(User),
        });
    }
}