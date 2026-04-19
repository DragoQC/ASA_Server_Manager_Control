namespace managerwebapp.Models.Invitations;

public sealed record InviteRemoteServerRequest(
    string ClusterId,
    string VpnAddress,
    string ServerEndpoint,
    string Dns,
    string AllowedIps,
    string ServerPublicKey,
    string ClientPrivateKey,
    string? PresharedKey);
