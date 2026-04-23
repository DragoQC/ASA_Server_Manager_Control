namespace asa_server_controller.Models.Cluster;

public sealed record SmbShareInviteListItem(
    int Id,
    int RemoteServerId,
    string VpnAddress,
    string InviteLink,
    DateTimeOffset? UsedAtUtc,
    DateTimeOffset CreatedAtUtc);
