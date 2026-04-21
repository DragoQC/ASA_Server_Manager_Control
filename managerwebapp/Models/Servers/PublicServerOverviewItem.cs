namespace managerwebapp.Models.Servers;

public sealed record PublicServerOverviewItem(
    int RemoteServerId,
    string ServerName,
    string VpnAddress,
    int? Port,
    string ConnectionState,
    string ValidationStatus,
    int CurrentPlayers,
    int MaxPlayers,
    string MapName,
    IReadOnlyList<PublicServerModItem> Mods);
