using MusicX.Server.Shared.ListenTogether;
using MusicX.Server.Shared.Player;

namespace MusicX.Server.Models;

public class ListenTogetherSession
{
    public User Owner { get; set; } = null!;
    public List<User> Listeners { get; set; } = new();
    public PlaylistTrack? CurrentTrack { get; set; }
}
