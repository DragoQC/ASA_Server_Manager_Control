namespace managerwebapp.Models.Cluster;

public sealed record NfsShareInviteServerOption(
    int Id,
    string VpnAddress,
    int? Port);
