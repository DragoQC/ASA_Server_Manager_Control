namespace asa_server_controller.Models.Cluster;

public sealed record SmbShareInviteResponse(
    string SharePath,
    string MountPath,
    string ClientConfig);
