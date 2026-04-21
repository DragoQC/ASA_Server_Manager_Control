namespace managerwebapp.Models.Servers;

public sealed record RemoteServerInfoResponse(
    bool Success,
    string ServerName,
    string MapName,
    int MaxPlayers,
    int? GamePort,
    DateTimeOffset CheckedAtUtc);
