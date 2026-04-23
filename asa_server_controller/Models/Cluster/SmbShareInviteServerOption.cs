namespace asa_server_controller.Models.Cluster;

public sealed record SmbShareInviteServerOption(
    int Id,
    string VpnAddress,
    int? Port);
