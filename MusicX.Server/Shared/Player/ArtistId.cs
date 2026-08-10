using ProtoBuf;

namespace MusicX.Server.Shared.Player;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
public record ArtistId(string Id, ArtistIdType Type);