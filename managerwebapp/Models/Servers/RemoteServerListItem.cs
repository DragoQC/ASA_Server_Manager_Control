namespace managerwebapp.Models.Servers;

public sealed record RemoteServerListItem(
    int Id,
    string RemoteUrl,
    string VpnAddress,
    string InviteStatus,
    string ValidationStatus,
    DateTimeOffset? LastSeenAtUtc,
    DateTimeOffset CreatedAtUtc);
