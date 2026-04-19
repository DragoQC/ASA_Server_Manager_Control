namespace managerwebapp.Models.Servers;

public sealed record PublicServerOverviewItem(
    int RemoteServerId,
    string RemoteUrl,
    string VpnAddress,
    string ConnectionState,
    string ValidationStatus,
    int CurrentPlayers,
    int MaxPlayers,
    string MapName,
    IReadOnlyList<PublicServerModItem> Mods);
