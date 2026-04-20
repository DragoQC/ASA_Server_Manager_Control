namespace managerwebapp.Models.Servers;

public sealed record RemoteServerInfoResponse(
    bool Success,
    string MapName,
    int MaxPlayers,
    DateTimeOffset CheckedAtUtc);
