namespace managerwebapp.Models.Cluster;

public sealed record NfsShareInviteListItem(
    int Id,
    int RemoteServerId,
    string VpnAddress,
    string InviteLink,
    DateTimeOffset? UsedAtUtc,
    DateTimeOffset CreatedAtUtc);
