using managerwebapp.Models.Servers;

namespace managerwebapp.Models.Home;

public sealed record HomeServerModel(
    int RemoteServerId,
    string DisplayServerName,
    string StateLabel,
    int CurrentPlayers,
    int MaxPlayers,
    string FormattedMapName,
    IReadOnlyList<PublicServerModItem> Mods);
