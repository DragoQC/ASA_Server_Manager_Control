namespace managerwebapp.Models.Servers;

public sealed record ServerOverviewItem(
    int RemoteServerId,
    string ServerName,
    string VpnAddress,
    int? Port,
    string StateLabel,
    int CurrentPlayers,
    int MaxPlayers,
    string MapName,
    IReadOnlyList<PublicServerModItem> Mods);
