namespace managerwebapp.Models.Servers;

public sealed record RemoteServerListItem(
    int Id,
    string VpnAddress,
    int? Port,
    string StateLabel,
    bool IsOnline,
    bool CanStart,
    bool CanStop,
    bool CanOpenRcon,
    string MapName,
    int CurrentPlayers,
    int MaxPlayers,
    DateTimeOffset? ServerInfoCheckedAtUtc,
    DateTimeOffset? LastSeenAtUtc,
    DateTimeOffset CreatedAtUtc);
