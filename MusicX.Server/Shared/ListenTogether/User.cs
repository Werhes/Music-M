using ProtoBuf;

namespace MusicX.Server.Shared.ListenTogether;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
public record User(string ConnectionId, long VkId);
